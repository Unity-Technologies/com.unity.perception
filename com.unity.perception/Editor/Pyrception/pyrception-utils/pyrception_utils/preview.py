import argparse
import os
from typing import Dict, List, Tuple

import numpy as np
import streamlit as st
from PIL import ImageFont
from PIL.Image import Image
from PIL.ImageDraw import ImageDraw
from pyrception_utils import PyrceptionDataset


def list_datasets(path) -> List:
    """
    Lists the datasets in a diretory.
    :param path: path to a directory that contains dataset folders
    :type str:
    :return: A list of dataset directories.
    :rtype: List
    """
    datasets = []
    for item in os.listdir(path):
        if os.path.isdir(os.path.join(path, item)) and item != "Unity":
            datasets.append(item)

    return datasets


def frame_selector_ui(dataset: PyrceptionDataset) -> int:
    """
    Frame selector streamlist widget to select which frame in the dataset to display
    :param dataset: the PyrceptionDataset
    :type PyrceptionDataset:
    :return: The image index
    :rtype: int
    """
    st.sidebar.markdown("# Image set")
    num_images = len(dataset)
    image_index = st.sidebar.slider("Image number", 0, num_images - 1)
    return image_index


def draw_image_with_boxes(
    image: Image,
    classes: Dict,
    labels: List,
    boxes: List[List],
    colors: Dict,
    header: str,
    description: str,
):
    """
    Draws an image in streamlit with labels and bounding boxes.

    :param image: the PIL image
    :type PIL:
    :param classes: the class dictionary
    :type Dict:
    :param labels: list of integer object labels for the frame
    :type List:
    :param boxes: List of bounding boxes (as a List of coordinates) for the frame
    :type List[List]:
    :param colors: class colors
    :type Dict:
    :param header: Image header
    :type str:
    :param description: Image description
    :type str:
    """
    image_draw = ImageDraw(image)
    # draw bounding boxes
    font = ImageFont.truetype("C:\Windows\Fonts\Arial.ttf", 15)
    for label, box in zip(labels, boxes):
        class_name = classes[label]
        image_draw.rectangle(box, outline=colors[class_name], width=2)
        image_draw.text(
            (box[0], box[1]), class_name, font=font, fill=colors[class_name]
        )
    st.subheader(header)
    st.markdown(description)
    st.image(image, use_column_width=True)


@st.cache(show_spinner=True, allow_output_mutation=True)
def load_perception_dataset(path: str) -> Tuple:
    """
    Loads the perception dataset in the cache and caches the random bounding box color scheme.
    :param path: Dataset path
    :type str:
    :return: A tuple with the colors and PyrceptionDataset object as (colors, dataset)
    :rtype: Tuple
    """
    dataset = PyrceptionDataset(data_dir=path)
    classes = dataset.classes
    colors = {name: tuple(np.random.randint(128, 255, size=3)) for name in classes}
    return colors, dataset


def preview_dataset(base_dataset_dir: str):
    """
    Adds streamlit components to the app to construct the dataset preview.

    :param base_dataset_dir: The directory that contains the perceptions datasets.
    :type str:
    """
    st.markdown("# Synthetic Dataset Preview\n ## Unity Technologies ")
    dataset_name = st.sidebar.selectbox(
        "Please select a dataset...", list_datasets(base_dataset_dir)
    )
    if dataset_name is not None:
        colors, dataset = load_perception_dataset(
            os.path.join(base_dataset_dir, dataset_name)
        )
        classes = dataset.classes
        image_index = frame_selector_ui(dataset)
        image, target = dataset[image_index]
        labels = target["labels"]
        boxes = target["boxes"]
        draw_image_with_boxes(
            image, classes, labels, boxes, colors, "Synthetic Image Preview", ""
        )


def preview_app(args):
    """
    Starts the dataset preview app.

    :param args: Arguments for the app, such as dataset
    :type args: Namespace
    """
    dataset_dir = args.data
    if dataset_dir is not None:
        st.sidebar.title("Pyrception Dataset Preview")
        preview_dataset(dataset_dir)
    else:
        raise ValueError("Please specify the path to the main dataset directory!")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("data", type=str)
    args = parser.parse_args()
    preview_app(args)
