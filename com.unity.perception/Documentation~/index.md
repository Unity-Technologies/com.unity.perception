<img src="images/banner2.PNG" align="middle"/>

# Unity Perception package (com.unity.perception)
The Perception package provides a toolkit for generating large-scale datasets for perception-based machine learning training and validation. It is focused on capturing ground truth for camera-based use cases for now and will ultimately expand to other forms of sensors and machine learning tasks.

> The Perception package is in active development. Its features and API are subject to significant change as development progresses.

[Installation instructions](SetupSteps.md)

[Setting up your first perception scene](GettingStarted.md)

## Package contents

|Feature|Description
|---|---|
|[Labeling](GroundTruth-Labeling.md)|Component which marks a GameObject and its descendants with a set of labels|
|[Labeling Configuration](GroundTruth-Labeling.md#LabelingConfiguration)|Asset which defines a taxonomy of labels for ground truth generation|
|[Perception Camera](PerceptionCamera.md)|Captures RGB images and ground truth from a [Camera](https://docs.unity3d.com/Manual/class-Camera.html)|
|[SimulationManager](SimulationManager.md)|Ensures sensors are triggered at proper rates and accepts data for the JSON dataset|