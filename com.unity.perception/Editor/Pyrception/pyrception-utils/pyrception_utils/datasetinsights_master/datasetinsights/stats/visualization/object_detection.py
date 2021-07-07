import dash_core_components as dcc
import dash_html_components as html
import dash_table
import pandas as pd

import datasetinsights.datasets.unity_perception as sim
import datasetinsights.stats.visualization.constants as constants

from .plots import histogram_plot, rotation_plot


class ScaleFactor:
    """Generate scale factor distribution.

    Scale Factor describes the size of the rendered object in a capture.
    Higher the scale factor, higher would be the visible pixels.

    Atrributes:
        captures(sim.Captures): a collection of capture records.

    """

    def __init__(self, data_root):
        """

        Args:
            data_root(str): path to the dataset.

        """
        capture = sim.Captures(data_root)
        self.captures = capture.filter(
            def_id=constants.BOUNDING_BOX_2D_DEFINITION_ID
        )

    @staticmethod
    def generate_scale_data(captures):
        """ Method to extract scale parameter from sensor data.

        Args:
            captures(sim.Captures): a collection of capture records.

        Returns:
            pandas.DataFrame: contains 'scale' parameter from the sensor data.

        """
        df_scale_factor = pd.DataFrame(
            [sensor_data["scale"] for sensor_data in captures["sensor"]],
            columns=["scale"],
        )
        return df_scale_factor

    def _generate_scale_factor_figures(self):
        """ Method for generating plots for scale factor distribution.

        Returns:
            plotly.graph_objects.Figure: scale factor distribution.

        """
        df_scale_factor = self.generate_scale_data(self.captures)
        scale_factor_distribution_figure = histogram_plot(
            df_scale_factor,
            x="scale",
            x_title="Scale",
            y_title="Capture count",
            title="Distribution of Scale Factor",
            range_x=[
                min(df_scale_factor["scale"]),
                max(df_scale_factor["scale"]),
            ],
            max_samples=constants.MAX_SAMPLES,
        )
        return scale_factor_distribution_figure

    def html(self):
        """ Method for generating plots for scale factor distribution.

        Returns:
            html layout: displays scale factor distribution.

        """
        scale_factor_distribution_figure = self._generate_scale_factor_figures()
        html_layout = html.Div(
            [
                dcc.Markdown(
                    """# Scale Factor """, style={"text-align": "center"}
                ),
                dcc.Graph(
                    id="scale_factor_distribution_figure",
                    figure=scale_factor_distribution_figure,
                ),
            ]
        )
        return html_layout


class UserParameter:
    """Generate User Parameter

    Generate User Parameter table to be displayed on the Dashboard.
    Users parameters, such as ScaleFactors, MaxFrames,
    MaxForegroundObjectsPerFrame are used to control the domain
    randomization parameter used in the simulation.

    Atrributes:
        metrics(sim.Metrics): a collection of metrics records
        user_parameter_table (pandas.DataFrame): dataframe containing user
            parameters.

    Args:
        data_root(str): path to the dataset.
    """

    def __init__(self, data_root):
        self.metrics = sim.Metrics(data_root=data_root)
        self.user_parameter_table = self.metrics.filter_metrics(
            constants.USER_PARAMETERS_DEFINITION_ID
        )

    def html(self):
        """ Method for generating html layout for the
            user input parameter table.

        Returns:
            html layout: displays user input parameter table.

        """
        html_layout = html.Div(
            [
                dcc.Markdown(
                    """# User Input Parameters Table """,
                    style={"text-align": "center"},
                ),
                dash_table.DataTable(
                    id="user_parameter_table",
                    columns=[
                        {"name": i, "id": i}
                        for i in self.user_parameter_table.columns
                    ],
                    data=self.user_parameter_table.to_dict("records"),
                    style_table={"overflowX": "auto"},
                    style_cell={
                        "height": "auto",
                        # all three widths are needed
                        "minWidth": "180px",
                        "width": "180px",
                        "maxWidth": "400px",
                        "whiteSpace": "normal",
                        "text-align": "center",
                    },
                ),
            ]
        )
        return html_layout


