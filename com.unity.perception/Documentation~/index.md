<img src="images/banner2.PNG" align="middle"/>

# Unity Perception package (com.unity.perception)
The Perception package provides a toolkit for generating large-scale datasets for perception-based machine learning training and validation. It is focused on capturing ground truth for camera-based use cases for now and will ultimately expand to other forms of sensors and machine learning tasks.

> The Perception package is in active development. Its features and API are subject to significant change as development progresses.

[Installation instructions](SetupSteps.md)

[Setting up your first perception scene](GettingStarted.md)

[Randomizing your simulation](Randomization/Index.md)

## Example projects using Perception

### SynthDet

<img src="images/synthdet.png"/>

[SynthDet](https://github.com/Unity-Technologies/SynthDet) is an end-to-end solution for training a 2d object detection model using synthetic data.

### Unity Simulation Smart Camera Example
<img src="images/smartcamera.png"/>

The [Unity Simulation Smart Camera Example](https://github.com/Unity-Technologies/Unity-Simulation-Smart-Camera-Outdoor) illustrates how Perception could be used in a smart city or autonomous vehicle simulation. Datasets can be generated locally or at scale in [Unity Simulation](https://unity.com/products/unity-simulation).

## Package contents

|Feature|Description
|---|---|
|[Labeling](GroundTruth-Labeling.md)|Component which marks a GameObject and its descendants with a set of labels|
|[LabelConfig](GroundTruth-Labeling.md#LabelConfig)|Asset which defines a taxonomy of labels for ground truth generation|
|[Perception Camera](PerceptionCamera.md)|Captures RGB images and ground truth from a [Camera](https://docs.unity3d.com/Manual/class-Camera.html)|
|[DatasetCapture](DatasetCapture.md)|Ensures sensors are triggered at proper rates and accepts data for the JSON dataset|
|[Randomization](Randomization/Index.md)|Integrate domain randomization principles into your simulation|

## Known Issues

* The Linux Editor 2019.4.7f1 and 2019.4.8f1 have been found to hang when importing HDRP-based perception projects. For Linux Editor support, use 2019.4.6f1 or 2020.1