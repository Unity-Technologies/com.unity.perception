<img src="com.unity.perception/Documentation~/images/unity-wide.png" align="middle" width="3000"/>

<img src="com.unity.perception/Documentation~/images/banner2.PNG" align="middle"/>

![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/5ab9a162-9dd0-4ba1-ba41-cf25378a927a)

[![license badge](https://img.shields.io/badge/license-Apache--2.0-green.svg)](LICENSE.md)

> com.unity.perception is in active development. Its features and API are subject to significant change as development progresses.


# Perception Package ([Unity Computer Vision](https://unity.com/computer-vision))

The Perception package provides a toolkit for generating large-scale datasets for computer vision training and validation. It is focused on a handful of camera-based use cases for now and will ultimately expand to other forms of sensors and machine learning tasks.

Visit the [Unity Computer Vision](https://unity.com/computer-vision) page for more information on our tools and offerings!

## Getting Started

**[Quick Installation Instructions](com.unity.perception/Documentation~/SetupSteps.md)**  
Get your local Perception workspace up and running quickly. Recommended for users with prior Unity experience.

**[Perception Tutorial](com.unity.perception/Documentation~/Tutorial/TUTORIAL.md)**  
Detailed instructions covering all the important steps from installing Unity Editor, to creating your first computer vision data generation project, building a randomized Scene, and generating large-scale synthetic datasets by leveraging the power of Unity Simulation.  No prior Unity experience required.

**[Human Pose Labeling and Randomization Tutorial](com.unity.perception/Documentation~/HPTutorial/TUTORIAL.md)**  
Step by step instructions for using the keypoint, pose, and animation randomization tools included in the Perception package. It is recommended that you finish Phase 1 of the Perception Tutorial above before starting this tutorial.

## Documentation
In-depth documentation on individual components of the package. 

|Feature|Description|
|---|---|
|[Labeling](com.unity.perception/Documentation~/GroundTruthLabeling.md)|A component that marks a GameObject and its descendants with a set of labels|
|[Label Config](com.unity.perception/Documentation~/GroundTruthLabeling.md#label-config)|An asset that defines a taxonomy of labels for ground truth generation|
|[Perception Camera](com.unity.perception/Documentation~/PerceptionCamera.md)|Captures RGB images and ground truth from a [Camera](https://docs.unity3d.com/Manual/class-Camera.html).|
|[Dataset Capture](com.unity.perception/Documentation~/DatasetCapture.md)|Ensures sensors are triggered at proper rates and accepts data for the JSON dataset.|
|[Randomization](com.unity.perception/Documentation~/Randomization/Index.md)|The Randomization tool set lets you integrate domain randomization principles into your simulation.|

## Example Projects

### SynthDet

<img src="com.unity.perception/Documentation~/images/synthdet.png"/>

[SynthDet](https://github.com/Unity-Technologies/SynthDet) is an end-to-end solution for training a 2D object detection model using synthetic data.

### Unity Simulation Smart Camera example
<img src="com.unity.perception/Documentation~/images/smartcamera.png"/>

The [Unity Simulation Smart Camera Example](https://github.com/Unity-Technologies/Unity-Simulation-Smart-Camera-Outdoor) illustrates how the Perception package could be used in a smart city or autonomous vehicle simulation. You can generate datasets locally or at scale in [Unity Simulation](https://unity.com/products/unity-simulation).

## Local development
The repository includes two projects for local development in `TestProjects` folder, one set up for HDRP and the other for URP.

### Suggested IDE Setup
For closest standards conformity and best experience overall, JetBrains Rider or Visual Studio w/ JetBrains Resharper are suggested. For optimal experience, perform the following additional steps:
* To allow navigating to code in all packages included in your project, in your Unity Editor, navigate to `Edit -> Preferences... -> External Tools` and check `Generate all .csproj files.` 

## Known issues

* The Linux Editor 2019.4.7f1 and 2019.4.8f1 might hang when importing HDRP-based Perception projects. For Linux Editor support, use 2019.4.6f1 or 2020.1

## License
* [License](com.unity.perception/LICENSE.md)

## Support 

For general questions or concerns please contact the Computer Vision team at computer-vision@unity3d.com.

For feedback, bugs, or other issues please file a GitHub issue and the Computer Vision team will investigate the issue as soon as possible.

## Citation
If you find this package useful, consider citing it using:
```
@misc{com.unity.perception2021,
    title={Unity {P}erception Package},
    author={{Unity Technologies}},
    howpublished={\url{https://github.com/Unity-Technologies/com.unity.perception}},
    year={2020}
}
```
