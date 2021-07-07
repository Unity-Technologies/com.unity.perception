import os

import dash


def _init_app():
    """ Intializes the dash app."""

    this_dir = os.path.dirname(os.path.abspath(__file__))
    css_file = os.path.join(this_dir, "stylesheet.css")
    app = dash.Dash(
        __name__,
        external_stylesheets=[css_file],
        suppress_callback_exceptions=True,
    )
    return app


_app = _init_app()


def get_app():
    return _app
