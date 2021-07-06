import argparse
import json
import os
import sys
import time
import subprocess
from pathlib import Path
from typing import List, Tuple

import numpy as np
import streamlit as st
import SessionState
import visualization.visualizers as v


# import datasetinsights
from pyrception_utils import PyrceptionDataset

st.set_page_config(layout="wide") #This needs to be the first streamlit command
import helpers.custom_components_setup as cc


def list_datasets(path) -> List:
    """
    Lists the datasets in a diretory.
    :param path: path to a directory that contains dataset folders
    :type str:
    :return: list of dataset directories
    :rtype: List
    """
    datasets = []
    for item in os.listdir(path):
        if os.path.isdir(os.path.join(path, item)) and item != "Unity":
            date = os.path.getctime(os.path.join(path, item))
            datasets.append((date, item))
    datasets.sort(reverse=True)
    for idx, (date, item) in enumerate(datasets):
        datasets[idx] = (time.ctime(date)[4:], item)
    return datasets


@st.cache(show_spinner=True, allow_output_mutation=True)
def load_perception_dataset(path: str) -> Tuple:
    """
    Loads the perception dataset in the cache and caches the random bounding box color scheme.
    :param path: Dataset path
    :type str:
    :return: A tuple with the colors and PyrceptionDataset object as (colors, dataset)
    :rtype: Tuple
    """
    # --------------------------------CHANGE TO DATASETINSIGHTS LOADING---------------------------------------------

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

    session_state = SessionState.get(image='-1', start_at='0', num_cols='3', current_page='main', curr_dir=base_dataset_dir)
    base_dataset_dir = session_state.curr_dir

    st.sidebar.markdown("# Select Project")
    if st.sidebar.button("Change dataset folder"):
        folder_select(session_state)

    if session_state.current_page == 'main':
        st.sidebar.markdown("# Dataset Selection")
        datasets = list_datasets(base_dataset_dir)
        datasets_names = [ctime + " " + item for ctime, item in datasets]

        dataset_name = st.sidebar.selectbox(
            "Please select a dataset...", datasets_names
        )
        for ctime, item in datasets:
            if dataset_name.startswith(ctime):
                dataset_name = item
                break

        if dataset_name is not None:
            colors, dataset = load_perception_dataset(
                os.path.join(base_dataset_dir, dataset_name)
            )

            st.sidebar.markdown("# Labeler Visualization")

            # change this to load from dataset insights the names of the available labelers
            available_labelers = [a["name"] for a in dataset.metadata.annotations]

            labelers = {}
            if 'bounding box' in available_labelers:
                labelers['bounding box'] = st.sidebar.checkbox("Bounding Boxes 2D", key="bb2d")
            if 'bounding box 3D' in available_labelers:
                labelers['bounding box 3D'] = st.sidebar.checkbox("Bounding Boxes 3D", key="bb2d")
            if 'keypoints' in available_labelers:
                labelers['keypoints'] = st.sidebar.checkbox("Key Points", key="kp")
            if 'instance segmentation' in available_labelers and 'semantic segmentation' in available_labelers:
                if st.sidebar.checkbox('Segmentation'):
                    selected_segmentation = st.sidebar.radio("Select the segmentation type:", ['Semantic Segmentation', 'Instance Segmentation'], index=0)
                    if selected_segmentation == 'Semantic Segmentation':
                        labelers['semantic segmentation'] = True
                    elif selected_segmentation == 'Instance Segmentation':
                        labelers['instance segmentation'] = True
            elif 'semantic segmentation' in available_labelers:
                labelers['semantic segmentation'] = st.sidebar.checkbox("Semantic Segmentation", key="ss")
            elif 'instance segmentation' in available_labelers:
                labelers['instance segmentation'] = st.sidebar.checkbox("Instance Segmentation", key="is")

            st.sidebar.markdown("# Filter")
            st.sidebar.write("Coming soon")

            st.sidebar.markdown("# Highlight")
            st.sidebar.write("Coming soon")

            index = int(session_state.image)
            if index >= 0:
                dataset_path = os.path.join(base_dataset_dir, dataset_name)
                zoom(index, colors, dataset, session_state, labelers, dataset_path)
            else:
                num_rows = 5
                grid_view(num_rows, colors, dataset, session_state, labelers)


def get_image_with_labelers(image_and_labelers, dataset, colors, labelers_to_use):
    image = image_and_labelers['image']
    if 'semantic segmentation' in labelers_to_use and labelers_to_use['semantic segmentation']:
        semantic = image_and_labelers["semantic segmentation"]
        image = v.draw_image_with_segmentation(
            image, semantic
        )

    if 'instance segmentation' in labelers_to_use and labelers_to_use['instance segmentation']:
        instance = image_and_labelers['instance segmentation']
        image = v.draw_image_with_segmentation(
            image, instance
        )

    if 'bounding box' in labelers_to_use and labelers_to_use['bounding box']:
        target = image_and_labelers["bounding box"]
        labels = target["labels"]
        boxes = target["boxes"]
        classes = dataset.classes
        image = v.draw_image_with_boxes(
            image, classes, labels, boxes, colors
        )

    if 'keypoints' in labelers_to_use and labelers_to_use['keypoints']:
        keypoints = image_and_labelers["keypoints"]
        image = v.draw_image_with_keypoints(
            image, keypoints, dataset
        )

    if 'bounding box 3D' in labelers_to_use and labelers_to_use['bounding box 3D']:
        sensor, values = image_and_labelers['bounding box 3D']
        image = v.draw_image_with_box_3d(image, sensor, values, colors)

    return image


