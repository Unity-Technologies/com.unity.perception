# Dataset Insights

[![PyPI python](https://img.shields.io/pypi/pyversions/datasetinsights)](https://pypi.org/project/datasetinsights)
[![PyPI version](https://badge.fury.io/py/datasetinsights.svg)](https://pypi.org/project/datasetinsights)
[![Downloads](https://pepy.tech/badge/datasetinsights)](https://pepy.tech/project/datasetinsights)
[![Tests](https://github.com/Unity-Technologies/datasetinsights/actions/workflows/linting-and-unittests.yaml/badge.svg?branch=master&event=push)](https://github.com/Unity-Technologies/datasetinsights/actions/workflows/linting-and-unittests.yaml?query=branch%3Amaster+event%3Apush)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

Unity Dataset Insights is a python package for downloading, parsing and analyzing synthetic datasets generated using the Unity [Perception package](https://github.com/Unity-Technologies/com.unity.perception).

## Installation

Dataset Insights maintains a pip package for easy installation. It can work in any standard Python environment using `pip install datasetinsights` command.

## Getting Started

### Dataset Statistics

We provide a sample [notebook](notebooks/Perception_Statistics.ipynb) to help you load synthetic datasets generated using [Perception package](https://github.com/Unity-Technologies/com.unity.perception) and visualize dataset statistics. We plan to support other sample Unity projects in the future.

### Dataset Download

You can download the datasets from HTTP(s), GCS, and Unity simulation projects using the 'download' command from CLI or API.

[CLI](https://datasetinsights.readthedocs.io/en/latest/datasetinsights.commands.html#datasetinsights-commands-download)

```bash
datasetinsights download \
  --source-uri=<xxx> \
  --output=$HOME/data
```
[Programmatically](https://datasetinsights.readthedocs.io/en/latest/datasetinsights.io.downloader.html#module-datasetinsights.io.downloader.gcs_downloader)

UnitySimulationDownloader downloads a dataset from Unity Simulation.

```python3
from datasetinsights.io.downloader import UnitySimulationDownloader

source_uri=usim://<project_id>/<run_execution_id>
dest = "~/data"
access_token = "XXX"
downloader = UnitySimulationDownloader(access_token=access_token)
downloader.download(source_uri=source_uri, output=dest)
```
GCSDatasetDownloader downloads a dataset from GCS location.
```python3
from datasetinsights.io.downloader import GCSDatasetDownloader

source_uri=gs://url/to/file.zip or gs://url/to/folder
dest = "~/data"
downloader = GCSDatasetDownloader()
downloader.download(source_uri=source_uri, output=dest)
```
HTTPDatasetDownloader downloads a dataset from any HTTP(S) location.
```python3
from datasetinsights.io.downloader import HTTPDatasetDownloader

source_uri=http://url.to.file.zip
dest = "~/data"
downloader = HTTPDatasetDownloader()
downloader.download(source_uri=source_uri, output=dest)
```
### Dataset Explore
You can explore the dataset [schema](https://datasetinsights.readthedocs.io/en/latest/Synthetic_Dataset_Schema.html#synthetic-dataset-schema) by using following API:

[Unity Perception](https://datasetinsights.readthedocs.io/en/latest/datasetinsights.datasets.unity_perception.html#datasetinsights-datasets-unity-perception)

AnnotationDefinitions and MetricDefinitions loads synthetic dataset definition tables and return a dictionary containing the definitions.

```python3
from datasetinsights.datasets.unity_perception import AnnotationDefinitions,
MetricDefinitions
annotation_def = AnnotationDefinitions(data_root=dest, version="my_schema_version")
definition_dict = annotation_def.get_definition(def_id="my_definition_id")

metric_def = MetricDefinitions(data_root=dest, version="my_schema_version")
definition_dict = metric_def.get_definition(def_id="my_definition_id")
```
Captures loads synthetic dataset captures tables and return a pandas dataframe with captures and annotations columns.

```python3
from datasetinsights.datasets.unity_perception import Captures
captures = Captures(data_root=dest, version="my_schema_version")
captures_df = captures.filter(def_id="my_definition_id")
```
Metrics loads synthetic dataset metrics table which holds extra metadata that can be used to describe a particular sequence, capture or annotation and return a pandas dataframe with captures and metrics columns.

```python3
from datasetinsights.datasets.unity_perception import Metrics
metrics = Metrics(data_root=dest, version="my_schema_version")
metrics_df = metrics.filter_metrics(def_id="my_definition_id")
```

## Docker

You can use the pre-build docker image [unitytechnologies/datasetinsights](https://hub.docker.com/r/unitytechnologies/datasetinsights) to run similar commands.

## Documentation

You can find the API documentation on [readthedocs](https://datasetinsights.readthedocs.io/en/latest/).

## Contributing

Please let us know if you encounter a bug by filing an issue. To learn more about making a contribution to Dataset Insights, please see our Contribution [page](CONTRIBUTING.md).

## License

Dataset Insights is licensed under the Apache License, Version 2.0. See [LICENSE](LICENCE) for the full license text.

## Citation
If you find this package useful, consider citing it using:
```
@misc{datasetinsights2020,
    title={Unity {D}ataset {I}nsights Package},
    author={{Unity Technologies}},
    howpublished={\url{https://github.com/Unity-Technologies/datasetinsights}},
    year={2020}
}
```
