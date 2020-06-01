# Setup for local development
* Clone the [Perception](https://github.com/Unity-Technologies/com.unity.perception) repository
* Install and use Unity latest [2019.3 Unity editor](https://unity.com/releases/2019-3) 

## Setting up a Project
Below are two options for getting started using the Perception package. Option 1 is opening existing test projects in the repository. Option 2 new Unity project and integrate the Perception package.

### Option 1: PerceptionHDRP & PerceptionURP Projects
The repository includes two projects for local development in `TestProjects` folder, one set up for HDRP and the other for URP. You can open these with the Unity Editor you installed in Setup instructions.

<img src="images/TestProjects.PNG" align="middle"/>

### Option 2: Create a new Project 
You can also set up a new or existing project to use Perception.
*The following instructions reference the Unity doc's page on [installing a local package](https://docs.unity3d.com/Manual/upm-ui-local.html)*

1. Create a new HDRP or URP project, or open an existing project
2. Open `Window` ->  `Package Manager`
	1. In the Package Manager window find and click the ***+*** button in the upper lefthand corner of the window
	2. Select the ***add package from disk*** option
	3. Navigate to the com.unity.perception folder in your cloned repository and select the package.json file

Once completed you can continue with the [getting started steps](com.unity.perception/Documentation~/GettingStarted.md).
