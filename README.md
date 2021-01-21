<img src="com.unity.perception/Documentation~/images/unity-wide.png" align="middle" width="3000"/>

<img src="com.unity.perception/Documentation~/images/banner2.PNG" align="middle"/>

# Perception

The Perception package provides a toolkit for generating large-scale datasets for perception-based machine learning training and validation. It is focused on a handful of camera-based use cases for now and will ultimately expand to other forms of sensors and machine learning tasks.

![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/5ab9a162-9dd0-4ba1-ba41-cf25378a927a)

[![license badge](https://img.shields.io/badge/license-Apache--2.0-green.svg)](LICENSE.md)

> com.unity.perception is in active development. Its features and API are subject to significant change as development progresses.

**[Quick Installation Instructions](com.unity.perception/Documentation~/SetupSteps.md)**  
Get your local Perception workspace up and running quickly. Recommended for users with prior Unity experience.

**[Perception Tutorial](com.unity.perception/Documentation~/Tutorial/TUTORIAL.md)**  
Detailed instructions covering all the important steps from installing Unity Editor, to creating your first Perception project, building a randomized Scene, and generating large-scale synthetic datasets by leveraging the power of Unity Simulation.  No prior Unity experience required.

**[Perception Manual](com.unity.perception/Documentation~/index.md)**  
Sample projects and documentation of the SDK.

## Package Documentation

This section contains in-depth documentation on inidividual components of the Perception package. 

|Feature|Description|
|---|---|
|[Labeling](GroundTruthLabeling.md)|A component that marks a GameObject and its descendants with a set of labels|
|[LabelConfig](GroundTruthLabeling.md#label-config)|An asset that defines a taxonomy of labels for ground truth generation|
|[Perception Camera](PerceptionCamera.md)|Captures RGB images and ground truth from a [Camera](https://docs.unity3d.com/Manual/class-Camera.html).|
|[DatasetCapture](DatasetCapture.md)|Ensures sensors are triggered at proper rates and accepts data for the JSON dataset.|
|[Randomization (Experimental)](Randomization/Index.md)|The Randomization tool set lets you integrate domain randomization principles into your simulation.|

## Local development
The repository includes two projects for local development in `TestProjects` folder, one set up for HDRP and the other for URP.

### Suggested IDE Setup
For closest standards conformity and best experience overall, JetBrains Rider or Visual Studio w/ JetBrains Resharper are suggested. For optimal experience, perform the following additional steps:
* To allow navigating to code in all packages included in your project, in your Unity Editor, navigate to `Edit -> Preferences... -> External Tools` and check `Generate all .csproj files.` 

## License
* [License](com.unity.perception/LICENSE.md)

## Citation
If you find this package useful, consider citing it using:
```
@misc{com.unity.perception2020,
    title={Unity {P}erception Package},
    author={{Unity Technologies}},
    howpublished={\url{https://github.com/Unity-Technologies/com.unity.perception}},
    year={2020}
}
```
