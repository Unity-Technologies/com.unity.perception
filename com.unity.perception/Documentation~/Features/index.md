<p align="center">
  <img src="../images/unity-wide-whiteback.png" align="middle" width="600"/>
</p>

# Features Overview

This page serves as the hub for all Perception-related documentation. We have broken it up into sections to make it easier to search through. Let us know via the **[GitHub Issues](https://github.com/Unity-Technologies/com.unity.perception/issues)** page if you find outdated content or missing documentation!

## Fundamentals

| Feature                                                   | Description                                                                                         |
|-----------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| [Perception Camera](../PerceptionCamera.md)               | Captures RGB images and ground truth from a [Camera](https://docs.unity3d.com/Manual/               | [Output Endpoint](CustomEndpoints.md)       | Currently supported output endpoints are: `No Output`, `Perception` endpoint, and `Solo` endpoint.       |
| [SOLO Schema](../Schema/SoloSchema.md)                    | Schema for annotation, metric, and ground-truth data for the default SOLO endpoint                  |
| [Labeling](../GroundTruthLabeling.md)                     | A component that marks a GameObject and its descendants with a set of labels                        |
| [Label Config](../GroundTruthLabeling.md#label-config)    | An asset that defines a taxonomy of labels for ground truth generation                              |
| [Randomization](../Randomization/index.md)                | The Randomization tool set lets you integrate domain randomization principles into your simulation. |
| [FAQ](../FAQ/FAQ.md)                                      | Frequently Asked Questions about common workflows and issues.                                       |
| [Legacy Perception Schema](../Schema/PerceptionSchema.md) | Schema for annotation, metric, and ground-truth data for the legacy Perception endpoint             |

## Labeling

| Feature                                                | Description                                                                                              |
|--------------------------------------------------------|----------------------------------------------------------------------------------------------------------|
| [Labeling](../GroundTruthLabeling.md)                  | A component that marks a GameObject and its descendants with a set of labels                             |
| [Label Config](../GroundTruthLabeling.md#label-config) | An asset that defines a taxonomy of labels for ground truth generation                                   |
| [Bounding Box 2D Labeler](BoundingBox2DLabeler.md)     | Capture 2D bounding boxes for visible labeled objects.                                                   |
| [Hierarchical Bounding Boxes](BoundingBoxHierarchy.md) | How to combine bounding boxes of objects with parent-child hierarchical relationships during runtime.    |
| [Bounding Box 3D Labeler](BoundingBox3DLabeler.md)     | Capture 3D bounding boxes for visible labeled objects.                                                   |
| [Keypoint Labeler](KeypointLabeler.md)                 | Record the screen locations of specific points on labeled objects such as keypoints on humans.           |
| [Metadata Labeler](MetadataLabeler.md)                 | Reporting object-level or environment-level metadata information during runtime.                         |

## Randomization

| Feature                                               | Description                                                                                                                               |
|-------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------|
| [Randomization](../Randomization/index.md)            | The Randomization toolset lets you integrate domain randomization principles into your simulation.                                        |
| [Scenarios](../Randomization/Scenarios.md)            | Scenarios control execution flow of your simulation – how many iterations to run the simulation, what randomizers to use, etc.            |
| [Randomizers](../Randomization/Randomizers.md)        | Randomizers encapsulate specific randomization activities to perform during the lifecycle of a randomized simulation.                     |
| [Randomizer Tags](../Randomization/RandomizerTags.md) | RandomizerTags are the primary mechanism by which Randomizers query for a certain subset of GameObjects to randomize within a simulation. |
| [Parameters](../Randomization/Parameters.md)          | Parameters are classes that utilize Samplers to deterministically generate random typed values.                                           |
| [Samplers](../Randomization/Samplers.md)              | Samplers generate bounded random float values by sampling from probability distributions.                                                 |

## Data Generation

| Feature                                     | Description                                                                                              |
|---------------------------------------------|----------------------------------------------------------------------------------------------------------|
| [Perception Camera](../PerceptionCamera.md) | Captures RGB images and ground truth from a [Camera](https://docs.unity3d.com/Manual/class-Camera.html). |
| [Dataset Capture](../DatasetCapture.md)     | Ensures sensors are triggered at proper rates and accepts data for the JSON dataset.                     |
| [Output Endpoint](CustomEndpoints.md)       | Currently supported output endpoints are: `No Output`, `Perception` endpoint, and `Solo` endpoint.       |
| [Metadata Labeler](MetadataLabeler.md)      | Reporting object-level or environment-level metadata information during runtime.                         |
