import logging

import datasetinsights.constants as const
from datasetinsights.datasets.unity_perception import MetricDefinitions, Metrics
from datasetinsights.datasets.unity_perception.tables import SCHEMA_VERSION

logger = logging.getLogger(__name__)


class RenderedObjectInfo:
    """Rendered Object Info in Captures

    This metric stores common object info captured by a sensor in the simulation
    environment. It can be used to calculate object statistics such as
    object count, object rotation and visible pixels.

    Attributes:
        raw_table (pd.DataFrame): rendered object info stored with a tidy
        pandas dataframe. Columns "label_id", "instance_id", "visible_pixels",
        "capture_id, "label_name".

    Examples:

    .. code-block:: python

        >>> # set the data root path to where data was stored
        >>> data_root = "$HOME/data"
        >>> # use rendered object info definition id
        >>> definition_id = "659c6e36-f9f8-4dd6-9651-4a80e51eabc4"
        >>> roinfo = RenderedObjectInfo(data_root, definition_id)
        #total object count per label dataframe
        >>> roinfo.total_counts()
        label_id label_name count
               1    object1    10
               2    object2    21
        #object count per capture dataframe
        >>> roinfo.per_capture_counts()
        capture_id  count
            qwerty     10
            asdfgh     21
    """

    LABEL = "label_id"
    LABEL_READABLE = "label_name"
    INDEX_COLUMN = "capture_id"
    VALUE_COLUMN = "values"
    COUNT_COLUMN = "count"

    def __init__(
        self,
        data_root=const.DEFAULT_DATA_ROOT,
        version=SCHEMA_VERSION,
        def_id=None,
    ):
        """Initialize RenderedObjectInfo

        Args:
            data_root (str): root directory where the dataset was stored
            version (str): synthetic dataset schema version
            def_id (str): rendered object info definition id
        """
        filtered_metrics = Metrics(data_root, version).filter_metrics(def_id)
        label_mappings = self._read_label_mappings(data_root, version, def_id)
        self.raw_table = self._read_filtered_metrics(
            filtered_metrics, label_mappings
        )

    def num_captures(self):
        """Total number of captures

        Returns:
            integer: Total number of captures
        """
        return self.raw_table[self.INDEX_COLUMN].nunique()

    @staticmethod
    def _read_label_mappings(data_root, version, def_id):
        """Read label_mappings from a metric_definition record.

        Args:
            data_root (str): root directory where the dataset was stored
            version (str): synthetic dataset schema version
            def_id (str): rendered object info definition id

        Returns:
            dict: The mappings of {label_id: label_name}
        """
        definition = MetricDefinitions(data_root, version).get_definition(
            def_id
        )
        name = RenderedObjectInfo.LABEL
        readable_name = RenderedObjectInfo.LABEL_READABLE

        return {d[name]: d[readable_name] for d in definition["spec"]}

    @staticmethod
    def _read_filtered_metrics(filtered_metrics, label_mappings):
        """Read label_mappings from a metric_definition record.

        Args:
            filtered_metrics (pd.DataFrame): A pandas dataframe for metrics
                filtered by definition id.
            label_mappings (dict): the mappings of {label_id: label_name}

        Returns:
            pd.DataFrame: rendered object info stored with a tidy
            pandas dataframe. Columns "label_id", "instance_id",
            "visible_pixels", "capture_id, "label_name".
        """
        filtered_metrics[RenderedObjectInfo.LABEL_READABLE] = filtered_metrics[
            RenderedObjectInfo.LABEL
        ].map(label_mappings)
        # Remove metrics data not defined in label_mappings
        filtered_metrics.dropna(
            subset=[RenderedObjectInfo.LABEL_READABLE], inplace=True
        )

        return filtered_metrics

    def total_counts(self):
        """Aggregate Total Object Counts Per Label

        Returns:
            pd.DataFrame: Total object counts table.
                Columns "label_id", "label_name", "count"
        """
        agg = (
            self.raw_table.groupby([self.LABEL, self.LABEL_READABLE])
            .size()
            .to_frame(name=self.COUNT_COLUMN)
            .reset_index()
        )

        return agg

    def per_capture_counts(self):
        """ Aggregate Object Counts Per Label

        Returns:
            pd.DataFrame: Total object counts table.
                Columns "capture_id", "count"
        """
        agg = (
            self.raw_table.groupby(self.INDEX_COLUMN)
            .size()
            .to_frame(name=self.COUNT_COLUMN)
            .reset_index()
        )

        return agg
