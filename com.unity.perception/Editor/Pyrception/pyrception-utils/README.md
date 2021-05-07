# Pyrception Utils

Pyrception Utils: A toolkit for managing [Unity Perception SDK datasets](https://github.com/Unity-Technologies/com.unity.perception).

[API Reference](docs/pyrception-utils.md)

## Dependencies

* python >= 3.7
* python virtual environment (optional but recommended)

## Installation

```bash
> git clone git@github.cds.internal.unity3d.com:unity/pyrception-utils.git
> cd pyrception-utils
> pip install -e .
```

## Dataset Preview Tool

The pyrception-utils package includes a perception dataset preview cli tool built in streamlit. You can use this tool as follows:
```bash
> pyrception-utils preview --data=<path_to_a_perception_dataset_enclosing_folder>
```

Here, <path_to_a_perception_dataset_enclosing_folder> is the path to the folder that contains one or more perception dataset folders, not
the path to a dataset folder. For example, on a mac, this would be:

```bash
> pyrception-utils preview --data=/Users/<username>/Library/Application\ Support/DefaultCompany/<ProjectName>
```

where <username> is your mac username and <ProjectName> is the Unity project name that uses the perception SDK to generate the
data.

## PyrceptionDataset

The *PyrceptionDataset* class is a simple iterator that can be used to serialize data generated from the perception SDK and used
in other frameworks such as PyTorch or Tensorflow. Checkout it out [here](pyrception_utils/pyrception.py).


## Developers

To install the pre commit hooks run

```bash
> pip install pre-commit
> pre-commit install
```

To run the pre-commit checks manually

```bash
> pre-commit run --all-files
```

# Converting to public repository
Any and all Unity software of any description (including components) (1) whose source is to be made available other than under a Unity source code license or (2) in respect of which a public announcement is to be made concerning its inner workings, may be licensed and released only upon the prior approval of Legal.
The process for that is to access, complete, and submit this [FORM](https://docs.google.com/forms/d/e/1FAIpQLSe3H6PARLPIkWVjdB_zMvuIuIVtrqNiGlEt1yshkMCmCMirvA/viewform).
