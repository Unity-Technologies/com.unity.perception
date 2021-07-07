import json
import logging
import pathlib
from collections import namedtuple
from enum import Enum

import pandas as pd

from .validation import verify_version

logger = logging.getLogger(__name__)
SCHEMA_VERSION = "0.0.1"  # Synthetic dataset schema version


class FileType(Enum):
    BINARY = "binary"
    REFERENCE = "reference"
    METRIC = "metric"
    CAPTURE = "capture"


Table = namedtuple("Table", "file pattern filetype")
DATASET_TABLES = {
    "annotation_definitions": Table(
        "**/annotation_definitions.json",
        r"(?:\w|-|/)*annotation_definitions.json",
        FileType.REFERENCE,
    ),
    "captures": Table(
        "**/captures_*.json",
        r"(?:\w|-|/)*captures_[0-9]+.json",
        FileType.CAPTURE,
    ),
    "egos": Table("**/egos.json", r"(?:\w|-|/)*egos.json", FileType.REFERENCE),
    "metric_definitions": Table(
        "**/metric_definitions.json",
        r"(?:\w|-|/)*metric_definitions.json",
        FileType.REFERENCE,
    ),
    "metrics": Table(
        "**/metrics_*.json", r"(?:\w|-|/)*metrics_[0-9]+.json", FileType.METRIC
    ),
    "sensors": Table(
        "**/sensors.json", r"(?:\w|-|/)*sensors.json", FileType.REFERENCE
    ),
}


def glob(data_root, pattern):
    """Find all matching files in a directory.

    Args:
        data_root (str): directory containing capture files
        pattern (str): Unix file pattern

    Yields:
        str: matched filenames in a directory
    """
    path = pathlib.Path(data_root)
    for fp in path.glob(pattern):
        yield fp


def load_table(json_file, table_name, version, **kwargs):
    """Load records from json files into a pandas table

    Args:
        json_file (str): filename to json.
        table_name (str): table name in the json file to be loaded
        version (str): requested version of this table
        **kwargs: arbitrary keyword arguments to be passed to pandas'
            json_normalize method.

    Returns:
        a pandas dataframe of the loaded table.

    Raises:
        VersionError: If the version in json file does not match the requested
        version.
    """
    logger.debug(f"Loading table {table_name} from {json_file}")
    data = json.load(open(json_file, "r"))
    verify_version(data, version)
    table = pd.json_normalize(data[table_name], **kwargs)

    return table
