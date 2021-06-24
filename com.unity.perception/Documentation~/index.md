# About the Perception SDK
com.unity.perception provides a toolkit for generating large-scale datasets for perception-based machine learning training and validation. It is focused on a handful of camera-based use cases for now and will ultimately expand to other forms of sensors and machine learning tasks.

# Technical details
## Requirements

This version of _Perception_ is compatible Unity Editor 2019.3 and later

## Package contents
|Ground Truth|Captures semantic segmentation, bounding boxes, and other forms of ground truth.|
|---|---|
|Labeling|MonoBehaviour which marks an object and its descendants with a set of labels|
|Labeling Configuration|Asset which defines a taxonomy of labels used for ground truth generation |
|Perception Camera|Captures RGB images and ground truth on a Unity Camera|
|---|---|