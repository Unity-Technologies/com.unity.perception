
# Perception Tutorial
## Phase 1: Setup and Basic Simulations

In this phase of the Perception tutorial, you will start from downloading and installing Unity Editor and the Perception package. You will then use our sample assets and provided components to easily generate a synthetic dataset for training an object-detection model. Through-out the tutorial, lines starting with bullet points denote the individual actions you will need to take in order to progress through the tutorial. This is while non-bulleted lines will provide additional context and explanation around the actions. If in a hurry, you can just follow the bullets!

### Step 1: Download Unity Editor and Create a New Project
* Navigate to [this](https://unity3d.com/get-unity/download/archive) page to download and install the latest version of Unity Editor 2019.4. 

When you first run Unity, you will be asked to open an existing project, or create a new one. 

* Open Unity and create a new project using the Universal Render Pipeline. Name your new project _**Perception Tutorial**_, as shown below. 

<img src="Images/create_new_project.png" align="middle"/>


### Step 2: Download the Perception Package
Once your new project is created and loaded, you will be presented with the Unity Editor interface. 
* From the top menu bar, open _**Window**_ -> _**Package Manager**_. 

As the name suggests, the _**Package Manager**_ is where you can download new packages, update or remove existing ones, and access a variety of information and additional actions for each package.

* Click on the _**+**_ sign at the top-left corner of the _**Package Manager**_ window and then choose the option _**Add package frim git URL...**_. 
* Enter the address `com.unity.perception` and click _**Add**_

It will take some time for the manager to download and import the package. Once the operation finishes, you will see the newly download Perception package automatically selected in the _**Package Manager**_, as depicted below:

<img src="Images/package_manager.png" align="middle"/>



Each package can come with a set of samples. As seen in the righthand panel, the Perception package includes a sample named _**Tutorial Files**_, which will be required for completing this tutorial. The sample files consist of example foreground and background objects (foreground: objects that the eventual machine learning model will try to detect, background: objects that will be placed in the background as distractions to for the machine learning model), randomizers, shaders, and other useful elements to work with during this tutorial.

* In the _**Package Manager**_ window, from the list of _**Samples**_ for the Perception package, click on the _**Import into Project**_ button for the sample named _**Tutorial Files**_.

Once the sample files are imported, they will be placed inside the `Assets/Samples/Perception` folder in your Unity project. You can view your project's folder structure and access your files from the _**Project**_ tab of Unity Editor, as seen in the image below:

<img src="Images/project_folders_samples.png" align="middle"/>

### Step 3: Setup a Scene for Your Perception Simulation
Simply put, in Unity, Scenes contain any object that exists in the world. This world can be a game, or in this case, a perception-oriented simulation. Every new project contains a Scene named _**SampleScene**_, which is automatically open  We will now modifysadaw d
