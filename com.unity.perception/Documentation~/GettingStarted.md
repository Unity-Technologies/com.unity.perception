# Getting Started with SynthDet
This will provide a step by step instructions on creating a new scene using the Perception features to create semantic data and image captures. These steps should work with both options for setup steps of using a existing project or creating a new project. The goal is to have a working scene by the end of these instructions that will provide you with a dataset, rgb images captures, and segmentic data.

## Step 1: Create a new scene and camera
1. Create a new scene File-> New Scene
2. Save the Scene File-> Save and give it a name, i.e created scene name is PerceptionScene
3. Select the Main Camera and reset the Position transform to 0 
4. In the Hierarchy window select the main camera
	1. In the inspector panel of the main camera select Add Component
	2. Add a script called Perception Camera  
5. Fill out the Description field in the script 
	1. i.e example description is Perception Sample 
<img src="docs/images/MainCameraConfig.PNG" align="middle"/>

## Step 2: Create labeled objects
1. In the Hierarchy window right click -> Go to 3D Object -> Select Cube 
2. Select the cube
	1. Adjust the position transform of the cube to 1, 0, 5 (X,Y,Z)
	2. Adjust the rotation of the cube to 0, 45, 0 (X,Y,Z)
3. In the Hierarchy window right click -> Go to 3D Object -> Select Cube 
4. Change the name of the cube to Box in the Inspector panel
	1. Adjust the position transform of the cube to -1, 0, 5 (X,Y,Z)
	2. Adjust the rotation of the cube to 0, 45, 0 (X,Y,Z)
4. In the Hierarchy window right click -> Go to 3D Object -> Select Cube 
5. Change the name of the cube to Crate in the Inspector panel
	1. Adjust the position transform of the cube to 0, 0, 6.5 (X,Y,Z)
	2. Adjust the rotation of the cube to 0, 45, 0 (X,Y,Z)
<img src="docs/images/CompletedScene.PNG" align="middle"/>
6. For each object in the scene that was created, from the inspector panel add the script called Labeling 
	1. Click the **+**
	2. In the text field add the name of the object i.e Crate 
<img src="docs/images/LabeledObject.PNG" align="middle"/>
7. In the Project panel right click -> Perception -> Labeling Configuration
8. Select the Labeling Configuration created in the project panel 
	1. Click the **+**
	2. In the label text field add the same text that the Label script contains on the object 
	3. Add a numerical value to the value field 
		1. Make sure the labels all have different values, for this example use values of 10,000
<img src="docs/images/LabelingConfigurationFinished.PNG" align="middle"/>
9. Select the Main Camera in the Hierarchy panel 
	1. In the Perception Camera script in the Labeling Configuration field add the Labeling Configuration script created in previous step 
<img src="docs/images/MainCameraConfig.PNG" align="middle"/>

## Step 3: Checking local files
1. Press play in the editor and allow the scene to run for 10 seconds before ending playmode
2. In the console log you will see a Shutdown in Progress message that will show a file path to the location of the created data from the labeling objects 
3. The file path is the Application Persistent Path + /Defaultcompany/UnityTestFramework/<Hash Key>
	1. Example file path on a Windows PC : *C:/Users/<User Name>/AppData/LocalLow/DefaultCompany/UnityTestFramework\2e10ec21-9d97-4cee-b5a2-7e95e299afa4\RGB18f61842-ef8d-4b31-acb5-cb1da36fb7b1*
4. In the output path for the Labeling content you can verify the following data is present:
	1. RGB capture 
	2. Semantic Segmentation
	3. Logs
	4. Dataset
<img src="docs/images/rgb_2.png" align="middle"/>
<img src="docs/images/segmentation_2.png" align="middle"/>