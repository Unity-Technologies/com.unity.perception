import argparse
import os
import pathlib
from typing import Dict, List, Tuple

import numpy as np
import streamlit as st
import PIL
from PIL import ImageFont
from PIL.Image import Image
from PIL.ImageDraw import ImageDraw
from pyrception_utils import PyrceptionDataset

st.set_page_config(layout="wide")

#--------------------------------Custom component-----------------------------------------------------------------------

import streamlit.components.v1 as components

root_dir = os.path.dirname(os.path.abspath(__file__))
build_dir = os.path.join(root_dir, "slider/build")

_discrete_slider = components.declare_component(
    "discrete_slider",
    path=build_dir
)


def discrete_slider(greeting, name, key,default=0):
    return _discrete_slider(greeting=greeting, name=name, default=default, key=key)

#-------------------------------------END-------------------------------------------------------------------------------

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
    image = image.copy()
    image_draw = ImageDraw(image)
    # draw bounding boxes
    path_to_font = pathlib.Path(__file__).parent.absolute()
    font = ImageFont.truetype(f"{path_to_font}/NairiNormal-m509.ttf", 15)

    for label, box in zip(labels, boxes):
        label = label - 1
        class_name = classes[label]
        image_draw.rectangle(box, outline=colors[class_name], width=2)
        image_draw.text(
            (box[0], box[1]), class_name, font=font, fill=colors[class_name]
        )
    #st.subheader(header)
    #st.markdown(description)
    #st.image(image, use_column_width=True)
    return image

def draw_image_with_semantic_segmentation(
    image: Image,
    height: int,
    width: int,
    segmentation: Image,
    header: str,
    description: str,
):
    """
    Draws an image in streamlit with labels and bounding boxes.

    :param image: the PIL image
    :type PIL:
    :param height: height of the image
    :type int:
    :param width: width of the image
    :type int:
    :param segmentation: Segmentation Image
    :type PIL:
    :param header: Image header
    :type str:
    :param description: Image description
    :type str:
    """
    # image_draw = ImageDraw(segmentation)
    rgba = np.array(segmentation.copy().convert("RGBA"))
    r,g,b,a = rgba.T
    black_areas = (r == 0) & (b == 0) & (g == 0) & (a == 255)
    other_areas = (r != 0) | (b != 0) | (g != 0)
    rgba[...,0:4][black_areas.T] = (0,0,0,0)
    rgba[...,-1][other_areas.T] = int(0.6 * 255)

    foreground = PIL.Image.fromarray(rgba)
    image = image.copy()
    image.paste(foreground,(0,0),foreground)
    return image

def draw_image_stacked(
    image: Image,
    classes: Dict,
    labels: List,
    boxes: List[List],
    colors: Dict,
    header: str,
    description: str,
    height: int,
    width: int,
    segmentation: Image,

):
    image = image.copy()
    color_intensity = st.sidebar.slider('color intensity 2 (%)', 0, 100, 65);
    alpha = color_intensity / 100;

    for x in range(0, width - 1):
        for y in range(0, height - 1):
            (seg_r, seg_g, seg_b) = segmentation.getpixel((x, y))
            (r, g, b) = image.getpixel((x, y))
            # if it isn't a black pixel in the segmentation image then highlight it with the segmentation color
            if seg_r != 0 or seg_g != 0 or seg_b != 0:
                image.putpixel((x, y),
                               (int((1 - alpha) * r + alpha * seg_r),
                                int((1 - alpha) * g + alpha * seg_g),
                                int((1 - alpha) * b + alpha * seg_b)))

    image_draw = ImageDraw(image)
    # draw bounding boxes
    path_to_font = pathlib.Path(__file__).parent.absolute()
    font = ImageFont.truetype(f"{path_to_font}/NairiNormal-m509.ttf", 15)

    for label, box in zip(labels, boxes):
        label = label - 1
        class_name = classes[label]
        image_draw.rectangle(box, outline=colors[class_name], width=2)
        image_draw.text(
            (box[0], box[1]), class_name, font=font, fill=colors[class_name]
        )

    st.subheader(header)
    st.markdown(description)
    st.image(image, use_column_width=True)


def display_count(
    header: str,
    description: str,
):
    """
    :param header: Image header
    :type str:
    :param description: Image description
    :type str:
    """
    return
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
    #st.markdown("# Synthetic Dataset Preview\n ## Unity Technologies ")
    dataset_name = st.sidebar.selectbox(
        "Please select a dataset...", list_datasets(base_dataset_dir)
    )

    num_rows = 5
    num_cols = 3
    if dataset_name is not None:
        colors, dataset = load_perception_dataset(
            os.path.join(base_dataset_dir, dataset_name)
        )
        #classes = dataset.classes
        #st.sidebar.selectbox(
        #    "hello", classes
        #)
        #image_index = frame_selector_ui(dataset)
        #image, segmentation, target = dataset[image_index]
        #labels = target["labels"]
        #boxes = target["boxes"]

        #st.image(image, use_column_width=True)

        #draw_image_stacked(
        #    image, classes, labels, boxes, colors, "Bounding Boxes Preview", "", dataset.metadata.image_size[0], dataset.metadata.image_size[1], segmentation
        #)
        #draw_image_with_boxes(
        #    image, classes, labels, boxes, colors, "Bounding Boxes Preview", ""
        #)
        #image = draw_image_with_semantic_segmentation(
        #    image, dataset.metadata.image_size[0], dataset.metadata.image_size[1], segmentation, "Semantic Segmentation Preview", ""
        #)
        #image = draw_image_with_boxes(
        #    image, classes, labels, boxes, colors, "Bounding Boxes Preview", ""
        #)

        grid_view(num_cols, num_rows, colors, dataset)


def sidebar():
    return None

def navbar():
    return None

def grid_view(num_cols, num_rows, colors, dataset):
    print("Now did I create the slider?")
    count_of_clicks = discrete_slider("Hello", "Leopoldo", "123")
    st.write("Return value: ", count_of_clicks)

    inner_cols = st.beta_columns([0.1, 0.0001])
    cols = st.beta_columns(num_cols)

    semantic_segmentation = st.sidebar.checkbox("Semantic Segmentation", key="ss")
    bounding_boxes_2d = st.sidebar.checkbox("Bounding Boxes", key="bb2d")

    app_state = st.experimental_get_query_params()
    if "start_at" in app_state:
        start_at = int(app_state["start_at"][0])
    else:
        start_at = 0

    if inner_cols[1].button('>'):
        start_at = min(start_at + num_cols * num_rows, len(dataset)-(len(dataset) % (num_cols * num_rows)))
    if inner_cols[0].button('<'):
        start_at = max(0,start_at - num_cols * num_rows)

    st.experimental_set_query_params(start_at=start_at)

    for i in range(start_at, min(start_at + (num_cols * num_rows), len(dataset))):
        classes = dataset.classes
        image, segmentation, target = dataset[i]
        labels = target["labels"]
        boxes = target["boxes"]

        if semantic_segmentation:
            image = draw_image_with_semantic_segmentation(
                image, dataset.metadata.image_size[0], dataset.metadata.image_size[1], segmentation, "Semantic Segmentation Preview", ""
            )
        if bounding_boxes_2d:
            image = draw_image_with_boxes(
                image, classes, labels, boxes, colors, "Bounding Boxes Preview", ""
            )
        cols[i % num_cols].image(image, caption=str(i), use_column_width = True)



def zoom(index):
    return None

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
