from setuptools import find_packages, setup

# TODO: add versions for packages
setup(
    name="pyrception-utils",
    version="0.1.1",
    description="Pyrception-Utils: A toolkit for managing Unity Perception SDK datasets.",
    author="Unity Technologies",
    packages=find_packages(),
    python_requires=">=3.7",
    install_requires=[
        "Pillow>=8.1.0",
        "streamlit==0.75.0",
        "google-cloud-storage==1.19.0",
        "gcsfs==0.7.1",
    ],
    entry_points={"console_scripts": ["pyrception-utils=pyrception_utils.cli:main"]},
)