class Lighting:
    """ This class contains methods for object lighting statistics
    visualization.

    Attributes:
        metrics(sim.Metrics): a collection of metrics records
        lighting (pandas.DataFrame): contains information about per-frame light
            color and orientation information.

    """

    X_Y_COLUMNS = ["x_rotation", "y_rotation"]
    COLOR_COLUMNS = ["color.r", "color.g", "color.b", "color.a"]

    def __init__(self, data_root):
        """
        Args:
            data_root(str): path to the dataset.

        """
        self.metrics = sim.Metrics(data_root=data_root)
        self.lighting = self._read_lighting_info()

    def _read_lighting_info(self):
        """ Method to obtain per-frame light color and orientation information.

        Returns:
            pandas.DataFrame: contains information about per-frame light color
                and orientation information.

        """
        metrics = self.metrics
        filtered_metrics = metrics.filter_metrics(
            constants.LIGHTING_INFO_DEFINITION_ID
        )
        colors = pd.json_normalize(filtered_metrics["color"])
        colors.columns = self.COLOR_COLUMNS
        combined = pd.concat(
            [filtered_metrics[self.X_Y_COLUMNS], colors], axis=1, join="inner"
        )

        return combined

    def _generate_figures_lighting(self):
        """ Method for generating plots for displaying light orientations,
            rotations and color distribution.

        Args:
            lighting(pandas.DataFrame): contains lighting information.

        Returns:
            plotly.graph_objects.Figure: chart to display light orientations,
                rotations and color distribution.

        """

        lighting_fig = rotation_plot(
            self.lighting,
            x="x_rotation",
            y="y_rotation",
            max_samples=constants.MAX_SAMPLES,
        )

        lighting_x_rot_fig = histogram_plot(
            self.lighting,
            x="x_rotation",
            x_title="Lighting Rotation (Degree)",
            y_title="Frequency",
            title="Distribution of Lighting Rotations along X direction",
            max_samples=constants.MAX_SAMPLES,
        )

        lighting_y_rot_fig = histogram_plot(
            self.lighting,
            x="y_rotation",
            x_title="Lighting Rotation (Degree)",
            y_title="Frequency",
            title="Distribution of Lighting Rotations along Y direction",
            max_samples=constants.MAX_SAMPLES,
        )

        lighting_redness_fig = histogram_plot(
            self.lighting,
            x="color.r",
            x_title="Lighting Color",
            y_title="Frequency",
            title="Distribution of Lighting Color Redness",
            max_samples=constants.MAX_SAMPLES,
            color_discrete_sequence=["indianred"],
        )

        lighting_greeness_fig = histogram_plot(
            self.lighting,
            x="color.g",
            x_title="Lighting Color",
            y_title="Frequency",
            title="Distribution of Lighting Color Greeness",
            max_samples=constants.MAX_SAMPLES,
            color_discrete_sequence=["MediumSeaGreen"],
        )

        lighting_blueness_fig = histogram_plot(
            self.lighting,
            x="color.b",
            x_title="Lighting Color",
            y_title="Frequency",
            title="Distribution of Lighting Color Blueness",
            max_samples=constants.MAX_SAMPLES,
        )

        return {
            "lighting_fig": lighting_fig,
            "lighting_x_rot_fig": lighting_x_rot_fig,
            "lighting_y_rot_fig": lighting_y_rot_fig,
            "lighting_redness_fig": lighting_redness_fig,
            "lighting_greeness_fig": lighting_greeness_fig,
            "lighting_blueness_fig": lighting_blueness_fig,
        }

    def html(self):
        """ Method for generating html layout for the
            lighting statistics.

        Returns:
            html layout: displays lighting graphs.

        """
        lighting_figures = self._generate_figures_lighting()

        html_layout = html.Div(
            [
                html.Div(
                    [
                        dcc.Markdown(
                            """# Lighting Orientations """,
                            style={"text-align": "center"},
                        ),
                        dcc.Graph(
                            id="lighting_fig",
                            figure=lighting_figures["lighting_fig"],
                        ),
                        dcc.Tabs(
                            [
                                dcc.Tab(
                                    label="Lighting along X direction",
                                    children=[
                                        dcc.Graph(
                                            id="lighting_x_rot_fig",
                                            figure=lighting_figures[
                                                "lighting_x_rot_fig"
                                            ],
                                        )
                                    ],
                                ),
                                dcc.Tab(
                                    label="Lighting along Y direction",
                                    children=[
                                        dcc.Graph(
                                            id="lighting_y_rot_fig",
                                            figure=lighting_figures[
                                                "lighting_y_rot_fig"
                                            ],
                                        )
                                    ],
                                ),
                            ]
                        ),
                    ]
                ),
                html.Div(
                    [
                        dcc.Graph(
                            id="lighting_redness_fig",
                            figure=lighting_figures["lighting_redness_fig"],
                        ),
                        dcc.Graph(
                            id="lighting_greeness_fig",
                            figure=lighting_figures["lighting_greeness_fig"],
                        ),
                        dcc.Graph(
                            id="lighting_blueness_fig",
                            figure=lighting_figures["lighting_blueness_fig"],
                        ),
                    ],
                    style={"columnCount": 3},
                ),
            ]
        )
        return html_layout


