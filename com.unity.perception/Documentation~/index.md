<img src="images/banner2.PNG" align="middle"/>

# Unity Perception Package Documentation (com.unity.perception)

Visit the pages below for in-depth documentation on inidividual components of the package. 

|Feature|Description|
|---|---|
|[Labeling](GroundTruthLabeling.md)|A component that marks a GameObject and its descendants with a set of labels|
|[LabelConfig](GroundTruthLabeling.md#label-config)|An asset that defines a taxonomy of labels for ground truth generation|
|[Perception Camera](PerceptionCamera.md)|Captures RGB images and ground truth from a [Camera](https://docs.unity3d.com/Manual/class-Camera.html).|
|[DatasetCapture](DatasetCapture.md)|Ensures sensors are triggered at proper rates and accepts data for the JSON dataset.|
|[Randomization (Experimental)](Randomization/Index.md)|The Randomization tool set lets you integrate domain randomization principles into your simulation.|

## Preview package

This package is available as a preview, so it is not ready for production use. The features and documentation in this package might change before it is verified for release.

## Known issues

* The Linux Editor 2019.4.7f1 and 2019.4.8f1 might hang when importing HDRP-based Perception projects. For Linux Editor support, use 2019.4.6f1 or 2020.1

## Other Resources

**[Quick Installation Instructions](com.unity.perception/Documentation~/SetupSteps.md)**  
Get your local Perception workspace up and running quickly. Recommended for users with prior Unity experience.

**[Perception Tutorial](com.unity.perception/Documentation~/Tutorial/TUTORIAL.md)**  
Detailed instructions covering all the important steps from installing Unity Editor, to creating your first Perception project, building a randomized Scene, and generating large-scale synthetic datasets by leveraging the power of Unity Simulation.  No prior Unity experience required.

### Example projects using Perception

#### SynthDet

<img src="images/synthdet.png"/>

[SynthDet](https://github.com/Unity-Technologies/SynthDet) is an end-to-end solution for training a 2D object detection model using synthetic data.

#### Unity Simulation Smart Camera example
<img src="images/smartcamera.png"/>

The [Unity Simulation Smart Camera Example](https://github.com/Unity-Technologies/Unity-Simulation-Smart-Camera-Outdoor) illustrates how Perception could be used in a smart city or autonomous vehicle simulation. You can generate datasets locally or at scale in [Unity Simulation](https://unity.com/products/unity-simulation).
