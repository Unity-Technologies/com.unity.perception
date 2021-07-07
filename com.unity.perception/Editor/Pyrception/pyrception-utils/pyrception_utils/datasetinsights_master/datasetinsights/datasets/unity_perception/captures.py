""" Load Synthetic dataset captures and annotations tables
"""
import pandas as pd

from datasetinsights.constants import DEFAULT_DATA_ROOT

from .exceptions import DefinitionIDError
from .tables import DATASET_TABLES, SCHEMA_VERSION, glob, load_table


class Captures:
    """Load captures table

    A capture record stores the relationship between a captured file,
    a collection of annotations, and extra metadata that describes this
    capture. For more detail, see schema design here:

    :ref:`captures`

    Examples:

    .. code-block:: python

        >>> captures = Captures(data_root="/data")
        #captures class automatically loads the captures (e.g. lidar scan,
        image, depth map) and the annotations (e.g semantic segmentation
        labels, bounding boxes, etc.)
        >>> data = captures.filter(def_id="6716c783-1c0e-44ae-b1b5-7f068454b66e") # noqa E501 table command not be broken down into multiple lines
        #return the captures and annotations filtered by the annotation
        definition id

    Attributes:
        captures (pd.DataFrame): a collection of captures without annotations
        annotations (pd.DataFrame): a collection of annotations
    """

    TABLE_NAME = "captures"
    FILE_PATTERN = DATASET_TABLES[TABLE_NAME].file

    def __init__(self, data_root=DEFAULT_DATA_ROOT, version=SCHEMA_VERSION):
        """ Initialize Captures

        Args:
            data_root (str): the root directory of the dataset
            version (str): desired schema version
        """
        self.captures = self._load_captures(data_root, version)
        self.annotations = self._load_annotations(data_root, version)

    def _load_captures(self, data_root, version):
        """Load captures except annotations.
        :ref:`captures`

        Args:
            data_root (str): the root directory of the dataset
            version (str): desired schema version

        Returns:
            A pandas dataframe with combined capture records.
            Columns: 'id' (UUID of the capture), 'sequence_id',
            'step' (index of captures), 'timestamp' (Simulation timestamp in
            milliseconds since the sequence started.), 'sensor'
            (sensor attributes), 'ego' (ego pose of the simulation),
            'filename' (single filename that stores captured data)

        Example Captures DataFrame:
         id(str)      sequence_id(str)  step(int)    timestamp(float) \
         cdc8bc5c...  2954c...          300           4.979996

        sensor (dict) \
        {'sensor_id': 'da873b...', 'ego_id': '44ca9...', 'modality': 'camera',
        'translation': [0.0, 0.0, 0.0], 'rotation': [0.0, 0.0, 0.0, 1.0],
        'scale': 0.344577253}


         ego (dict)  \
        {'ego_id': '44ca9...', 'translation': [0.0, 0.0, -20.0],
        'rotation': [0.0, 0.0, 0.0, 1.0], 'velocity': None,
        'acceleration': None}

        filename (str)          format (str)
        RGB3/rgb_30...           PNG

        """
        captures = []
        for c_file in glob(data_root, self.FILE_PATTERN):
            capture = load_table(c_file, self.TABLE_NAME, version, max_level=0)
            if "annotations" in capture.columns:
                capture.drop(columns="annotations")

            captures.append(capture)

        # pd.concat might create memory bottleneck
        return pd.concat(captures, axis=0)

    def _load_annotations(self, data_root, version):
        """Load annotations and capture IDs.
        :ref:`capture-annotation`

        Args:
            data_root (str): the root directory of the dataset
            version (str): desired schema version

        Returns:
            A pandas dataframe with combined annotation records
            Columns: 'id' (annotation id), 'annotation_definition' (annotation
            definition ID),
             'values'
             (list of objects that store annotation data, e.g. 2d bounding
             box), 'capture.id'

        Example Annotation Dataframe:

        id(str)	annotation_definition(str)	\
        ace0...	6716c...

        values (dict) \
        [{'label_id': 34, 'label_name': 'snack_chips_pringles',
        ...'height': 118.0}, {'label_id': 35, '... 'height': 91.0}...]

        capture.id (str)
        cdc8b...
      """
        annotations = []
        for c_file in glob(data_root, self.FILE_PATTERN):
            try:
                annotation = load_table(
                    c_file,
                    self.TABLE_NAME,
                    version,
                    record_path="annotations",
                    meta="id",
                    meta_prefix="capture.",
                )
            except KeyError:
                annotation = pd.DataFrame(
                    {"annotation_definition": [], "capture.id": []}
                )

            annotations.append(annotation)

        return pd.concat(annotations, axis=0)

    def filter(self, def_id):
        """Get captures and annotations filtered by annotation definition id
        :ref:`captures`

        Args:
            def_id (int): annotation definition id used to filter results

        Returns:
            A pandas dataframe with captures and annotations
            Columns: 'id' (capture id), 'sequence_id', 'step', 'timestamp',
            'sensor', 'ego',
             'filename', 'format', 'annotation.id',
             'annotation.annotation_definition','annotation.values'

        Raises:
            DefinitionIDError: Raised if none of the annotation records in the
                combined annotation and captures dataframe match the def_id
                specified as a parameter.

        Example Returned Dataframe (first row):


        +---------------+------------------+-----------+-------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+---------------+--------------+---------------------+---------------------------------------+-----------------------------------------------------------------------------------------------------------------------+
        | label_id(int) | sequence_id(str) | step(int) | timestamp (float) | sensor (dict)                                                                                                                                                 | ego (dict) | filename(str) | format (str) | annotation.id (str) | annotation.annotation_definition(str) | annotation.values                                                                                                     |
        +===============+==================+===========+===================+===============================================================================================================================================================+============+===============+==============+=====================+=======================================+=======================================================================================================================+
        | 2             | None             | 50        | 4.9               | {'sensor_id': 'dDa873b...', 'ego_id': '44ca9...', 'modality': 'camera','translation': [0.0, 0.0, 0.0], 'rotation': [0.0, 0.0, 0.0, 1.0],'scale': 0.344577253} | ...        | RGB3/asd.png  | PNG          | ace0                | 6716c                                 | [{'label_id': 34, 'label_name': 'snack_chips_pringles',...'height': 118.0}, {'label_id': 35, '... 'height': 91.0}...] |
        +---------------+------------------+-----------+-------------------+---------------------------------------------------------------------------------------------------------------------------------------------------------------+------------+---------------+--------------+---------------------+---------------------------------------+-----------------------------------------------------------------------------------------------------------------------+

        """  # noqa: E501 table should not be broken down into multiple lines
        if self.annotations.empty:
            msg = (
                f"Can't find annotations records associate with the given "
                f"definition id {def_id}."
            )
            raise DefinitionIDError(msg)

        mask = self.annotations.annotation_definition == def_id
        annotations = (
            self.annotations[mask]
            .set_index("capture.id")
            .add_prefix("annotation.")
        )
        captures = self.captures.set_index("id")

        combined = (
            captures.join(annotations, how="inner")
            .reset_index()
            .rename(columns={"index": "id"})
        )

        if combined.empty:
            msg = (
                f"Can't find annotations records associate with the given "
                f"definition id {def_id}."
            )
            raise DefinitionIDError(msg)

        return combined
