import json
import os
import pathlib
import re
from collections import namedtuple
from enum import Enum
from typing import Dict, Iterator, List, Tuple, Union

from PIL import Image


class FileType(Enum):
    """
    Enumerator for file types in the perception dataset. Based on
    """

    BINARY = "binary"
    REFERENCE = "reference"
    METRIC = "metric"
    CAPTURE = "capture"


MetadataFile = namedtuple("File", "file pattern filetype")
DATASET_METADATA = {
    "annotation_definitions": MetadataFile(
        "**/annotation_definitions.json",
        r"(?:\w|-|/)*annotation_definitions.json",
        FileType.REFERENCE,
    ),
    "captures": MetadataFile(
        "**/captures_*.json",
        r"(?:\w|-|/)*captures_[0-9]+.json",
        FileType.CAPTURE,
    ),
    "egos": MetadataFile("**/egos.json", r"(?:\w|-|/)*egos.json", FileType.REFERENCE),
    "metric_definitions": MetadataFile(
        "**/metric_definitions.json",
        r"(?:\w|-|/)*metric_definitions.json",
        FileType.REFERENCE,
    ),
    "metrics": MetadataFile(
        "**/metrics_*.json", r"(?:\w|-|/)*metrics_[0-9]+.json", FileType.METRIC
    ),
    "sensors": MetadataFile(
        "**/sensors.json", r"(?:\w|-|/)*sensors.json", FileType.REFERENCE
    ),
    "metadata": MetadataFile(
        "**/metadata.json", r"(?:\w|-|/)*metadata.json", FileType.REFERENCE
    ),
}


# File globbing based on https://github.com/Unity-Technologies/datasetinsights/blob/master
# /datasetinsights/datasets/unity_perception/tables.py
def glob(data_root: str, pattern: str) -> Iterator[str]:
    """
    Find all files in a directory, data_dir, that match the pattern.

    :param data_root: The path to the directory that contains the dataset.
    :type str:
    :param pattern: The file pattern to match.
    :type str:
    :return: Returns an string iterator containing the paths to the matching files.
    :rtype: Iterator[str]
    """
    path = pathlib.Path(data_root)
    for file_path in path.glob(pattern):
        yield file_path


def file_number(filename):
    """
    Key function to sort glob list.

    :param filename: POSIX path
    :type filename:
    :return:
    :rtype:
    """
    result = re.split("_|\.", str(filename))[-2]
    return int(result)


def glob_list(data_root: str, pattern: str) -> List:
    """
    Find all files in a directory, data_dir, that match the pattern.

    :param data_root: The path to the directory that contains the dataset.
    :type str:
    :param pattern: The file pattern to match.
    :type str:
    :return: Returns an string iterator containing the paths to the matching files.
    :rtype: Iterator[str]
    """

    path = pathlib.Path(data_root)
    file_list = []
    for file_path in sorted(path.glob(pattern), key=file_number):
        file_list.append(file_path)

    return file_list


# TODO add version checking
def load_json(file: str, key: Union[str, List]) -> Dict:
    """
     Loads top level records from json file given key or list of keys.

    :param file: The json filename.
    :type str:
    :param key: The top-level key or list of keys to load.
    :type Union[str, List]:
    :return: Returns a dictionary representing the json record
    :rtype: Dict
    """
    data = json.load(open(file, "r"))
    if isinstance(key, str):
        return data[key]
    elif isinstance(key, List):
        return {k: data[k] for k in key}


class PyrceptionDatasetMetadata:
    DATA = "captures"
    META = "metadata"
    ANNOTATION_META = "annotation_definitions"
    DATASET_INFO = ["dataset_size", "per_file_size", "image_width", "image_height"]
    DATA_PATTERN = DATASET_METADATA[DATA].file
    META_PATTERN = DATASET_METADATA[META].file
    ANN_PATTERN = DATASET_METADATA[ANNOTATION_META].file

    def __init__(self, data_dir: str = None):
        """

        Creates a PyrceptionDataset object that can be used to iterate through the perception
        dataset.

        :param data_dir: The path to the perception dataset.
        :type str:
        """
        self.image_size = None
        self.data_dir = data_dir
        self.data = []
        self.annotations = []
        if self.data_dir is None:
            raise ValueError("You must specify the path to a perception sdk dataset.")

        # Load annotation metadata file set
        self.data_files = glob_list(self.data_dir, self.DATA_PATTERN)
        self.data = None

        # Load metadata info from metadata file
        for metadata_file in glob(self.data_dir, self.META_PATTERN):
            self.dataset_info = load_json(metadata_file, self.DATASET_INFO)

        # Load dataset info and annotation definitions file
        for annotation_file in glob(self.data_dir, self.ANN_PATTERN):
            self.annotations.extend(load_json(annotation_file, self.ANNOTATION_META))

        # Set dataset info
        self.length = self.dataset_info["dataset_size"]
        self.file_mod_factor = self.dataset_info["per_file_size"]

        # Extract the class labels
        self.classes = []
        for label in self.annotations[0]["spec"]:
            if "label_name" in label:
                self.classes.append(label["label_name"])

        # Set the number of classes
        self.num_classes = len(self.classes)

        # Set the image size
        self.image_size = (
            self.dataset_info["image_height"],
            self.dataset_info["image_width"],
            3,
        )


