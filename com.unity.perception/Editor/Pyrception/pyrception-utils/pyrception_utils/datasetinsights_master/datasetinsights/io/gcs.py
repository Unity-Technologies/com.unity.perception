import base64
import logging
import os
import re
from os import makedirs
from os.path import basename, isdir
from pathlib import Path

from google.cloud.storage import Client

from datasetinsights.io.download import validate_checksum
from datasetinsights.io.exceptions import ChecksumError

logger = logging.getLogger(__name__)


class GCSClient:
    """ This class is used to download data from GCS location
        and perform function such as downloading the dataset and checksum
        validation.
    """

    GCS_PREFIX = "^gs://"
    KEY_SEPARATOR = "/"

    def __init__(self, **kwargs):
        """ Initialize a client to google cloud storage (GCS).
        """
        self.client = Client(**kwargs)

    def download(self, *, url=None, local_path=None, bucket=None, key=None):
        """ This method is used to download the dataset from GCS.

        Args:
            url (str): This is the downloader-uri that indicates where
                              the dataset should be downloaded from.

            local_path (str): This is the path to the directory where the
                          download will store the dataset.

            bucket (str): gcs bucket name
            key (str): object key path

            Examples:
                >>> url = "gs://bucket/folder or gs://bucket/folder/data.zip"
                >>> local_path = "/tmp/folder"
                >>> bucket ="bucket"
                >>> key ="folder/data.zip" or "folder"

        """
        if not (bucket and key) and url:
            bucket, key = self._parse(url)

        bucket_obj = self.client.get_bucket(bucket)
        if self._is_file(bucket_obj, key):
            self._download_file(bucket_obj, key, local_path)
        else:
            self._download_folder(bucket_obj, key, local_path)

    def _download_folder(self, bucket, key, local_path):
        """ download all files from directory
        """
        blobs = bucket.list_blobs(prefix=key)
        for blob in blobs:
            local_file_path = blob.name.replace(key, local_path)
            self._download_validate(blob, local_file_path)

    def _download_file(self, bucket, key, local_path):
        """ download single file
        """
        blob = bucket.get_blob(key)
        key_suffix = key.replace("/" + basename(key), "")
        local_file_path = blob.name.replace(key_suffix, local_path)
        self._download_validate(blob, local_file_path)

    def _download_validate(self, blob, local_file_path):
        """ download file and validate checksum
        """
        self._download_blob(blob, local_file_path)
        self._checksum(blob, local_file_path)

    def _download_blob(self, blob, local_file_path):
        """ download blob from gcs
        Raises:
            NotFound: This will raise when object not found
        """
        dst_dir = local_file_path.replace("/" + basename(local_file_path), "")
        key = blob.name
        if not isdir(dst_dir):
            makedirs(dst_dir)

        logger.info(f"Downloading from {key} to {local_file_path}.")
        blob.download_to_filename(local_file_path)

    def _checksum(self, blob, filename):
        """validate checksum and delete file if checksum does not match

        Raises:
            ChecksumError: This will raise this error if checksum doesn't
                           matches
        """
        expected_checksum = blob.md5_hash
        if expected_checksum:
            expected_checksum_hex = self._md5_hex(expected_checksum)
            try:
                validate_checksum(
                    filename, expected_checksum_hex, algorithm="MD5"
                )
            except ChecksumError as e:
                logger.exception(
                    "Checksum mismatch. Delete the downloaded files."
                )
                os.remove(filename)
                raise e

    def _is_file(self, bucket, key):
        """Check if the key is a file or directory"""
        blob = bucket.get_blob(key)
        return blob and blob.name == key

    def _md5_hex(self, checksum):
        """fix the missing padding if requires and converts into hex"""
        missing_padding = len(checksum) % 4
        if missing_padding != 0:
            checksum += "=" * (4 - missing_padding)
        return base64.b64decode(checksum).hex()

    def _parse(self, url):
        """Split an GCS-prefixed URL into bucket and path."""
        match = re.search(self.GCS_PREFIX, url)
        if not match:
            raise ValueError(
                f"Specified destination prefix: {url} does not start "
                f"with {self.GCS_PREFIX}"
            )
        url = url[len(self.GCS_PREFIX) - 1 :]
        if self.KEY_SEPARATOR not in url:
            raise ValueError(
                f"Specified destination prefix: {self.GCS_PREFIX + url} does "
                f"not have object key "
            )
        idx = url.index(self.KEY_SEPARATOR)
        bucket = url[:idx]
        path = url[(idx + 1) :]

        return bucket, path

    def upload(
        self, *, local_path=None, bucket=None, key=None, url=None, pattern="*"
    ):
        """ Upload a file or list of files from directory to GCS

            Args:
                url (str): This is the gcs location that indicates where
                the dataset should be uploaded.

                local_path (str): This is the path to the directory or file
                where the data is stored.

                bucket (str): gcs bucket name
                key (str): object key path
                pattern: Unix glob patterns. Use **/* for recursive glob.

                Examples:
                    For file upload:
                        >>> url = "gs://bucket/folder/data.zip"
                        >>> local_path = "/tmp/folder/data.zip"
                        >>> bucket ="bucket"
                        >>> key ="folder/data.zip"
                    For directory upload:
                        >>> url = "gs://bucket/folder"
                        >>> local_path = "/tmp/folder"
                        >>> bucket ="bucket"
                        >>> key ="folder"
                        >>> key ="**/*"

        """
        if not (bucket and key) and url:
            bucket, key = self._parse(url)

        bucket_obj = self.client.get_bucket(bucket)
        if isdir(local_path):
            self._upload_folder(
                local_path=local_path,
                bucket=bucket_obj,
                key=key,
                pattern=pattern,
            )
        else:
            self._upload_file(local_path=local_path, bucket=bucket_obj, key=key)

    def _upload_file(self, local_path=None, bucket=None, key=None):
        """ Upload a single object to GCS
        """
        blob = bucket.blob(key)
        logger.info(f"Uploading from {local_path} to {key}.")
        blob.upload_from_filename(local_path)

    def _upload_folder(
        self, local_path=None, bucket=None, key=None, pattern="*"
    ):
        """Upload all files from a folder to GCS based on pattern
        """
        for path in Path(local_path).glob(pattern):
            if path.is_dir():
                continue
            full_path = str(path)
            relative_path = str(path.relative_to(local_path))
            object_key = os.path.join(key, relative_path)
            self._upload_file(
                local_path=full_path, bucket=bucket, key=object_key
            )

    def get_most_recent_blob(self, url=None, bucket_name=None, key=None):
        """ Get the last updated blob in a given bucket under given prefix

            Args:
                bucket_name (str): gcs bucket name
                key (str): object key path
        """
        if not (bucket_name and key) and url:
            bucket_name, key = self._parse(url)

        bucket = self.client.get_bucket(bucket_name)

        if self._is_file(bucket, key):
            # Called on file, return file
            return bucket.get_blob(key)
        else:
            logger.debug(
                f"Cloud path not a file. Checking for most recent file in {url}"
            )
            # Return the blob with the max update time (most recent)
            blobs = self._list_blobs(bucket, prefix=key)
            return max(
                blobs, key=lambda blob: bucket.get_blob(blob.name).updated
            )

    def _list_blobs(self, bucket_name=None, prefix=None):
        """List all blobs with given prefix
        """
        blobs = self.client.list_blobs(bucket_name, prefix=prefix)
        blob_list = list(blobs)
        logger.debug(f"Blobs in {bucket_name} under prefix {prefix}:")
        logger.debug(blob_list)
        return blob_list
