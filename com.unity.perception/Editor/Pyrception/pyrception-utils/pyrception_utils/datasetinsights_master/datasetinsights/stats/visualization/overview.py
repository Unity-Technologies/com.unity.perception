import json

import dash_core_components as dcc
import dash_html_components as html
from dash.dependencies import Input, Output

import datasetinsights.stats.statistics as stat
import datasetinsights.stats.visualization.constants as constants

from .app import get_app
from .plots import bar_plot, histogram_plot

app = get_app()


def generate_total_counts_figure(max_samples, roinfo):
    """ Method for generating total object count bar plot using ploty.

    Args:
        max_samples(int): maximum number of samples that will be included
            in the plot.
        roinfo(datasetinsights.data.datasets.statistics.RenderedObjectInfo):
            Rendered Object Info in Captures.

    Returns:
        plotly.graph_objects.Figure: chart to display total object count
    """

    total_counts_fig = bar_plot(
        roinfo.total_counts(),
        x="label_id",
        y="count",
        x_title="Label Id",
        y_title="Count",
        title="Total Object Count in Dataset",
        hover_name="label_name",
    )
    return total_counts_fig


def generate_per_capture_count_figure(max_samples, roinfo):
    """ Method for generating object count per capture histogram using ploty.

    Args:
        max_samples(int): maximum number of samples that will be included
            in the plot.
        roinfo(datasetinsights.data.datasets.statistics.RenderedObjectInfo):
            Rendered Object Info in Captures.

    Returns:
        plotly.graph_objects.Figure: chart to display object counts per capture
    """

    per_capture_count_fig = histogram_plot(
        roinfo.per_capture_counts(),
        x="count",
        x_title="Object Counts Per Capture",
        y_title="Frequency",
        title="Distribution of Object Counts Per Capture: Overall",
        max_samples=max_samples,
    )
    return per_capture_count_fig


def generate_pixels_visible_per_object_figure(max_samples, roinfo):
    """ Method for generating pixels visible per object histogram using ploty.

    Args:
        max_samples(int): maximum number of samples that will be included
            in the plot.
        roinfo(datasetinsights.data.datasets.statistics.RenderedObjectInfo):
            Rendered Object Info in Captures.

    Returns:
        plotly.graph_objects.Figure: chart to display visible pixels per object
    """

    pixels_visible_per_object_fig = histogram_plot(
        roinfo.raw_table,
        x="visible_pixels",
        x_title="Visible Pixels Per Object",
        y_title="Frequency",
        title="Distribution of Visible Pixels Per Object: Overall",
        max_samples=max_samples,
    )

    return pixels_visible_per_object_fig


def html_overview(data_root):
    """ Method for displaying overview statistics.

    Args:
        data_root(str): path to the dataset.

    Returns:
        html layout: displays graphs for overview statistics.
    """

    roinfo = stat.RenderedObjectInfo(
        data_root=data_root, def_id=constants.RENDERED_OBJECT_INFO_DEFINITION_ID
    )
    label_names = roinfo.total_counts()["label_name"].unique()

    total_counts_fig = generate_total_counts_figure(
        constants.MAX_SAMPLES, roinfo
    )
    per_capture_count_fig = generate_per_capture_count_figure(
        constants.MAX_SAMPLES, roinfo
    )
    pixels_visible_per_object_fig = generate_pixels_visible_per_object_figure(
        constants.MAX_SAMPLES, roinfo
    )

    overview_layout = html.Div(
        [
            html.Div(id="overview"),
            dcc.Markdown(
                """ # Total Object Count  """, style={"text-align": "center"}
            ),
            dcc.Graph(id="total_count", figure=total_counts_fig,),
            html.Div(
                [
                    dcc.Markdown(
                        """ # Object Count Distribution """,
                        style={"text-align": "center"},
                    ),
                    dcc.Dropdown(
                        id="object_count_filter",
                        options=[{"label": i, "value": i} for i in label_names],
                        value=label_names[0],
                    ),
                ],
            ),
            html.Div(
                [
                    dcc.Graph(
                        id="per_object_count", figure=per_capture_count_fig,
                    ),
                    dcc.Graph(id="per_object_count_filter_graph",),
                ],
                style={"columnCount": 2},
            ),
            html.Div(
                [
                    dcc.Markdown(
                        """# Visible Pixels Distribution """,
                        style={"text-align": "center"},
                    ),
                    dcc.Dropdown(
                        id="pixels_visible_filter",
                        options=[{"label": i, "value": i} for i in label_names],
                        value=label_names[0],
                    ),
                ],
            ),
            html.Div(
                [
                    dcc.Graph(
                        id="pixels_visible_per_object",
                        figure=pixels_visible_per_object_fig,
                    ),
                    dcc.Graph(id="pixels_visible_filter_graph",),
                ],
                style={"columnCount": 2},
            ),
        ],
    )
    return overview_layout


@app.callback(
    Output("pixels_visible_filter_graph", "figure"),
    [
        Input("pixels_visible_filter", "value"),
        Input("data_root_value", "children"),
    ],
)
def update_visible_pixels_figure(label_value, json_data_root):
    """ Method for generating pixels visible histogram for selected object.
    Args:
        label_value (str): value selected by user using drop-down
    Returns:
        plotly.graph_objects.Figure: displays visible pixels distribution.
    """
    roinfo = stat.RenderedObjectInfo(
        data_root=json.loads(json_data_root),
        def_id=constants.RENDERED_OBJECT_INFO_DEFINITION_ID,
    )
    filtered_roinfo = roinfo.raw_table[
        roinfo.raw_table["label_name"] == label_value
    ][["visible_pixels"]]
    filtered_figure = histogram_plot(
        filtered_roinfo,
        x="visible_pixels",
        x_title="Visible Pixels For " + str(label_value),
        y_title="Frequency",
        title="Distribution of Visible Pixels For " + str(label_value),
        max_samples=constants.MAX_SAMPLES,
    )
    return filtered_figure


@app.callback(
    Output("per_object_count_filter_graph", "figure"),
    [
        Input("object_count_filter", "value"),
        Input("data_root_value", "children"),
    ],
)
def update_object_counts_capture_figure(label_value, json_data_root):
    """ Method for generating object count per capture histogram for selected
        object.
    Args:
        label_value (str): value selected by user using drop-down
    Returns:
        plotly.graph_objects.Figure: displays object count distribution.
    """
    roinfo = stat.RenderedObjectInfo(
        data_root=json.loads(json_data_root),
        def_id=constants.RENDERED_OBJECT_INFO_DEFINITION_ID,
    )
    filtered_object_count = roinfo.raw_table[
        roinfo.raw_table["label_name"] == label_value
    ]
    filtered_object_count = (
        filtered_object_count.groupby(["capture_id"])
        .size()
        .to_frame(name="count")
        .reset_index()
    )
    filtered_figure = histogram_plot(
        filtered_object_count,
        x="count",
        x_title="Object Counts Per Capture For " + str(label_value),
        y_title="Frequency",
        title="Distribution of Object Counts Per Capture For "
        + str(label_value),
        max_samples=constants.MAX_SAMPLES,
    )
    return filtered_figure
