# Configuration file for the Sphinx documentation builder.
#
# This file only contains a selection of the most common options. For a full
# list see the documentation:
# https://www.sphinx-doc.org/en/master/usage/configuration.html
import os
import sys

import pkg_resources

sys.path.insert(0, os.path.abspath("../.."))


# -- Project information -----------------------------------------------------

project = "datasetinsights"
copyright = "2020, Unity Technologies"
author = "Unity Technologies"

# The full version, including alpha/beta/rc tags
release = pkg_resources.get_distribution(project).version
napoleon_google_docstring = True

# -- General configuration ---------------------------------------------------

master_doc = "index"


# Add any Sphinx extension module names here, as strings. They can be
# extensions coming with Sphinx (named 'sphinx.ext.*') or your custom
# ones.
extensions = [
    "recommonmark",
    "sphinx.ext.autosectionlabel",
    "sphinx_rtd_theme",
    "sphinx.ext.napoleon",
    "sphinx_click",
]

source_suffix = {
    ".rst": "restructuredtext",
    ".txt": "markdown",
    ".md": "markdown",
}


# Add any paths that contain templates here, relative to this directory.
templates_path = ["_templates"]

# List of patterns, relative to source directory, that match files and
# directories to ignore when looking for source files.
# This pattern also affects html_static_path and html_extra_path.
exclude_patterns = []


# -- Options for HTML output -------------------------------------------------

# The theme to use for HTML and HTML Help pages.  See the documentation for
# a list of builtin themes.
#
html_theme = "sphinx_rtd_theme"

# Add any paths that contain custom static files (such as style sheets) here,
# relative to this directory. They are copied after the builtin static files,
# so a file named "default.css" will overwrite the builtin "default.css".
