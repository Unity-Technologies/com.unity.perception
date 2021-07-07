"""UnitySimulationDownloader downloads a dataset from Unity Simulation"""
import concurrent.futures
import logging
import os
import re
from pathlib import Path

import numpy as np
import pandas as pd
import requests
from codetiming import Timer
from requests.packages.urllib3.util.retry import Retry
from tqdm import tqdm

import datasetinsights.constants as const
from datasetinsights.datasets.unity_perception.tables import (
    DATASET_TABLES,
    FileType,
)
from datasetinsights.io.download import TimeoutHTTPAdapter, download_file
from datasetinsights.io.downloader.base import DatasetDownloader
from datasetinsights.io.exceptions import DownloadError

# number of workers for ThreadPoolExecutor. This is the default value
# in python3.8
MAX_WORKER = min(32, os.cpu_count() + 4)
# Timeout of requests (in seconds)
DEFAULT_TIMEOUT = 1800
# Retry after failed request
DEFAULT_MAX_RETRIES = 5


logger = logging.getLogger(__name__)


class UnitySimulationDownloader(DatasetDownloader, protocol="usim://"):
    """ This class is used to download data from Unity Simulation

        For more on Unity Simulation please see these
        `docs <https://github.com/Unity-Technologies/Unity-Simulation-Docs>`

        Args:
            access_token (str): Access token to be used to authenticate to
                unity simulation for downloading the dataset

    """

    SOURCE_URI_PATTERN = r"usim://([^@]*)?@?([a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12})/(\w+)"  # noqa: E501

    def __init__(self, access_token=None, **kwargs):
        super().__init__(**kwargs)
        self.access_token = access_token
        self.run_execution_id = None
        self.project_id = None

    def download(self, source_uri, output, include_binary=False, **kwargs):
        """ Download from Unity Simulation

        Args:
            source_uri: This is the downloader-uri that indicates where on
                unity simulation the dataset should be downloaded from.
                The expected source-uri should follow these patterns:
                usim://access-token@project-id/run-execution-id
                or
                usim://project-id/run-execution-id
            output: This is the path to the directory where the download
                method will store the dataset.
            include_binary: Whether to download binary files such as images
                or LIDAR point clouds. This flag applies to Datasets where
                metadata (e.g. annotation json, dataset catalog, ...)
                can be separated from binary files.

        """
        self.parse_source_uri(source_uri)
        manifest_file = os.path.join(output, f"{self.run_execution_id}.csv")
        manifest_file = download_manifest(
            self.run_execution_id,
            manifest_file,
            self.access_token,
            project_id=self.project_id,
        )

        dl_worker = Downloader(manifest_file, output)
        dl_worker.download_references()
        dl_worker.download_metrics()
        dl_worker.download_captures()
        if include_binary:
            dl_worker.download_binary_files()

    def parse_source_uri(self, source_uri):
        """ Parse unity simulation source uri

        Args:
            source_uri: Parses source-uri in the following format
            usim://access-token@project-id/run-execution-id
            or
            usim://project-id/run-execution-id

        """
        pattern = re.compile(self.SOURCE_URI_PATTERN)
        result = pattern.findall(source_uri)
        if len(result) == 1:
            (access_token, project_id, run_execution_id,) = pattern.findall(
                source_uri
            )[0]
            if not self.access_token:
                if access_token:
                    self.access_token = access_token
                else:
                    raise ValueError(f"Missing access token")
            if project_id:
                self.project_id = project_id
            if run_execution_id:
                self.run_execution_id = run_execution_id

        else:
            raise ValueError(
                f"{source_uri} needs to be in format"
                f" usim://access_token@project_id/run_execution_id "
                f"or usim://project_id/run_execution_id "
            )


