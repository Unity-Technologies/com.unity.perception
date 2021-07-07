"""Load Synthetic dataset Metrics
"""
import json

import dask.bag as db

from datasetinsights.constants import DEFAULT_DATA_ROOT

from .exceptions import DefinitionIDError
from .tables import DATASET_TABLES, SCHEMA_VERSION, glob
from .validation import verify_version


class Metrics:
    """Load metrics table

    Metrics store extra metadata that can be used to describe a particular
    sequence, capture or annotation. Metric records are stored as arbitrary
    number (M) of key-value pairs.
    For more detail, see schema design doc:
    :ref:`metrics`

    Attributes:
        metrics (dask.bag.core.Bag): a collection of metrics records
    Examples:
        >>> metrics = Metrics(data_root="/data")
        >>> metrics_df = metrics.filter_metrics(def_id="my_definition_id")
        #metrics_df now contains all the metrics data corresponding to
        "my_definition_id"

        One example of metrics_df (first row shown below):

        +---------------+------------------+---------------------+
        | label_id(int) | instance_id(int) | visible_pixels(int) |
        +===============+==================+=====================+
        | 2             | 2                | 2231                |
        +---------------+------------------+---------------------+

    """

    TABLE_NAME = "metrics"
    FILE_PATTERN = DATASET_TABLES[TABLE_NAME].file

    def __init__(self, data_root=DEFAULT_DATA_ROOT, version=SCHEMA_VERSION):
        """ Initialize Metrics

        Args:
            data_root (str): the root directory of the dataset containing
            metrics
            version (str): desired schema version
        """
        self.metrics = self._load_metrics(data_root, version)

    def _load_metrics(self, data_root, version):
        """
        `:ref:`metrics`


        Args:
            data_root: (str): the root directory of the dataset containing
            metrics
            version (str): desired schema version

        Returns:
            dask.bag.core.Bag
        """
        metrics_files = db.from_sequence(glob(data_root, self.FILE_PATTERN))
        metrics = metrics_files.map(
            lambda path: Metrics._load_json(path, self.TABLE_NAME, version)
        ).flatten()

        return metrics

    @staticmethod
    def _normalize_values(metric):
        """ Filter unnecessary info from metric.
        1-level faltten of metrics.values column.
        """
        values = metric["values"]
        for value in values:
            value["capture_id"] = metric["capture_id"]
            value["annotation_id"] = metric["annotation_id"]
            value["sequence_id"] = metric["sequence_id"]
            value["step"] = metric["step"]

        return values

    def filter_metrics(self, def_id):
        """Get all metrics filtered by a given metric definition id

        Args:
            def_id (str): metric definition id used to filter results
        Raises:
            DefinitionIDError: raised if no metrics records match the given
            def_id
        Returns (pd.DataFrame):
        Columns: "label_id", "capture_id", "annotation_id", "sequence_id",
        "step"
        """
        metrics = (
            self.metrics.filter(
                lambda metric: metric["metric_definition"] == def_id
            )
            .map(Metrics._normalize_values)
            .flatten()
        )
        if metrics.count().compute() == 0:
            msg = (
                f"Can't find metrics records associated with the given "
                f"definition id {def_id}."
            )
            raise DefinitionIDError(msg)

        return metrics.to_dataframe().compute()

    @staticmethod
    def _load_json(filename, table_name, version):
        """Load records from json files into a dict
        """
        with open(filename, "r") as file:
            data = json.load(file)
        verify_version(data, version)

        return data[table_name]
