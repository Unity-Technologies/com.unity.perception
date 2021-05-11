# Table of Contents

* [pyrception\_utils](#pyrception_utils)
* [pyrception\_utils.cli](#pyrception_utils.cli)
  * [preview](#pyrception_utils.cli.preview)
* [pyrception\_utils.preview](#pyrception_utils.preview)
  * [list\_datasets](#pyrception_utils.preview.list_datasets)
  * [frame\_selector\_ui](#pyrception_utils.preview.frame_selector_ui)
  * [draw\_image\_with\_boxes](#pyrception_utils.preview.draw_image_with_boxes)
  * [load\_perception\_dataset](#pyrception_utils.preview.load_perception_dataset)
  * [preview\_dataset](#pyrception_utils.preview.preview_dataset)
  * [preview\_app](#pyrception_utils.preview.preview_app)
* [pyrception\_utils.pyrception](#pyrception_utils.pyrception)
  * [FileType](#pyrception_utils.pyrception.FileType)
  * [glob](#pyrception_utils.pyrception.glob)
  * [file\_number](#pyrception_utils.pyrception.file_number)
  * [glob\_list](#pyrception_utils.pyrception.glob_list)
  * [load\_json](#pyrception_utils.pyrception.load_json)
  * [PyrceptionDatasetMetadata](#pyrception_utils.pyrception.PyrceptionDatasetMetadata)
    * [\_\_init\_\_](#pyrception_utils.pyrception.PyrceptionDatasetMetadata.__init__)
  * [PyrceptionDataset](#pyrception_utils.pyrception.PyrceptionDataset)
    * [\_\_init\_\_](#pyrception_utils.pyrception.PyrceptionDataset.__init__)
    * [\_\_getitem\_\_](#pyrception_utils.pyrception.PyrceptionDataset.__getitem__)
    * [\_\_len\_\_](#pyrception_utils.pyrception.PyrceptionDataset.__len__)
* [pyrception\_utils.pyrception\_gcs](#pyrception_utils.pyrception_gcs)
  * [FileType](#pyrception_utils.pyrception_gcs.FileType)
  * [glob](#pyrception_utils.pyrception_gcs.glob)
  * [glob\_list](#pyrception_utils.pyrception_gcs.glob_list)
  * [load\_json](#pyrception_utils.pyrception_gcs.load_json)
  * [PyrceptionGCSDataset](#pyrception_utils.pyrception_gcs.PyrceptionGCSDataset)
    * [\_\_init\_\_](#pyrception_utils.pyrception_gcs.PyrceptionGCSDataset.__init__)
    * [\_\_getitem\_\_](#pyrception_utils.pyrception_gcs.PyrceptionGCSDataset.__getitem__)
    * [\_\_len\_\_](#pyrception_utils.pyrception_gcs.PyrceptionGCSDataset.__len__)

<a name="pyrception_utils"></a>
# pyrception\_utils

<a name="pyrception_utils.cli"></a>
# pyrception\_utils.cli

<a name="pyrception_utils.cli.preview"></a>
#### preview

```python
@subcommand(
    [argument("--data", type=str, help="The path to the main perception data folder.")]
)
preview(args)
```

Previews the dataset in a streamlit app.

<a name="pyrception_utils.preview"></a>
# pyrception\_utils.preview

<a name="pyrception_utils.preview.list_datasets"></a>
#### list\_datasets

```python
list_datasets(path) -> List
```

Lists the datasets in a diretory.

**Arguments**:

- `path`: path to a directory that contains dataset folders
:type str:

**Returns**:

A list of dataset directories.
:rtype: List

<a name="pyrception_utils.preview.frame_selector_ui"></a>
#### frame\_selector\_ui

```python
frame_selector_ui(dataset: PyrceptionDataset) -> int
```

Frame selector streamlist widget to select which frame in the dataset to display

**Arguments**:

- `dataset`: the PyrceptionDataset
:type PyrceptionDataset:

**Returns**:

The image index
:rtype: int

<a name="pyrception_utils.preview.draw_image_with_boxes"></a>
#### draw\_image\_with\_boxes
<a name="pyrception_utils.preview.draw_image_with_boxes"></a>
#### draw\_image\_with\_boxes
```python
draw_image_with_boxes(image: Image, classes: Dict, labels: List, boxes: List[List], colors: Dict, header: str, description: str)
```

Draws an image in streamlit with labels and bounding boxes.

**Arguments**:

- `image`: the PIL image
:type PIL:
- `classes`: the class dictionary
:type Dict:
- `labels`: list of integer object labels for the frame
:type List:
- `boxes`: List of bounding boxes (as a List of coordinates) for the frame
:type List[List]:
- `colors`: class colors
:type Dict:
- `header`: Image header
:type str:
- `description`: Image description
:type str:

<a name="pyrception_utils.preview.load_perception_dataset"></a>
#### load\_perception\_dataset

```python
@st.cache(show_spinner=True, allow_output_mutation=True)
load_perception_dataset(path: str) -> Tuple
```

Loads the perception dataset in the cache and caches the random bounding box color scheme.

**Arguments**:

- `path`: Dataset path
:type str:

**Returns**:

A tuple with the colors and PyrceptionDataset object as (colors, dataset)
:rtype: Tuple

<a name="pyrception_utils.preview.preview_dataset"></a>
#### preview\_dataset

```python
preview_dataset(base_dataset_dir: str)
```

Adds streamlit components to the app to construct the dataset preview.

**Arguments**:

- `base_dataset_dir`: The directory that contains the perceptions datasets.
:type str:

<a name="pyrception_utils.preview.preview_app"></a>
#### preview\_app

```python
preview_app(args)
```

Starts the dataset preview app.

**Arguments**:

- `args`: Arguments for the app, such as dataset
:type args: Namespace

<a name="pyrception_utils.pyrception"></a>
# pyrception\_utils.pyrception

<a name="pyrception_utils.pyrception.FileType"></a>
## FileType Objects

```python
class FileType(Enum)
```

Enumerator for file types in the perception dataset. Based on

<a name="pyrception_utils.pyrception.glob"></a>
#### glob

```python
glob(data_root: str, pattern: str) -> Iterator[str]
```

Find all files in a directory, data_dir, that match the pattern.

**Arguments**:

- `data_root`: The path to the directory that contains the dataset.
:type str:
- `pattern`: The file pattern to match.
:type str:

**Returns**:

Returns an string iterator containing the paths to the matching files.
:rtype: Iterator[str]

<a name="pyrception_utils.pyrception.file_number"></a>
#### file\_number

```python
file_number(filename)
```

Key function to sort glob list.

**Arguments**:

- `filename`: POSIX path
:type filename:

**Returns**:


:rtype:

<a name="pyrception_utils.pyrception.glob_list"></a>
#### glob\_list

```python
glob_list(data_root: str, pattern: str) -> List
```

Find all files in a directory, data_dir, that match the pattern.

**Arguments**:

- `data_root`: The path to the directory that contains the dataset.
:type str:
- `pattern`: The file pattern to match.
:type str:

**Returns**:

Returns an string iterator containing the paths to the matching files.
:rtype: Iterator[str]

<a name="pyrception_utils.pyrception.load_json"></a>
#### load\_json

```python
load_json(file: str, key: Union[str, List]) -> Dict
```

Loads top level records from json file given key or list of keys.

**Arguments**:

- `file`: The json filename.
:type str:
- `key`: The top-level key or list of keys to load.
:type Union[str, List]:

**Returns**:

Returns a dictionary representing the json record
:rtype: Dict

<a name="pyrception_utils.pyrception.PyrceptionDatasetMetadata"></a>
## PyrceptionDatasetMetadata Objects

```python
class PyrceptionDatasetMetadata()
```

<a name="pyrception_utils.pyrception.PyrceptionDatasetMetadata.__init__"></a>
#### \_\_init\_\_

```python
 | __init__(data_dir: str = None)
```

Creates a PyrceptionDataset object that can be used to iterate through the perception
dataset.

**Arguments**:

- `data_dir`: The path to the perception dataset.
:type str:

<a name="pyrception_utils.pyrception.PyrceptionDataset"></a>
## PyrceptionDataset Objects

```python
class PyrceptionDataset()
```

Pyrception class for reading and visualizing annotations generated by the perception SDK.

<a name="pyrception_utils.pyrception.PyrceptionDataset.__init__"></a>
#### \_\_init\_\_

```python
 | __init__(metadata: PyrceptionDatasetMetadata = None, data_dir: str = None)
```

Creates a PyrceptionDataset object that can be used to iterate through the perception
dataset.

**Arguments**:

- `data_dir`: The path to the perception dataset.
:type str:

<a name="pyrception_utils.pyrception.PyrceptionDataset.__getitem__"></a>
#### \_\_getitem\_\_

```python
 | __getitem__(index: int) -> Tuple
```

Iterator to get one frame at a time based on index.

**Arguments**:

- `index`: the index of the frame to retrieve
:type int:

**Returns**:

Returns a tuple containing the image and target metadata as (image, target)
:rtype: Tuple

<a name="pyrception_utils.pyrception.PyrceptionDataset.__len__"></a>
#### \_\_len\_\_

```python
 | __len__() -> int
```

Returns the length of the perception dataset.

**Returns**:

Length of the dataset.
:rtype: int

<a name="pyrception_utils.pyrception_gcs"></a>
# pyrception\_utils.pyrception\_gcs

<a name="pyrception_utils.pyrception_gcs.FileType"></a>
## FileType Objects

```python
class FileType(Enum)
```

Enumerator for file types in the perception dataset. Based on

<a name="pyrception_utils.pyrception_gcs.glob"></a>
#### glob

```python
glob(data_root: str, pattern: str) -> Iterator[str]
```

Find all files in a directory, data_dir, that match the pattern.

**Arguments**:

- `data_root`: The path to the directory that contains the dataset.
:type str:
- `pattern`: The file pattern to match.
:type str:

**Returns**:

Returns an string iterator containing the paths to the matching files.
:rtype: Iterator[str]

<a name="pyrception_utils.pyrception_gcs.glob_list"></a>
#### glob\_list

```python
glob_list(fs: GCSFileSystem, data_root: str, pattern: str) -> List
```

Find all files in a directory, data_dir, that match the pattern.

**Arguments**:

- `fs`: the GCSFileSystem object
:type GCSFileSystem
- `data_root`: The path to the directory that contains the dataset.
:type str:
- `pattern`: The file pattern to match.
:type str:

**Returns**:

Returns an string iterator containing the paths to the matching files.
:rtype: Iterator[str]

<a name="pyrception_utils.pyrception_gcs.load_json"></a>
#### load\_json

```python
load_json(fs: GCSFileSystem, file: str, key: Union[str, List]) -> Dict
```

Loads top level records from json file given key or list of keys.

**Arguments**:

- `fs`: the GCSFileSystem object
:type GCSFileSystem
- `file`: The json filename.
:type str:
- `key`: The top-level key or list of keys to load.
:type Union[str, List]:

**Returns**:

Returns a dictionary representing the json record
:rtype: Dict

<a name="pyrception_utils.pyrception_gcs.PyrceptionGCSDataset"></a>
## PyrceptionGCSDataset Objects

```python
class PyrceptionGCSDataset()
```

Pyrception class for reading and visualizing annotations generated by the perception SDK.

<a name="pyrception_utils.pyrception_gcs.PyrceptionGCSDataset.__init__"></a>
#### \_\_init\_\_

```python
 | __init__(project_id: str = None, dataset_bucket: str = None, dataset_folder: str = None)
```

Creates a PyrceptionDataset object that can be used to iterate through the perception
dataset.

**Arguments**:

- `dataset_bucket`: The path to the perception dataset.
:type str:

<a name="pyrception_utils.pyrception_gcs.PyrceptionGCSDataset.__getitem__"></a>
#### \_\_getitem\_\_

```python
 | __getitem__(index: int) -> Tuple
```

Iterator to get one frame at a time based on index.

**Arguments**:

- `index`: the index of the frame to retrieve
:type int:

**Returns**:

Returns a tuple containing the image and target metadata as (image, target)
:rtype: Tuple

<a name="pyrception_utils.pyrception_gcs.PyrceptionGCSDataset.__len__"></a>
#### \_\_len\_\_

```python
 | __len__() -> int
```

Returns the length of the perception dataset.

**Returns**:

Length of the dataset.
:rtype: int

