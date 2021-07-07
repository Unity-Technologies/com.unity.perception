""" Validate Simulation Data
"""


class VersionError(Exception):
    """Raise when the data file version does not match"""

    pass


class DuplicateRecordError(Exception):
    """ Raise when the definition file has duplicate definition id
    """

    pass


class NoRecordError(Exception):
    """ Raise when no record is found matching a given definition id
    """

    pass


def verify_version(json_data, version):
    """Verify json schema version

    Args:
        json_data (json): a json object loaded from file.
        version (str): string of the requested version.

    Raises:
        VersionError: If the version in json file does not match the requested
    version.
    """
    loaded = json_data["version"]
    if loaded != version:
        raise VersionError(f"Version mismatch. Expected version: {version}")


def check_duplicate_records(table, column, table_name):
    """ Check if table has duplicate records for a given column

    Args:
        table (pd.DataFrame): a pandas dataframe
        column (str): the column where no duplication is allowed
        table_name (str): table name

    Raises:
        DuplicateRecordError: If duplicate records are found in a column
    """
    if table[column].nunique() != len(table):
        raise DuplicateRecordError(
            f"Duplicate record was found in {column} of table {table_name}. "
            f"This column is expected to be unique. Violating this requirement "
            f"might cause ambiguity when the records are loaded."
        )