def _filter_unsuccessful_attempts(manifest_df):
    """
    remove all rows from a dataframe where a greater attempt_id exists for
    the 'instance_id'. This is necessary so that we avoid using data from
    a failed USim run and only get the most recent retry.
    Args:
        manifest_df (pandas df): must have columns 'attempt_id', 'app_param_id'
        and 'instance_id'

    Returns(pandas df): where all rows for earlier attempt ids have been
    removed

    """
    last_attempt_per_instance = manifest_df.groupby("instance_id")[
        "attempt_id"
    ].agg(["max"])
    merged = manifest_df.merge(
        how="outer",
        right=last_attempt_per_instance,
        left_on="instance_id",
        right_on="instance_id",
    )
    filtered = merged[merged["attempt_id"] == merged["max"]]
    filtered = filtered.reset_index(drop=True)
    filtered = filtered.drop(columns="max")
    return filtered


class Downloader:
    """Parse a given manifest file to download simulation output

    For more on Unity Simulation please see these
    `docs <https://github.com/Unity-Technologies/Unity-Simulation-Docs>`_

    Attributes:
        manifest (DataFrame): the csv manifest file stored in a pandas dataframe
        data_root (str): root directory where the simulation output should
            be downloaded
    """

    MANIFEST_FILE_COLUMNS = (
        "run_execution_id",
        "app_param_id",
        "instance_id",
        "attempt_id",
        "file_name",
        "download_uri",
    )

    def __init__(self, manifest_file: str, data_root: str):
        """ Initialize Downloader

        Args:
            manifest_file (str): path to a manifest file
            data_root (str): root directory where the simulation output should
                be downloaded
        """
        self.manifest = pd.read_csv(
            manifest_file, header=0, names=self.MANIFEST_FILE_COLUMNS
        )
        self.manifest = _filter_unsuccessful_attempts(manifest_df=self.manifest)
        self.manifest["filetype"] = self.match_filetypes(self.manifest)
        self.data_root = data_root

    @staticmethod
    def match_filetypes(manifest):
        """ Match filetypes for every rows in the manifest file.

        Args:
            manifest (pd.DataFrame): the manifest csv file

        Returns:
            a list of filetype strings
        """
        filenames = manifest.file_name
        filetypes = []
        for name in filenames:
            for _, table in DATASET_TABLES.items():
                if re.match(table.pattern, name):
                    filetypes.append(table.filetype)
                    break
            else:
                filetypes.append(FileType.BINARY)

        return filetypes

    @Timer(name="download_all", text=const.TIMING_TEXT, logger=logging.info)
    def download_all(self):
        """ Download all files in the manifest file.
        """
        matched_rows = np.ones(len(self.manifest), dtype=bool)
        downloaded = self._download_rows(matched_rows)
        logger.info(
            f"Total {len(downloaded)} files in manifest are successfully "
            f"downloaded."
        )

    @Timer(
        name="download_references", text=const.TIMING_TEXT, logger=logging.info
    )
    def download_references(self):
        """ Download all reference files.
        All reference tables are static tables during the simulation.
        This typically comes from the definition of the simulation and should
        be created before tasks running distributed at different instances.
        """
        logger.info("Downloading references files...")
        matched_rows = self.manifest.filetype == FileType.REFERENCE
        downloaded = self._download_rows(matched_rows)

        logger.info(
            f"Total {len(downloaded)} reference files are successfully "
            f"downloaded."
        )

    @Timer(name="download_metrics", text=const.TIMING_TEXT, logger=logging.info)
    def download_metrics(self):
        """ Download all metrics files.
        """
        logger.info("Downloading metrics files...")
        matched_rows = self.manifest.filetype == FileType.METRIC
        downloaded = self._download_rows(matched_rows)
        logger.info(
            f"Total {len(downloaded)} metric files are successfully downloaded."
        )

    @Timer(
        name="download_captures", text=const.TIMING_TEXT, logger=logging.info
    )
    def download_captures(self):
        """ Download all captures files. See :ref:`captures`
        """
        logger.info("Downloading captures files...")
        matched_rows = self.manifest.filetype == FileType.CAPTURE
        downloaded = self._download_rows(matched_rows)
        logger.info(
            f"Total {len(downloaded)} capture files are successfully "
            f"downloaded."
        )

    @Timer(
        name="download_binary_files",
        text=const.TIMING_TEXT,
        logger=logging.info,
    )
    def download_binary_files(self):
        """ Download all binary files.
        """
        logger.info("Downloading binary files...")
        matched_rows = self.manifest.filetype == FileType.BINARY
        downloaded = self._download_rows(matched_rows)
        logger.info(
            f"Total {len(downloaded)} binary files are successfully "
            f"downloaded."
        )

    def _download_rows(self, matched_rows):
        """ Download matched rows in a manifest file.

        Note:
        We might need to download 1M+ of simulation output files, in this case
        we don't want to have a single file transfer failure holding back on
        getting the simulation data. Here download exception are captured.
        We only log an error message and requires uses to pay attention to
        this error.

        Args:
            matched_rows (pd.Series): boolean series indicator of the manifest
                file that should be downloaded

        Returns:
            list of strings representing the downloaded destination path.
        """
        n_expected = sum(matched_rows)
        future_downloaded = []
        downloaded = []
        with concurrent.futures.ThreadPoolExecutor(MAX_WORKER) as executor:
            for _, row in self.manifest[matched_rows].iterrows():
                source_uri = row.download_uri
                relative_path = Path(self.data_root, row.file_name)
                dest_path = relative_path.parent
                file_name = relative_path.name
                future = executor.submit(
                    download_file, source_uri, dest_path, file_name
                )
                future_downloaded.append(future)

            for future in tqdm(
                concurrent.futures.as_completed(future_downloaded),
                total=n_expected,
            ):
                try:
                    downloaded.append(future.result())
                except DownloadError as ex:
                    logger.error(ex)

        n_downloaded = len(downloaded)
        if n_downloaded != n_expected:
            logger.warning(
                f"Found {n_expected} matching records in the manifest file, "
                f"but only {n_downloaded} are downloaded."
            )

        return downloaded


