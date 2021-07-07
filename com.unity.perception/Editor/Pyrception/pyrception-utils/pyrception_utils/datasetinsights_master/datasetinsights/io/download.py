import hashlib
import logging
import os
import re
import tempfile
import zlib
from pathlib import Path

import requests
from requests.adapters import HTTPAdapter
from requests.packages.urllib3.util.retry import Retry

from .exceptions import ChecksumError, DownloadError

logger = logging.getLogger(__name__)

# Timeout of requests (in seconds)
DEFAULT_TIMEOUT = 1800
# Retry after failed request
DEFAULT_MAX_RETRIES = 5


class TimeoutHTTPAdapter(HTTPAdapter):
    def __init__(self, timeout, *args, **kwargs):
        self.timeout = timeout
        super().__init__(*args, **kwargs)

    def send(self, request, **kwargs):
        kwargs["timeout"] = self.timeout
        return super().send(request, **kwargs)


def download_file(source_uri: str, dest_path: str, file_name: str = None):
    """Download a file specified from a source uri

    Args:
        source_uri (str): source url where the file should be downloaded
        dest_path (str): destination path of the file
        file_name (str): file name of the file to be downloaded

    Returns:
        String of destination path.
    """
    logger.debug(f"Trying to download file from {source_uri} -> {dest_path}")
    adapter = TimeoutHTTPAdapter(
        timeout=DEFAULT_TIMEOUT, max_retries=Retry(total=DEFAULT_MAX_RETRIES)
    )
    with requests.Session() as http:
        http.mount("https://", adapter)
        try:
            response = http.get(source_uri)
            response.raise_for_status()
        except requests.exceptions.RequestException as ex:
            logger.error(ex)
            err_msg = (
                f"The request download from {source_uri} -> {dest_path} can't "
                f"be completed."
            )

            raise DownloadError(err_msg)
        else:
            dest_path = Path(dest_path)
            if not file_name:
                file_name = _parse_filename(response, source_uri)
            dest_path = dest_path / file_name
            dest_path.parent.mkdir(parents=True, exist_ok=True)
            with open(dest_path, "wb") as f:
                f.write(response.content)

    return dest_path


def checksum_matches(filepath, expected_checksum, algorithm="CRC32"):
    """ Check if the checksum matches

    Args:
        filepath (str): the doaloaded file path
        expected_checksum (int): expected checksum of the file
        algorithm (str): checksum algorithm. Defaults to CRC32

    Returns:
        True if the file checksum matches.
    """
    computed = compute_checksum(filepath, algorithm)
    return computed == expected_checksum


def validate_checksum(filepath, expected_checksum, algorithm="CRC32"):
    """ Validate checksum of the downloaded file.

    Args:
        filepath (str): the doaloaded file path
        expected_checksum (int): expected checksum of the file
        algorithm (str): checksum algorithm. Defaults to CRC32

    Raises:
        ChecksumError if the file checksum does not match.
    """
    if not checksum_matches(filepath, expected_checksum, algorithm):
        raise ChecksumError


def compute_checksum(filepath, algorithm="CRC32"):
    """ Compute the checksum of a file.

    Args:
        filepath (str): the doaloaded file path
        algorithm (str): checksum algorithm. Defaults to CRC32

    Returns:
        int: the checksum value
    """
    if algorithm == "CRC32":
        chs = _crc32_checksum(filepath)
    elif algorithm == "MD5":
        chs = _md5_checksum(filepath)
    else:
        raise ValueError("Unsupported checksum algorithm!")

    return chs


def _crc32_checksum(filepath):
    """ Calculate the checksum of a file using CRC32.
    """
    with open(filepath, "rb") as f:
        checksum = zlib.crc32(f.read())

    return checksum


def _md5_checksum(filename):
    """ Calculate the checksum of a file using MD5.
    """
    md5 = hashlib.md5()
    with open(filename, "rb") as f:
        for chunk in iter(lambda: f.read(4096), b""):
            md5.update(chunk)
    return md5.hexdigest()


def get_checksum_from_file(filepath):
    """ This method return checksum of the file whose filepath is given.

    Args:
        filepath (str): Path of the checksum file.
                        Path can be HTTP(s) url or local path.

    Raises:
        ValueError: Raises this error if filepath is not local or not
                    HTTP or HTTPS url.

    """

    if filepath.startswith(("http://", "https://")):
        with tempfile.TemporaryDirectory() as tmp:
            checksum_file_path = os.path.join(tmp, "checksum.txt")
            file_path = download_file(
                source_uri=filepath, dest_path=checksum_file_path
            )
            return _read_checksum_from_txt(file_path)

    elif os.path.isfile(filepath):
        return _read_checksum_from_txt(filepath)

    else:
        raise ValueError(f"Can not get checksum from path: {filepath}")


def _read_checksum_from_txt(filepath):
    """ This method reads checksum from a txt file and returns it.

    Args:
        filepath (str): Local filepath of the checksum file.

    Returns:
        str: checksum value from the checksum file.

    """
    with open(filepath) as file:
        checksum = file.read()
    return checksum


def _parse_filename(response, uri):
    file_name = _get_filename_from_response(response)
    if file_name is None:
        file_name = _get_file_name_from_uri(uri)
    return file_name


def _get_filename_from_response(response):
    """ Gets filename from requests response object

        Args:
            response: requests.Response() object that contains the server's
            response to the HTTP request.

        Returns:
            filename (str): Name of the file to be downloaded
    """
    cd = response.headers.get("content-disposition")
    if not cd:
        return None
    file_name = re.findall("filename=(.+)", cd)
    if len(file_name) == 0:
        return None
    return file_name[0]


def _get_file_name_from_uri(uri):
    """ Gets filename from URI

    Args:
        uri (str): URI

    """
    return uri.split("/")[-1]