class PyrceptionDataset:
    """
    Pyrception class for reading and visualizing annotations generated by the perception SDK.
    """

    def __init__(
        self, metadata: PyrceptionDatasetMetadata = None, data_dir: str = None
    ):
        """

        Creates a PyrceptionDataset object that can be used to iterate through the perception
        dataset.

        :param data_dir: The path to the perception dataset.
        :type str:
        """
        if metadata:
            self.metadata = metadata
        elif not metadata and data_dir:
            self.metadata = PyrceptionDatasetMetadata(data_dir)
        else:
            raise ValueError(
                "You must specify either PyrceptionDatasetMetadata or a data directory"
            )
        self.last_file_index = None
        self.ann_to_index = None

    def __getitem__(self, index: int) -> dict:
        """
        Iterator to get one frame at a time based on index.

        :param index: the index of the frame to retrieve
        :type int:
        """

        if index > self.metadata.length - 1:
            raise IndexError("Index of out bounds.")

        sub_index = self.__load_subset(index)

        image_and_labelers = {}

        try:
            # Image
            image = Image.open(
                os.path.join(self.metadata.data_dir, self.data[sub_index]["filename"])
            ).convert("RGB")
            image_and_labelers["image"] = image

            if self.ann_to_index is None:
                # Assumes that the order is the same for the annotations in metadata as in the captures_***.json file
                self.ann_to_index = {}
                for i in range(len(self.metadata.annotations)):
                    a = self.metadata.annotations[i]
                    for j in range(len(self.data[sub_index]["annotations"])):
                        if self.data[sub_index]["annotations"][j]["annotation_definition"] == a["id"]:
                            self.ann_to_index[a["name"]] = j
                            break
                self.ann_to_index = self.ann_to_index

            # Bounding Boxes
            if "bounding box" in self.ann_to_index:
                image_and_labelers["bounding box"] = self.get_bounding_boxes(sub_index, self.ann_to_index["bounding box"])

            # Bounding Boxes 3d
            if "bounding box 3D" in self.ann_to_index:
                image_and_labelers["bounding box 3D"] = self.get_bounding_box_3d(sub_index, self.ann_to_index["bounding box 3D"])

            # Semantic Segmentation
            if "semantic segmentation" in self.ann_to_index:
                image_and_labelers["semantic segmentation"] = self.get_segmentation(sub_index, self.ann_to_index[
                    "semantic segmentation"])

            # Instance Segmentation
            if "instance segmentation" in self.ann_to_index:
                image_and_labelers["instance segmentation"] = self.get_segmentation(sub_index, self.ann_to_index[
                    "instance segmentation"])

            # Keypoints
            if "keypoints" in self.ann_to_index:
                image_and_labelers["keypoints"] = self.get_keypoints(sub_index, self.ann_to_index["keypoints"])

        except IndexError:
            print(self.data)
            raise IndexError(f"Index is :{index} Subindex is:{sub_index}")

        return image_and_labelers

    def get_keypoints(self, sub_index, ann_index):
        image_ann = self.data[sub_index]
        keypoints = image_ann["annotations"][ann_index]["values"][0]["keypoints"]
        return keypoints

    def get_segmentation(self, sub_index, ann_index):
        return Image.open(
            os.path.join(self.metadata.data_dir, self.data[sub_index]["annotations"][ann_index]["filename"])
        ).convert("RGB")

    def get_bounding_box_3d(self, sub_index, ann_index):
        sensor = self.data[sub_index]["sensor"]
        values = self.data[sub_index]["annotations"][ann_index]["values"]
        return sensor, values

    def get_bounding_boxes(self, sub_index, ann_index):
        image_ann = self.data[sub_index]
        boxes = []
        labels = []
        for value in image_ann["annotations"][ann_index]["values"]:
            box = [
                value["x"],
                value["y"],
                value["x"] + value["width"],
                value["y"] + value["height"],
            ]
            label = value["label_id"]
            boxes.append(box)
            labels.append(label)

        # assumes that the image id naming convention is
        # RGB<uuid>/rgb_<image_id>.png
        image_id = self.data[sub_index]["filename"][44:-4]
        return {"image_id": image_id, "labels": labels, "boxes": boxes}

    def __len__(self) -> int:
        """
        Returns the length of the perception dataset.

        :return: Length of the dataset.
        :rtype: int
        """
        return self.metadata.length

    def __load_subset(self, index):
        file_index = index // self.metadata.file_mod_factor
        sub_index = index % self.metadata.file_mod_factor
        if self.last_file_index != file_index:
            self.data = load_json(
                self.metadata.data_files[file_index], self.metadata.DATA
            )
            self.last_file_index = file_index
        return sub_index

    @property
    def num_classes(self):
        return self.metadata.num_classes

    @property
    def classes(self):
        return self.metadata.classes