def folder_select(session_state):

    '''if st.sidebar.button("< Back to main page"):
        session_state.current_page = 'main'
        st.experimental_rerun()
    else:
        curr_dir = session_state.curr_dir
        print(curr_dir)
        curr_dir = str(Path(curr_dir).absolute()).replace("\\", "/") + "/"
        placeholder = st.empty()
        prev = curr_dir
        curr_dir = placeholder.text_input("Path to data", curr_dir)
        if curr_dir != prev:
            curr_dir = str(Path(curr_dir).absolute()).replace("\\", "/") + "/"
            session_state.curr_dir = curr_dir
            st.experimental_rerun()

        if st.button("< parent folder"):
            curr_dir = str(Path(curr_dir).parent.absolute()).replace("\\", "/") + "/"
            session_state.curr_dir = curr_dir
            st.experimental_rerun()

        st.write("Contents of " + curr_dir)
        folder_cols = st.beta_columns(3)
        folders = [d for d in os.listdir(curr_dir) if os.path.isdir(curr_dir + d)]
        for i, folder in enumerate(folders):
            if folder_cols[i % 3].button(folder):
                session_state.curr_dir = str(Path(curr_dir + folder).absolute()).replace("\\", "/") + "/"
                st.experimental_rerun()
    '''


        # session_state.curr_dir = str(Path(fileName).absolute()).replace("\\", "/") + "/"

    # fileName = dialog.getExistingDirectory(
            #     self,
            #     "Select Directory",
            #     "",
            #     QFileDialog.ShowDirsOnly | QFileDialog.DontResolveSymlinks
            # )

    #app.quit()
    #del app
    output = subprocess.run([sys.executable, os.path.join(os.path.dirname(os.path.realpath(__file__)), "helpers/folder_explorer.py")], stdin=subprocess.PIPE, stdout=subprocess.PIPE,  stderr=subprocess.STDOUT)

    session_state.curr_dir = str(os.path.abspath(str(output.stdout).split("\'")[1])[:-4]).replace("\\", "/") + "/"
    st.experimental_rerun()


def grid_view(num_rows, colors, dataset, session_state, labelers):
    header = st.beta_columns([2 / 3, 1 / 3])

    num_cols = header[1].slider(label="Image per row: ", min_value=1, max_value=5, step=1,
                                value=int(session_state.num_cols))
    if not num_cols == session_state.num_cols:
        session_state.num_cols = num_cols
        st.experimental_rerun()

    with header[0]:
        start_at = cc.item_selector(int(session_state.start_at), num_cols * num_rows, len(dataset))
        session_state.start_at = start_at

    cols = st.beta_columns(num_cols)

    for i in range(start_at, min(start_at + (num_cols * num_rows), len(dataset))):
        image = get_image_with_labelers(dataset[i], dataset, colors, labelers)

        container = cols[(i - (start_at % num_cols)) % num_cols].beta_container()
        container.write("Capture #" + str(i))
        expand_image = container.button(label="Expand image", key="exp" + str(i))
        container.image(image, caption=str(i), use_column_width=True)
        if expand_image:
            session_state.image = i
            st.experimental_rerun()


def zoom(index, colors, dataset, session_state, labelers, dataset_path):
    header = st.beta_columns([0.2, 0.6, 0.2])

    if header[0].button('< Back to Grid view'):
        session_state.image = -1
        st.experimental_rerun()

    with header[1]:
        new_index = cc.item_selector_zoom(index, len(dataset))
        if not new_index == index:
            session_state.image = new_index
            st.experimental_rerun()

    image = get_image_with_labelers(dataset[index], dataset, colors, labelers)

    layout = st.beta_columns([0.7, 0.3])
    layout[0].image(image, use_column_width=True)
    layout[1].title("JSON metadata")

    captures_dir = None
    for directory in os.walk(dataset_path):
        name = str(directory[0]).replace('\\', '/').split('/')[-1]
        if name.startswith("Dataset") and "." not in name[1:]:
            captures_dir = directory[0]
            break

    file_num = index // 150
    postfix = ('000' + str(file_num))
    postfix = postfix[len(postfix) - 3:]
    path_to_captures = os.path.join(os.path.abspath(captures_dir), "captures_" + postfix + ".json")
    with layout[1]:
        json_file = json.load(open(path_to_captures, "r"))
        cc.json_viewer(json.dumps(json_file["captures"][index]))


def preview_app(args):
    """
    Starts the dataset preview app.

    :param args: Arguments for the app, such as dataset
    :type args: Namespace
    """
    if args.data is not None:
        preview_dataset(args.data)
    else:
        ValueError("Please use a valid path")


if __name__ == "__main__":

    parser = argparse.ArgumentParser()
    parser.add_argument("data", type=str)
    args = parser.parse_args()

    st.write(os.getcwd())

    # remove the default zoom in button for images
    st.markdown('<style>button.css-9eqr5v{display: none}</style>', unsafe_allow_html=True)
    preview_app(args)