def download_manifest(
    run_execution_id, manifest_file, access_token, project_id, use_cache=True
):
    """ Download manifest file from a single run_execution_id
    For more on Unity Simulation see these
    `docs <https://github.com/Unity-Technologies/Unity-Simulation-Docs>`_


    Args:
        run_execution_id (str): Unity Simulation run execution id
        manifest_file (str): path to the destination of the manifest_file
        access_token (str): short lived authorization token
        project_id (str): Unity project id that has Unity Simulation enabled
        use_cache (bool, optional): indicator to skip download if manifest
            file already exists. Default: True.

    Returns:
        str: Full path to the manifest_file
    """
    api_endpoint = const.USIM_API_ENDPOINT
    project_url = f"{api_endpoint}/v1/projects/{project_id}/"
    data_url = f"{project_url}runs/{run_execution_id}/data"
    if Path(manifest_file).exists() and use_cache:
        logger.info(
            f"Mainfest file {manifest_file} already exists. Skipping downloads."
        )
        return manifest_file

    logger.info(
        f"Trying to download manifest file for run-execution-id "
        f"{run_execution_id}"
    )
    adapter = TimeoutHTTPAdapter(
        timeout=DEFAULT_TIMEOUT, max_retries=Retry(total=DEFAULT_MAX_RETRIES)
    )
    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json",
    }
    with requests.Session() as http:
        http.mount("https://", adapter)
        try:
            resp = http.get(data_url, headers=headers)
            resp.raise_for_status()
        except requests.exceptions.RequestException as ex:
            logger.error(ex)
            err_msg = (
                f"Failed to download manifest file for run-execution-id: "
                f"{run_execution_id}."
            )
            raise DownloadError(err_msg)
        else:
            Path(manifest_file).parent.mkdir(parents=True, exist_ok=True)
            with open(manifest_file, "wb") as f:
                for chunk in resp.iter_content(chunk_size=1024):
                    f.write(chunk)

    logger.info(
        f"Manifest file {manifest_file} downloaded for run-execution-id "
        f"{run_execution_id}"
    )

    return manifest_file
