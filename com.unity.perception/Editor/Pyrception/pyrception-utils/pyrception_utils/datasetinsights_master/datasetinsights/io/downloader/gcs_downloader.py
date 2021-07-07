from datasetinsights.io.downloader.base import DatasetDownloader
from datasetinsights.io.gcs import GCSClient


class GCSDatasetDownloader(DatasetDownloader, protocol="gs://"):
    """ This class is used to download data from GCS
    """

    def __init__(self, **kwargs):
        """ initiating GCSDownloader
        """
        self.client = GCSClient()

    def download(self, source_uri=None, output=None, **kwargs):
        """

        Args:
            source_uri: This is the downloader-uri that indicates where on
                GCS the dataset should be downloaded from.
                The expected source-uri follows these patterns
                gs://bucket/folder or gs://bucket/folder/data.zip

            output: This is the path to the directory
                where the download will store the dataset.
        """
        self.client.download(local_path=output, url=source_uri)
