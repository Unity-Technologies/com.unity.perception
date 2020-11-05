# Installing the Perception package in your project

![ReleaseBadge](https://badge-proxy.cds.internal.unity3d.com/5ab9a162-9dd0-4ba1-ba41-cf25378a927a)

This page provides brief instructions on installing the Perception package. Head over to the [Perception Tutorial](Tutorial/TUTORIAL.md) for more detailed instructions and steps for building a sample project.


1. Install the latest version of 2019.4.x or 2020.1.x Unity Editor from [here](https://unity3d.com/get-unity/download/archive). (Perception has not been tested on Unity versions newer than 2020.1)
1. Create a new HDRP or URP project, or open an existing project.
1. Open `Window` ->  `Package Manager`
	1. In the Package Manager window find and click the ***+*** button in the upper lefthand corner of the window
	1. Select ***Add package from git URL...***
	1. Enter `com.unity.perception` and click ***Add***

Note that although the Perception package is compatible with both URP and HDRP, Unity Simulation currently only supports URP projects, therefore a URP project is recommended. 

If you want a specific version of the package, append the version to the end of the "git URL". Ex. `com.unity.perception@0.1.0-preview.4`

To install from a local clone of the repository, see [installing a local package](https://docs.unity3d.com/Manual/upm-ui-local.html) in the Unity manual.
