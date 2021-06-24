# Setup for local development
* Clone the [Perception](https://github.com/Unity-Technologies/com.unity.perception) repository
* Install and use Unity latest [2019.3 Unity editor](https://unity.com/releases/2019-3) 

## Setting up a Project
Below are two options for getting started using the Perception package. Option 1 is opening existing test projects in the repository. Option 2 new Unity project and integrate the Perception package.

### Option 1: PerceptionHDRP & PerceptionURP Projects
The repository includes two projects for local development in `TestProjects` folder, one set up for HDRP and the other for URP. You can open these with the Unity
editor you installed in Setup instructions.

<img src="images/TestProjects.PNG" align="middle"/>

### Option 2: Create a new Project 
These option is walkthrough in creating a new project, then adding the Perception SDK package to the project for development use.
*The following instructions reference the Unity doc's page on [installing a local package](https://docs.unity3d.com/Manual/upm-ui-local.html)*

#### Create a new project 
1. Create a new HDRP project or open an existing project
	1. Creating anew HDRP project can be done by creating a new project using the HDRP template 
2. Back in Unity editor, got Window ->  Package Manager
	1. Add the High Definition RP package, version 7.1.2 or later from the packages list 
	2. In the Package Manager window find and click the ***+*** button in the upper lefthand corner of the window
	3. Select the ***add package from disk*** option
	4. Navigate to the com.unity.perception folder in your cloned repository and select the package.json file
3. Once you have a project with Perception SDK installed you can move forward to the Getting Started walkthrough 

Once completed you can move on to the getting started steps, click [here](Documentation~/GettingStarted.md) to start project setup.