class ObjectPlacement:
    """ This class contains methods for object orientation statistics
    visualization.

    Attributes:
        metrics(sim.Metrics): a collection of metrics records
        lighting (pandas.DataFrame): contains information about per-frame light
            color and orientation information.

    """

    OBJECT_ORIENTATION = ("x_rot", "y_rot", "z_rot")

    def __init__(self, data_root):
        """

        Args:
            data_root(str): path to the dataset.

        """
        self.metrics = sim.Metrics(data_root=data_root)
        self.rotation = self._read_foreground_placement_info()

    def _read_foreground_placement_info(self):
        """ Method to obtain rotations of the foreground objects.

        Returns:
            pandas.DataFrame: contains information about foreground object
                orientations.

        """
        metrics = self.metrics
        filtered_metrics = metrics.filter_metrics(
            constants.FOREGROUND_PLACEMENT_INFO_DEFINITION_ID
        )
        combined = pd.DataFrame(
            filtered_metrics["rotation"].to_list(),
            columns=self.OBJECT_ORIENTATION,
        )

        return combined

    def _generate_figures_orientation(self, orientation):
        """ Method for generating object orientation statistics

        Args:
            orientation(pandas.DataFrame): contains object orientations
                along X, Y, Z direction.

        Returns:
            plotly.graph_objects.Figure: charts for object orientation
                statistics.

        """
        orientation_rotation_plot_fig = rotation_plot(
            orientation,
            x="x_rot",
            y="y_rot",
            z="z_rot",
            max_samples=constants.MAX_SAMPLES,
        )

        rotation_x_dir_fig = histogram_plot(
            orientation,
            x="x_rot",
            x_title="Object Rotation (Degree)",
            y_title="Frequency",
            title="Distribution of Object Rotations along X direction",
            max_samples=constants.MAX_SAMPLES,
        )

        rotation_y_dir_fig = histogram_plot(
            orientation,
            x="y_rot",
            x_title="Object Rotation (Degree)",
            y_title="Frequency",
            title="Distribution of Object Rotations along Y direction",
            max_samples=constants.MAX_SAMPLES,
        )

        rotation_z_dir_fig = histogram_plot(
            orientation,
            x="z_rot",
            x_title="Object Rotation (Degree)",
            y_title="Frequency",
            title="Distribution of Object Rotations along Z direction",
            max_samples=constants.MAX_SAMPLES,
        )

        return {
            "orientation_rotation_plot_fig": orientation_rotation_plot_fig,
            "rotation_x_dir_fig": rotation_x_dir_fig,
            "rotation_y_dir_fig": rotation_y_dir_fig,
            "rotation_z_dir_fig": rotation_z_dir_fig,
        }

    def html(self):
        """ Method for generating html layout for the object
            orientation statistics.

        Returns:
            html layout: displays object orientation graphs.

        """
        orientation_figures = self._generate_figures_orientation(self.rotation)

        html_layout = html.Div(
            [
                dcc.Markdown(
                    """# Object Orientations """, style={"text-align": "center"}
                ),
                dcc.Graph(
                    id="orientation_rotation",
                    figure=orientation_figures["orientation_rotation_plot_fig"],
                ),
                dcc.Tabs(
                    [
                        dcc.Tab(
                            label="Rotation along X direction",
                            children=[
                                dcc.Graph(
                                    id="rotation_x_dir",
                                    figure=orientation_figures[
                                        "rotation_x_dir_fig"
                                    ],
                                )
                            ],
                        ),
                        dcc.Tab(
                            label="Rotation along Y direction",
                            children=[
                                dcc.Graph(
                                    id="rotation_y_dir",
                                    figure=orientation_figures[
                                        "rotation_y_dir_fig"
                                    ],
                                )
                            ],
                        ),
                        dcc.Tab(
                            label="Rotation along Z direction",
                            children=[
                                dcc.Graph(
                                    id="rotation_z_dir",
                                    figure=orientation_figures[
                                        "rotation_z_dir_fig"
                                    ],
                                )
                            ],
                        ),
                    ]
                ),
            ],
        )
        return html_layout


def render_object_detection_layout(data_root):
    """ Method for displaying object detection statistics.

    Args:
        data_root(str): path to the dataset.

    Returns:
        html layout: displays graphs for rotation and
            lighting statistics for the object.

    """
    user_parameter = UserParameter(data_root)
    object_placement = ObjectPlacement(data_root)
    lighting = Lighting(data_root)
    scale_factor = ScaleFactor(data_root)
    object_detection_layout = html.Div(
        [
            html.Div(id="object_detection"),
            user_parameter.html(),
            object_placement.html(),
            lighting.html(),
            scale_factor.html(),
        ]
    )
    return object_detection_layout
