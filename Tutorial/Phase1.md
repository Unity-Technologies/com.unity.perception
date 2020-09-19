
# Perception Tutorial
## Phase 1: Setup and Basic Simulations

In this phase of the Perception tutorial, you will start from downloading and installing Unity Editor and the Perception package. You will then use our sample assets and provided components to easily generate a synthetic dataset for training an object-detection model. Through-out the tutorial, lines starting with bullet points denote the individual actions you will need to take in order to progress through the tutorial. This is while non-bulleted lines will provide additional context and explanation around the actions. If in a hurry, you can just follow the bullets!

### Step 1: Download Unity Editor and Create a New Project
* Navigate to [this](https://unity3d.com/get-unity/download/archive) page to download and install the latest version of Unity Editor 2019.4. 

When you first run Unity, you will be asked to open an existing project, or create a new one. 

* Open Unity and create a new project using the Universal Render Pipeline. Name your new project _**Perception Tutorial**_, as shown below. 

<p align="center">
<img src="Images/create_new_project.png" align="center" width="800"/>
</p>

### Step 2: Download the Perception Package and Import Samples
Once your new project is created and loaded, you will be presented with the Unity Editor interface. From this point, whenever we refer to _the editor_, we mean Unity Editor.
* From the top menu bar, open _**Window**_ -> _**Package Manager**_. 

As the name suggests, the _**Package Manager**_ is where you can download new packages, update or remove existing ones, and access a variety of information and additional actions for each package.

* Click on the _**+**_ sign at the top-left corner of the _**Package Manager**_ window and then choose the option _**Add package frim git URL...**_. 
* Enter the address `com.unity.perception` and click _**Add**_

It will take some time for the manager to download and import the package. Once the operation finishes, you will see the newly download Perception package automatically selected in the _**Package Manager**_, as depicted below:

<p align="center">
<img src="Images/package_manager.png" width="600"/>
</p>


Each package can come with a set of samples. As seen in the righthand panel, the Perception package includes a sample named _**Tutorial Files**_, which will be required for completing this tutorial. The sample files consist of example foreground and background objects (foreground: objects that the eventual machine learning model will try to detect, background: objects that will be placed in the background as distractions to for the machine learning model), randomizers, shaders, and other useful elements to work with during this tutorial.

* In the _**Package Manager**_ window, from the list of _**Samples**_ for the Perception package, click on the _**Import into Project**_ button for the sample named _**Tutorial Files**_.

Once the sample files are imported, they will be placed inside the `Assets/Samples/Perception` folder in your Unity project. You can view your project's folder structure and access your files from the _**Project**_ tab of the editor, as seen in the image below:

<p align="center">
<img src="Images/project_folders_samples.png" width="600"/>
</p>

* The _**Project**_ tab contains a search bar; use it to find the file named `ForwardRenderer.asset`, as shown below:

<p align="center">
<img src="Images/forward_renderer.png"/>
</p>

* Click on the found file to select it. Then, from the _**Inspector**_ tab of the editor, click on the _**Add Renderer Feature**_ button, and select _**Ground Truth Renderer Feature**_ from the dropdown menu:

<p align="center">
<img src="Images/forward_renderer_inspector.png" width="400"/>
</p>

This step prepares your project to render tailor-made images that will be later used for labeling the generates synthetic data.

### Step 3: Setup a Scene for Your Perception Simulation
Simply put, in Unity, Scenes contain any object that exists in the world. This world can be a game, or in this case, a perception-oriented simulation. Every new project contains a Scene named _**SampleScene**_, which is automatically openned when the project is created. We will now modify this scene to remove the parts we will not need and tailor it to this tutorial.

The _**Hierarchy**_ tab of the editor displays all the Scenes currently loaded, and all the objects currently present in each loaded Scene, as shown below:
<p align="center">
<img src="Images/hierarchy.png"/>
</p>

* Go ahead and remove everything shown in the hierarchy except for _**Main Camera**_ and _**Directional Light**_. 

To remove objects, select them and press _**Delete**_ (Windows) or _**cmd+delete**_ (Mac) on your keyboard. You can also right-click an object and click _**Delete**_. After this step, your Scene hierarchy should look like below: 
<p align="center">
<img src="Images/hierarchy_1.png" width="200"/>
</p>


