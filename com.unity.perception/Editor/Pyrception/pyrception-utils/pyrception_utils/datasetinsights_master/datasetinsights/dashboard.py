import argparse
import json
import os

import dash_core_components as dcc
import dash_html_components as html
from dash.dependencies import Input, Output

import datasetinsights.stats.visualization.overview as overview
from datasetinsights.stats.visualization.app import get_app
from datasetinsights.stats.visualization.object_detection import (
    render_object_detection_layout,
)

app = get_app()


def main_layout():
    """ Method for generating main app layout.

    Returns:
        html layout: main layout design with tabs for overview statistics
            and object detection.
    """
    app_layout = html.Div(
        [
            html.H1(
                children="Dataset Insights",
                style={
                    "textAlign": "center",
                    "padding": 20,
                    "background": "lightgrey",
                },
            ),
            html.Div(
                [
                    dcc.Tabs(
                        id="page_tabs",
                        value="dataset_overview",
                        children=[
                            dcc.Tab(
                                label="Overview", value="dataset_overview",
                            ),
                            dcc.Tab(
                                label="Object Detection",
                                value="object_detection",
                            ),
                        ],
                    ),
                    html.Div(id="main_page_tabs"),
                ]
            ),
            # Sharing data between callbacks using hidden division.
            # These hidden dcc and html components are for storing data-root
            # into the division. This is further used in callbacks made in the
            # object_detection module. This is a temporary hack and can be found
            # in example 1 of sharing data between callback dash tutorial.
            # ref: https://dash.plotly.com/sharing-data-between-callbacks
            # TODO: Fix this using a better solution to share data.
            dcc.Dropdown(id="dropdown", style={"display": "none"}),
            html.Div(id="data_root_value", style={"display": "none"}),
        ]
    )
    return app_layout


@app.callback(
    Output("data_root_value", "children"), [Input("dropdown", "value")]
)
def store_data_root(value):
    """ Method for storing data-root value in a hidden division.

    Returns:
        json : data-root encoded in json to be stored in data_root_value div.
    """
    json_data_root = json.dumps(data_root)

    return json_data_root


@app.callback(
    Output("main_page_tabs", "children"),
    [Input("page_tabs", "value"), Input("data_root_value", "children")],
)
def render_content(value, json_data_root):
    """ Method for rendering dashboard layout based
        on the selected tab value.

    Args:
        value(str): selected tab value
        json_data_root: data root stored in hidden div in json format.

    Returns:
        html layout: layout for the selected tab.
    """
    # read data root value from the data_root_value division
    data_root = json.loads(json_data_root)
    if value == "dataset_overview":
        return overview.html_overview(data_root)
    elif value == "object_detection":
        return render_object_detection_layout(data_root)


def check_path(path):
    """ Method for checking if the given data-root path is valid or not."""
    if os.path.isdir(path):
        return path
    else:
        raise ValueError(f"Path {path} not found")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--data-root", help="Path to the data root")
    args = parser.parse_args()
    data_root = check_path(args.data_root)
    app.layout = main_layout()
    app.run_server(debug=True)
