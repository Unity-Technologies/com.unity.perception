import os
# --------------------------------Custom component-----------------------------------------------------------------------

import streamlit.components.v1 as components

custom_components_dir = "../custom_components/"
root_dir = os.path.dirname(os.path.abspath(__file__))
build_dir_slider = os.path.join(root_dir, custom_components_dir+"slider/build")
build_dir_page_selector = os.path.join(root_dir, custom_components_dir+"pageselector/build")
build_dir_go_to = os.path.join(root_dir, custom_components_dir+"goto/build")
build_dir_item_selector = os.path.join(root_dir, custom_components_dir+"itemselector/build")
build_dir_image_selector = os.path.join(root_dir, custom_components_dir+"imageselector/build")
build_dir_json_viewer = os.path.join(root_dir, custom_components_dir+"jsonviewer/build")
build_dir_item_selector_zoom = os.path.join(root_dir, custom_components_dir+"itemselectorzoom/build")

_discrete_slider = components.declare_component(
    "discrete_slider",
    path=build_dir_slider
)

_page_selector = components.declare_component(
    "page_selector",
    path=build_dir_page_selector
)

_go_to = components.declare_component(
    "go_to",
    path=build_dir_go_to
)

_item_selector = components.declare_component(
    "item_selector",
    path=build_dir_item_selector
)

_image_selector = components.declare_component(
    "image_selector",
    path=build_dir_image_selector
)

_json_viewer = components.declare_component(
    "json_viewer",
    path=build_dir_json_viewer
)

_item_selector_zoom = components.declare_component(
    "item_selector_zoom",
    path=build_dir_item_selector_zoom
)


def discrete_slider(greeting, name, key, default=0):
    return _discrete_slider(greeting=greeting, name=name, default=default, key=key)


def page_selector(startAt, incrementAmt, key=0):
    return _page_selector(startAt=startAt, incrementAmt=incrementAmt, key=key, default=0)


def go_to(key=0):
    return _go_to(key=key, default=0)


def item_selector(startAt, incrementAmt, datasetSize, key=0):
    return _item_selector(startAt=startAt, incrementAmt=incrementAmt, datasetSize=datasetSize, key=key, default=startAt)


def image_selector(index, key=0):
    return _image_selector(index=index, key=key, default=index)


def json_viewer(metadata, key=0):
    return _json_viewer(jsonMetadata=metadata, key=key, default=0)


def item_selector_zoom(index, datasetSize, key=0):
    return _item_selector_zoom(index=index, datasetSize=datasetSize, key=key, default=index)


# -------------------------------------END-------------------------------------------------------------------------------
