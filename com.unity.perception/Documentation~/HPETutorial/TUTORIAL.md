# Human Pose Estimation Tutorial

In this tutorial, we will walk through the process of importing rigged humanoid models and animations of `.fbx` format into your computer vision data generation project, and using them to produce key-point and pose-estimation ground-truth data. We will use the tools and samples provided in the Perception package.

We strongly recommend you finish [Phase 1 of the Perception Tutorial](../Tutorial/Phase1.md) before continuing with this one, especially if you do not have prior experience with Unity Editor.

Through-out the tutorial, lines starting with bullet points followed by **":green_circle: Action:"** denote the individual actions you will need to perform in order to progress through the tutorial. This is while non-bulleted lines will provide additional context and explanation around the actions. If in a hurry, you can just follow the actions!

Steps included in this tutorial:

- [Human Pose Estimation Tutorial](#human-pose-estimation-tutorial)
    - [<a name="step-1">Step 1: Import `.fbx` Models and Animations</a>](#step-1-import-fbx-models-and-animations)
    - [<a name="step-2">Step 2: Set Up a Humanoid Character in a Scene</a>](#step-2-set-up-a-humanoid-character-in-a-scene)


  
  -
- [Step 3: Set Up the Perception Camera for Key Point Annotation](#step-3)
- [Step 4: Configure Human Pose Estimation](#step-4)
- [Step 5: Randomize the Humanoid Character's Animations](#step-5)

### <a name="step-1">Step 1: Import `.fbx` Models and Animations</a> 

This tutorial assumes that you have already created a Unity project, installed the Perception package, and set up a Scene with a `Perception Camera` inside. If this is not the case, please follow **steps 1 to 3** of [Phase 1 of the Perception Tutorial](../Tutorial/Phase1.md). 

* **:green_circle: Action**: Open the project you created in the Perception Tutorial steps mentioned above. Duplicate `TutorialScene` and name the new Scene `HPE_Scene`. Open `HPE_Scene`.

We will use this duplicated Scene in this tutorial so that we do not lose our grocery object detection setup from the Perception Tutorial.

* **:green_circle: Action**: If your Scene already contains a Scenario object from the Perception Tutorial, remove all previously added Randomizers from this Scenario.
* **:green_circle: Action**: If your Scene does not already contains a Scenario, create an empty GameObject, name it `Simulation Scenario`, and add a `Fixed Length Scenario` component to it.

Your Scenario should now look like this:

<p align="center">
<img src="Images/scenario_empty.png" width="400"/>
</p>

* **:green_circle: Action**: Select `Main Camera` and in the _**Inspector**_ view of the `Perception Camera` component, **disable** all previously added labelers using the check-mark in front of each. We will be using a new labeler in this tutorial.

We now need to import the sample files required for this tutorial.  

* **:green_circle: Action**: Open _**Package Manager**_ and select the Perception package, which should already be present in the navigation pane to the left side. 
* **:green_circle: Action**: From the list of ***Samples*** for the Perception package, click on the ***Import into Project*** button for the sample bundle named _**Human Pose Estimation**_.

Once the sample files are imported, they will be placed inside the `Assets/Samples/Perception` folder in your Unity project, as seen in the image below:

<p align="center">
<img src="Images/project_folders_samples.png" width="600"/>
</p>

* **:green_circle: Action**: Select all of the asset inside the `Assets/Samples/Perception/<perception-package-version>/Human Pose Estimation/Models and Animations`.
* **:green_circle: Action**: In the _**Inspector**_ tab, navigate to the _**Rig**_ section and set `Animation Type` to `Humanoid`, then click _**Apply**_.

This will make sure all animations included in the sample `.fbx` files are ready to be used on a rigged humanoid model.

### <a name="step-2">Step 2: Set Up a Humanoid Character in a Scene</a>

* **:green_circle: Action**: Drag and drop the file named `Player` into your Scene _**Hierarchy**_.
* **:green_circle: Action**: Select the new `Player` object in the Scene and in the _**Inspector**_ tab set its transform's position and rotation according to the image below to make the character face the camera.

<p align="center">
<img src="Images/character_transform.png" width="800"/>
</p>

You will see that the object it already has an `Animator` component attached. This is because we earlier set the `Animation Types` for all files to `Humanoid`.
We will now need to attach an `Animation Controller` to the `Animator` component, in order for our character to animate.

* **:green_circle: Action**: Create a new `Animation Controller` asset in your `Assets` folder and name it `TestAnimationController`.
* **:green_circle: Action**: Double click the new controller to open it. Then right click in the empty area and select _**Create State**_ -> _**Empty**_. 
  
<p align="center">
<img src="Images/anim_controller_1.png" width="600"/>
</p>

This will create a new state and attach the Entry state to it with a new transition edge. This means the controller will always move to this new state as soon as the `Animator` component is awoken. In this example, this will happen when the **▷** button is pressed and the simulation starts.

* **:green_circle: Action**: Click on the state named `New State`. Then, in the _**Inspector**_ tab click the small circle next to `Motion` to select an animation clip.

In the selector window that pops up, you will see several clips named `Take 001`. These are animation clips that are bundled inside of the sample `.fbx` files you imported into the project. 

* **:green_circle: Action**: Select the animation clip originating from the `TakeObjects.fbx` file, as seen below:

<p align="center">
<img src="Images/select_clip.png" width="600"/>
</p>

* **:green_circle: Action**: Assign `TestAnimationController` to the `Controller` property of the `Player` object's `Animator` component. 

<p align="center">
<img src="Images/assign_controller.png" width="400"/>
</p>

If you run the simulation now you will see the character going through a series of three animations for picking up a hypothetical object as seen in the GIF below. This is one complete run of the `TakeObjects` animation clip we selected earlier.

<p align="center">
<img src="Images/take_objects.gif" width="600"/>
</p> 


### <a name="step-3">Step 3: Set Up the Perception Camera for Key Point Annotation</a> 

Now that we have our character performing animations, let's modify our `Perception Camera` to report the character's key points in the output dataset, updating frame by frame as they animate.

* **:green_circle: Action**: Add a `KeyPointLabeler` to the list of labelers in `Perception Camera`.

Similar to the labelers we used in the Perception Tutorial, we will need a label configuration for this new labeler. 

* **:green_circle: Action**: In the _**Project**_ tab, right-click the `Assets` folder, then click _**Create -> Perception -> Id Label Config**_. Name the new asset `HPE_IdLabelConfig`.
* **:green_circle: Action**: Add the label `MyCharacter` to the newly created config.

> :information_source: You can use any label string, as long as you assign the same label to the `Player` object in the next step.

* **:green_circle: Action**: Add a `Labeling` component to the `Player` object in the Scene.
* **:green_circle: Action**: In the _**Inspector**_ UI for this new `Labeling` component, expand `HPE_IdLabelConfig` and click _**Add to Labels** on `MyCharacter`.

<p align="center">
<img src="Images/add_label_from_config.png" width="400"/>
</p> 

* **:green_circle: Action**: Return to `Perception Camera` and assign `HPE_IdLabelConfig` to the `KeyPointLabeler`'s label configuration property.

The labelers should now look like the image below:

<p align="center">
<img src="Images/keypoint_labeler.png" width="500"/>
</p> 

Note the `CocoKeyPointTemplate` asset that is already assigned as the `Active Template`. This template will tell the labeler how to map default Unity rig joints to human joint labels in the popular COCO dataset, so that the output of the labeler can be easily converted to COCO format. Later in this tutorial, we will learn how to add more joints to our character and how to customize joint mapping templates.

<p align="center">
<img src="Images/take_objects_keypoints.gif" width="600"/>
</p> 

You can now check out the output dataset to see what the annotations look like. To do this, click the _**Show Folder**_ button in the `Perception Camera` UI, then navigate inside to the dataset folder to find the `captures_000.json` file. Here is an example annotation for the first frame of our test-case here:


```json
"pose": "unset",
  "keypoints": [
    {
      "index": 0,
      "x": 0.0,
      "y": 0.0,
      "state": 0
    },
    {
      "index": 1,
      "x": 649.05615234375,
      "y": 300.65264892578125,
      "state": 2
    },
    {
      "index": 2,
      "x": 594.4522705078125,
      "y": 335.8978271484375,
      "state": 2
    },
    {
      "index": 3,
      "x": 492.46444702148438,
      "y": 335.72491455078125,
      "state": 2
    },
    {
      "index": 4,
      "x": 404.89456176757813,
      "y": 335.57647705078125,
      "state": 2
    },
    {
      "index": 5,
      "x": 705.89404296875,
      "y": 335.897705078125,
      "state": 2
    },
    {
      "index": 6,
      "x": 807.74688720703125,
      "y": 335.7244873046875,
      "state": 2
    },
    {
      "index": 7,
      "x": 895.1993408203125,
      "y": 335.57574462890625,
      "state": 2
    },
    {
      "index": 8,
      "x": 612.51654052734375,
      "y": 509.065185546875,
      "state": 2
    },
    {
      "index": 9,
      "x": 608.50006103515625,
      "y": 647.0631103515625,
      "state": 2
    },
    {
      "index": 10,
      "x": 611.7791748046875,
      "y": 797.7828369140625,
      "state": 2
    },
    {
      "index": 11,
      "x": 682.175048828125,
      "y": 509.06524658203125,
      "state": 2
    },
    {
      "index": 12,
      "x": 683.1016845703125,
      "y": 649.64434814453125,
      "state": 2
    },
    {
      "index": 13,
      "x": 686.3271484375,
      "y": 804.203857421875,
      "state": 2
    },
    {
      "index": 14,
      "x": 628.012939453125,
      "y": 237.50531005859375,
      "state": 2
    },
    {
      "index": 15,
      "x": 660.023193359375,
      "y": 237.50543212890625,
      "state": 2
    },
    {
      "index": 16,
      "x": 0.0,
      "y": 0.0,
      "state": 0
    },
    {
      "index": 17,
      "x": 0.0,
      "y": 0.0,
      "state": 0
    }
  ]
}
```