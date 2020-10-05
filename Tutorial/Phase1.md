
# Perception Tutorial
## Phase 1: Setup and Basic Randomizations

In this phase of the Perception tutorial, you will start from downloading and installing Unity Editor and the Perception package. You will then use our sample assets and provided components to easily generate a synthetic dataset for training an object-detection model. 

Through-out the tutorial, lines starting with bullet points followed by **"Action:"** denote the individual actions you will need to perform in order to progress through the tutorial. This is while non-bulleted lines will provide additional context and explanation around the actions. If in a hurry, you can just follow the actions!

Steps included this phase of the tutorial:
- [Step 1: Download Unity Editor and Create a New Project](#step-1)
- [Step 2: Download the Perception Package and Import Samples](#step-2)
- [Step 3: Setup a Scene for Your Perception Simulation](#step-3)
- [Step 4: Specify Ground-Truth and Setup Object Labels](#step-4)
- [Step 5: Add and Set-up Randomizers](#step-5)
- [Step 6: Generate and Verify Synthetic Data](#step-6)


### <a name="step-1">Step 1: Download Unity Editor and Create a New Project</a> 
* **Action**: Navigate to [this](https://unity3d.com/get-unity/download/archive) page to download and install the latest version of Unity Editor 2020.1.x. (The tutorial has not yet been tested on newer versions.)

An alternative approach is to first install [_**Unity Hub**_](https://unity3d.com/get-unity/download), which will allow you to have multiple versions of Unity on your computer, and make it easier to manage your Unity projects and the versions of Unity they will use. 

During the installation of Unity, you will be asked to choose which modules you would like to include. This will depend on the types of applications you eventually intend to build with your Unity installation; however, for the purposes of this tutorial, we need to make make sure _**Linux Build Support**_ is checked. In addition, if you do not already have _**Visual Studio**_ on your computer, the wizard will give you an option to install it. Go ahead and check this option, as we will need _**Visual Studio**_ for writing some simple scripts in Phase 2 of the tutorial. 

* **Action**: Make sure the _**Linux Build Support**_ and _**Visual Studio**_ installation options are checked when selecting modules during installation.

When you first run Unity, you will be asked to open an existing project, or create a new one. 

* **Action**: Open Unity and create a new project using the Universal Render Pipeline. Name your new project _**Perception Tutorial**_, and specify a desired location as shown below. 

<p align="center">
<img src="Images/create_new_project.png" align="center" width="800"/>
</p>

### <a name="step-2">Step 2: Download the Perception Package and Import Samples</a> 

Once your new project is created and loaded, you will be presented with the Unity Editor interface. From this point on, whenever we refer to _**the editor**_, we mean Unity Editor.
* **Action**: From the top menu bar, open _**Window**_ -> _**Package Manager**_. 

As the name suggests, the _**Package Manager**_ is where you can download new packages, update or remove existing ones, and access a variety of information and additional actions for each package.

* **Action**: Click on the _**+**_ sign at the top-left corner of the _**Package Manager**_ window and then choose the option _**Add package frim git URL...**_. 
* **Action**: Enter the address `com.unity.perception` and click _**Add**_.

**Note:** If you would like a specific version of the package, you can append the version to the end of the url. For example `com.unity.perception@0.1.0-preview.5`. For this tutorial, **we do not need to add a version**. You can also install the package from a local clone of the Perception repository. More inofrmation on installing local pacakges is available [here](https://docs.unity3d.com/Manual/upm-ui-local.html).

It will take some time for the manager to download and import the package. Once the operation finishes, you will see the newly download Perception package automatically selected in the _**Package Manager**_, as depicted below:

<p align="center">
<img src="Images/package_manager.png" width="600"/>
</p>


Each package can come with a set of samples. As seen in the righthand panel, the Perception package includes a sample named _**Tutorial Files**_, which will be required for completing this tutorial. The sample files consist of example foreground and background objects, randomizers, shaders, and other useful elements to work with during this tutorial. **Foreground** objects are those that the eventual machine learning model will try to detect, and **background** objects will be placed in the background as distractors for the model.

* **Action**: In the _**Package Manager**_ window, from the list of _**Samples**_ for the Perception package, click on the _**Import into Project**_ button for the sample named _**Tutorial Files**_.

Once the sample files are imported, they will be placed inside the `Assets/Samples/Perception` folder in your Unity project. You can view your project's folder structure and access your files from the _**Project**_ tab of the editor, as seen in the image below:

<p align="center">
<img src="Images/project_folders_samples.png" width="600"/>
</p>

* **Action**: The _**Project**_ tab contains a search bar; use it to find the file named `ForwardRenderer.asset`, as shown below:

<p align="center">
<img src="Images/forward_renderer.png"/>
</p>

* **Action**: Click on the found file to select it. Then, from the _**Inspector**_ tab of the editor, click on the _**Add Renderer Feature**_ button, and select _**Ground Truth Renderer Feature**_ from the dropdown menu:

<p align="center">
<img src="Images/forward_renderer_inspector.png" width="400"/>
</p>

This step prepares your project to render tailor-made images that will be later used for labeling the generated synthetic data.

### <a name="step-3">Step 3: Setup a Scene for Your Perception Simulation</a> 
Simply put, in Unity, Scenes contain any object that exists in the world. This world can be a game, or in this case, a perception-oriented simulation. Every new project contains a Scene named `SampleScene`, which is automatically openned when the project is created. This Scenes comes with several objects and settings that we do not need, so let's create a new one. 

* **Action**: In the _**Project**_ tab, right-click on the `Assets/Scenes` folder and click _**Create -> Scene**_. Name this new Scene `TutorialScene` and double-click on it to open it. 

The _**Hierarchy**_ tab of the editor displays all the Scenes currently loaded, and all the objects currently present in each loaded Scene, as shown below:
<p align="center">
<img src="Images/hierarchy.png" width="700"/>
</p>

As seen above, the new Scene already contains a camera (`Main Camera`) and a light (`Directional Light`). We will now modify the camera's field of view and position to prepare it for the tutorial. 

* **Action**: Click on `Main Camera` and in the _**Inspector**_ tab, modify the camera's `Position`, `Rotation`, `Projection` and `Size` to match the screenshot below. (Note that `Size` only becomes available once you set `Projection` to `Orthographic`)

<p align="center">
<img src="Images/camera_prep.png"/>
</p>


For this tutorial, we prefer our light to not cast any shadows, therefore:

* **Action**: Click on `Directional Light` and in the _**Inspector**_ tab, set `Shadow Type` to `No Shadows`.

We will now add the necessary components to the camera in order to equip it for the perception workflow. To do this, we need to add a `PerceptionCamera` component to it, and then define which types of ground-truth we wish to generate using this camera.

* **Action**: Select `Main Camera` again and in the _**Inspector**_ tab, click on the _**Add Component**_ button.
* **Action**: Start typing `Perception Camera` in the search bar that appears, until the `Perception Camera` script is found, with a **#** icon to the left:

<p align="center">
<img src="Images/add_comp_perc.png" width="400"/>
</p>

* **Action**: Click on this script to add it as a component. Your camera is now a `Perception` camera.

Adding components is the standard way in which objects can have various kinds of logic and data attached to them in Unity. This includes objects placed within the Scene (called GameObjects), such as the camera above, or objects outside of a Scene, in your project folders (called Prefabs).

The `Perception Camera` component comes with its own UI to modify various aspects of synthetic frame generation and annotation, as well as add or remove ground-truth labelers and labelling configurations. If you hover your mouse pointer over each of the fields shown (e.g. `Capture Interval`), you will see a tooltip popup with an explanation on what the item controls. You may see a warning at the bottom of this UI regarding asynchronous shader compilation. If so, follow the instructions in the warning message to disable this functionality and remove the warning.

As seen in the UI for `Perception Camera`, the list of `Camera Lebelers` is currently empty. For each type of ground-truth you wish to generate along-side your captured frames (e.g. 2D bounding boxes around objects), you will need to add a corresponding `Camera Labeler` to this list. 

To speed-up your perception workflow, the Perception comes pre-packaged with four useful and common labelers for object-detection tasks; however, if you are comfortable with code, you can easily add your own custom labelers. The labelers that come with the Perception package cover **2D bounding boxes, object counts, object information (pixel counts and ids), and semantic segmentation images (each object rendered in a unique colour)**. In this tutorial, we will be working with the first three. 

* **Action**: Click on the _**+**_ button to the bottom right corner of the empty labeler list, and select `BoundingBox2DLabeler`.
* **Action**: Repeat the above step to add `ObjectCountLabeler` and `RenderedObjectInfoLabeler`. 

Once you add the labelers, the _**Inspector**_ view of the `Perception Camera` component will look like this:

<p align="center">
<img src="Images/pc_labelers_added.png" width="400"/>
</p>


One of the useful features that comes with the `Perception Camera` is the ability to display real-time visualizations of the labelers when your simulation is running. For instance, the `BoundingBox2DLabeler` can display bounding boxes around the foreground objects that it tracks in real-time. To enable this feature, make sure the `Show Labeler Visualizations` checkmark is enabled. 

### <a name="step-4">Step 4: Specify Ground-Truth and Setup Object Labels</a> 

It is now time to tell your each labeler added to the `Perception Camera` which objects it should label in the generated dataset. For instance, if your workflow is intended to generate frames and ground-truth for detecting chairs, your labelers would need to know that they should look for objects labeled "chair" within the scene. The chairs should in turn also be labeled "chair" in order to make them visible to their intended labelers. We will now learn how to set-up these configuartions.

You will notice each added labeler has a field named `Id Label Config`. By adding a label configuration here you can instruct the labeler to look for certain labeles within the scene and ignore the rest. To do that, we should first create a fitting label configuration.

* **Action**: In the _**Project**_ tab, right-click the `Assets` folder, then click _**Create -> Perception -> Id Label Config**_.

This will create a new asset file named `IdLabelConfig` inside the `Assets` folder. 

* **Action**: Rename the newly created `IdLabelConfig` asset to `TutorialIdLabelConfig`.

Then, click on this asset to bring up its _**Inspector**_ view. In there, you can specify the labels that this config will keep track of. A new label config like this one contains an empty list of labels. 

In this tutorial, we will generate synthetic data intended for detecting 10 everyday grocery items. In this step, you will add labels for each of these 10 items to the list of labels for `TutorialIdLabelConfig`. 

* **Action**: Select `TutorialIdLabelConfig` and in the _**Inspector**_ tab, click on the _**+**_ button to add 10 new label entries. Use the following exact names for these entries:
  1 `candy_minipralines_lindt`
  2 `cereal_cheerios_honeynut`
  3 `cleaning_snuggle_henkel`
  4 `craft_yarn_caron`
  5 `drink_greentea_itoen`
  6 `drink_whippingcream_lucerne`
  7 `lotion_essentially_nivea`
  8 `pasta_lasagne_barilla`
  9 `snack_biscotti_ghiott`
  10 `snack_granolabar_naturevalley`

Once done, the _**Inspector**_ window for `TutorialIdLabelConfig` will look like this:

<p align="center">
<img src="Images/idlabelconfig.png" width="400"/>
</p>

These are the names of the 10 grocery items that we will work with in this tutorial. Wonder were the actual objects are? They were imported into your project when you imported the tutorial files from the _**Package Manager**_, and are located in the folder `Assets/Samples/Perception/0.5.0-preview.1/Tutorial Files/ Foreground Objects/Phase 1` .

Notice that each of the labels you entered automatically has a numerical ID assigned. These ids are required in order to use the generated data in machine learning models, which typically require numerical ids for classification of objects.

Now that you have created your label configuration, we need to assign this configuration to labelers that you previously added to your `Perception Camera` component. 

* **Action**: Select the `Main Camera` object from the Scene _**Hierarchy**_, and in the _**Inspector**_ tab, assign the newly created `TutorialIdLabelConfig` to all three labelers. To do so, you can either drag and drop the former into the corresponding fields for each labeler, or click on the small circular button in front of the `Id Label Config` field, which brings up an asset selection window filtered to only show compatible assets. The `Perception Camera` component will now look like the image below:

<p align="center">
<img src="Images/pclabelconfigsadded.png" width="400"/>
</p>

The final piece of the label set-up workflow is to assign the same 10 labels to the objects that are supposed to be detected by an eventual object-detection model. As mentioned above, these are located at `Assets/Samples/Perception/0.5.0-preview.1/Tutorial Files/ Foreground Objects/Phase 1`. Inside this folder, there are four assets related to each of the 10 foreground objects. These four includes a .fbx model file, a material, a .jpg texture, and a Prefab asset. While the file extensions are not shown as part of the file names in the folder view, if you click on a file to select it, you can view its full name and path in the address bar at the bottom of the _**Project**_ tab.

In Unity, Prefabs are essentially reusable GameObjects that are stored to disk, along with all their child GameObjects, components, and property values. In our case here, the Prefab asset is what brings all the pieces of each foreground object together and will be the actual asset that you will use in your perception pipeline. Let's see what these prefabs include.

* **Action**: In the _**Project**_ tab, navigate to `Assets/Samples/Perception/0.5.0-preview.1/Tutorial Files/ Foreground Objects/Phase 1`
* **Action**: Double click the file named `drink_whippingcream_lucerne.prefab` to open the Prefab asset. 

When you open the Prefab asset, you will see the object shown in the Scene view and its components shown on the right side of the editor, in the _**Inspector**_ view:

<p align="center">
<img src="Images/exampleprefab.png"/>
</p>

The Prefab contains a number of components, including a `Transform`, a `Mesh Filter`, a `Mesh Renderer` and a `Labeling` component (highlighted in the image above). While the first three of these are common Unity components, the fourth one is specific to the Perception package, and is used for assigning labels to objects. You can see here that the cream carton is already labeled `drink_whippingcream_lucerner`. This is true for all the foreground objects supplied in the sample tutorial files, which means you do not need to perform any additonal steps to label your foreground objects.

Note that each object can have multiple labels assigned, and thus appear as different objects to labelers with different label configurations. For instance, you may want your semantic segmentation labeler to detect all cream cartons as as `dairy_product`, while your bounding box labeler still distinguishes between different types of dairy product. To achieve this, you can add a `dairy_product` label to all your dairy products, and then in your label configuration for semantic segmentation, only add the `dairy_product` label, and not any specific products or brand names. To add an additional a label to the cream carton, you can click on the _**+**_ button to the bottom right corner of the label list, in the `Labeling` component.

### <a name="step-5">Step 5: Add and Set-up Randomizers</a> 

As mentioned earlier, one of the core ingredients of the perception workflow is the randomization of various aspects of the simulation, in order to introduce sufficient variation into the generated data. 

To start randomizing your simulation you will first need to add a `Scenario` to your scene. Scenarios control the execution flow of your simulation by coordinating all `Randomizer` components added to them. The Perception package comes with a useful set of Randomizers that let you quickly place your foreground objects in the Scene, generate varied backgrounds, as well as randomize various parameters of the simulation over time, including things such as positon, scale, and rotation of objects, number of objects within the camera's view, and so on. Randomizers achieve this through coordinating a number of `Parameter`s, which essentially define the most granular randomization behaviors. For instance, for continuous variable types such as floats, vectors, and colors, `Parameter`s can define the range, sampling distribution, and a seed for randomization. This is while another class of `Paramter`s let you randomly select one of a number of categorical options.  

To summarize, a sample `Scenario` could look like this:

<p align="center">
<img src="Images/scenario_hierarchy.png" width = "300"/>
</p>


In this tutorial, you will learn how to use the provided Randomizers, as well as how to create new ones that are custom-fitted to your randomization needs.

* **Action**: Create a new GameObject in your Scene by right-clicking in the _**Hierarchy**_ tab and clicking `Create Empty`.
* **Action**: Rename your new GameObject to `Simulation Scenario`.
* **Action**: In the _**Inspector**_ view of this new object, add a new `Fixed Length Scenario` component. 

Each `Scenario` executes a number of `Iterations`, and each `Iteration` carries on for a number of frames. These are timing elements you can leverage in order to customize your `Scenario`s and the timing of your randomizations. You will learn how to use `Iteartions` and frames in Phase 2 of this tutorial. For now, we will use the `Fixed Length Scenario`, which is a special kind of `Scenario` that runs for a fixed number of frames during each `Iteration`, and is sufficient for most beginner use-cases. Note that at any given time, you can have only one `Scenario` active in your Scene. 

Once the `Fixed Length Scenario` component is added, the inspector view for your `Simulation Scenario` object will look like below:

<p align="center">
<img src="Images/fixedscenarioempty.png" width = "400"/>
</p>

There are a number settings and properties you can modify here, such as `Quit On Complete` or `Constants`. Just like the `Perception Camera` component, if you hover your mouse cursor over each of these properties, you will see a tooltip containing a relevant explanation. For now, we will leave all of these fields as they are. This is currently an empty `Scneario`. Let's add some Randomizers.

* **Action**: Click _**Add Randomizer**_, and from the list choose `BackgroundObjectPlacementRandomizer`.

This Randomizer uses Poisson-Disk sampling to select random positions from a given area, and spawn copies of randomly selected Prefabs (from the given list) at the chosen positions. We will use this component to generate a background that will act as a distraction for our eventual object-detection machine learning model.

* **Action**: Click _**Add Folder**_, and from the file explorer window that opnes, choose the folder `Assets/Samples/Perception/0.5.0-preview.1/Tutorial Files/Background Objects/Prefabs`.

The beckground Prefabs are primitve shapes devoid of color or texture. Later Randomizers will take care of those aspects. Set the rest of the properties (except for `Seed`) according to the image below. The `Seed` attribute is the seed used for the underlying random sampler, and does not need to match the image shown.

<p align="center">
<img src="Images/background_randomizer.png" width = "400"/>
</p>

* **Action**: Click on the **▷** (play) button located at top middle section of the editor to run your simulation.

<p align="center">
<img src="Images/play.png" width = "500"/>
</p>

When the simulation starts running, Unity Editor will switch to the _**Game**_ tab to show you the output of the active camera, which carries the `Perception Camera` component, as seen below:

<p align="center">
<img src="Images/first_run.png" width = "700"/>
</p>

In this view, you will also see the real-time visualizations we discussed before shown on top of the camera's view. In the top right corner of the window, you can see a visualization control panel, through which you can enable or disable visualizations for individual labelers. That said, we currently have no foreground objects in the Scene yet, so no bounding boxes will be displayed.

Note that disabling visualizations for a labeler does not affect your generated data. The annotations from all labelers that are active before running the simulation will continue to be recorded and will appear in the output data.

To generate data as fast as possible, the simulation utilizes asynchronous processing to churn through frames quickly, rearranging and randomizing the objects in each frame. To be able to check out individual frames and inspect the real-time visualizations, click on the pause button (next to play). You can also switch back to the Scene view to be able to inspect each object individually. For performance reasons, it is recommended to disable visualizations altogether (from the _**Inspector**_ view of `Perception Camera` once you are ready to generate a large dataset)

As seen in the image above, what we have now is just a beige-colored wall of shapes. This is because so far we are only spawning them. To make this background more useful, let's add a couple more `Randomizers`. 

* **Action**: Repeat the previous steps to add `TextureRandomizer`, `HueOffsetRandomizer`, and `RotationRandomizer`.

`TextureRandomizer` and will have the task of attaching random textures to our colorless background objects at each `Iteration` of the `Scenario`. Simlarly, `HueOffsetRandomizer` will alter the color of the objects, and `RotationRandomizer` will give the objects a new random rotation each `Iteration`. 

* **Action**: In the UI snippet for `TextureRandomizer`, click _**Add Folder**_ and choose `Assets/Samples/Perception/0.5.0-preview.1/Tutorial Files/Background Textures`. 

* **Action**: In the UI snippet for `RotationRandomizer`, change the all the maximum values for the three ranges to `360` and leave the minimums at `0`. 

Your list of Randomizers should now look like the screenshot below:

<p align="center">
<img src="Images/all_back_rands.png" width = "400"/>
</p>

Note that the `Seed` values do not need to match the image above.

There is one more important thing left to do, in order to make sure all the above Randomizers operate as expected. Since `BackgroundObjectPlacementRandomizer` spawns objects, it already knows which objects in the Scene it is dealing with; however, the rest of the Randomizers  we added are not yet aware of what objects they should target because they don't spawn their own objects.

To make sure each Randomizer knows which objects it should work with, we will use an object tagging and querying workflow that the bundled Randomizers already use. Each Randomizer can query the Scene for objects that carry certain types of `RandomizerTag` components. For instance, the `TextureRandomizer` queries the Scene for objects that have a `TextureRandomizerTag` component (you can change this in code!). Therefore, in order to make sure our background Prefabs are affected by the `TextureRandomizer` we need to make sure they have `TextureRandomizerTag` attached to them.

* **Action**: In the _**Project**_ tab, navigate to the `Assets/Samples/Perception/0.5.0-preview.1/Tutorial Files/Background Objects/Prefabs`.
* **Action**: Select all the files inside and from the _**Inspector**_ tab add a `TextureRandomizerTag` to them. This will add the component to all the selected files.
* **Action**: Repeat the above step to add `HueOffsetRandomizerTag` and `RotationRandomizerTag` to all selected Prefabs.

Once the above step is done, the _**Inspector**_ tab for a background Prefab should look like this:

<p align="center">
<img src="Images/back_prefab.png" width = "400"/>
</p>

If you run the simulation now you will see the generated backgrounds look much more colourful and distracting!

<p align="center">
<img src="Images/background_good.png" width = "700"/>
</p>

It is now time to spawn and randomize our foregournd objects. We are getting close to generating our first set of synthetic data!

* **Action**: Add `ForegroundObjectPlacementRandomizer` to your list of Randomizers. Click _**Add Folder**_ and select `Assets/Samples/Perception/0.5.0-preview.1/Tutorial Files/Foreground Objects/Phase 1/Prefabs`.
* **Action**: Set these values for the above Randomizer: `Depth = 3, Separation Distance = 1.5, Placement Area = (5,5)`.

This Randomizer uses the same algorithm as the one we used for backgrounds; however, it is defined in a separate C# class because you can only have **one of each type of Randomizer added to your `Scenario`**. Therefore, this is our way of differentating between how background and foreground objects are treated.

While the texture and color of the foreground objects will be constant during the simulation, we would like their rotation to be randomized similar to the background Prefabs. To achieve this:

* **Action**: From the _**Project**_ tab select all the foreground Prefabs located in `Assets/Samples/Perception/0.5.0-preview.1/Tutorial Files/Foreground Objects/Phase 1/Prefabs`, and add a `RotationRandomizerTag` component to them.

The last step here is to make sure the order of randomizations is correct. Randomizers execute according to their order within the list of Randomizers added to your `Scenario`. If you look at the list now, you will notice that `ForegroundObjectPlacementRandomizer` is coming after `RotationRandomizer`, therefore, foreground objects will NOT be included in the rotation randomizations, even though they are carrying the proper tag. To fix that:

* **Action**: Drag  `ForegroundObjectPlacementRandomizer` and drop it above `RotationRandomizer`.

### <a name="step-6">Step 6: Generate and Verify Synthetic Data</a> 

You are now ready to generate your first dataset. The `FixedLengthScenario` component that we used has 1000 `Iteration`s by default, and each `Iteration` runs for one frame. Thus, the simulation will produce 1000 frames of annotated captures.

* **Action** Click **▷** (play) again and this time let the simulation finish. It should take about 1 minute, depending on your hardware.

While the simulation is running, your _**Game**_ view will quickly generate frames similar to the gif below:

<p align="center">
<img src="Images/generation1.gif" width = "700"/>
</p>


Once the run is complete, you will see a message in the _**Console**_ tab of the editor, with information on where the generated data has been saved. An example is shown below (Mac OS):

<p align="center">
<img src="Images/dataset_written.png"/>
</p>

* **Action**: Navigate to the dataset path addressed in the _**Console**_. 

In this folder, you will find a few types of data, depending on your `Perception Camera` settings. These can include:
- Logs
- JSON data
- RGB images (raw camera output) (if the `Save Camera Output to Disk` checkmark is enabled on `Perception Camera`)
- Semantic segmentation images (if the `SemanticSegmentationLabeler` is added and active on `Perception Camera')

The output dataset includes a large variety of information about various aspects of the active sensors in the Scene (currently only one), as well as the ground-truth generated by all active labelers. [This page](https://github.com/Unity-Technologies/com.unity.perception/blob/master/com.unity.perception/Documentation%7E/Schema/Synthetic_Dataset_Schema.md) provides a comprehensive explanation on the schema of this dataset. We strongly recommend having a look at the page once you have completed this tutorial.

* **Action**: To get a quick feel of how the data is stored, open the folder whose name starts with `Dataset`, then open the file named `captures_000.json`. This file contains the output from `BoundingBox2DLabeler`. The `captures` array contains the position and rotation of the sensor (camera), the position and rotation of the ego (sensor group, currently only one), and the annotations made by `BoundingBox2DLabeler` for all visible objects defined in its label configuration. For each visibile object, the annotations include:
* `label_id`: The numerical id assigned to this object's label in the labeler's label configuration
* `label_name`: The object's label, e.g. `candy_minipralines_lindt`
* `instance_id`: Unique instance id of the object
* `x` and `y`: Pixel coordinates of the top-left corner of the object's bounding box. (measured from the top-left corner of the image)
* `width` and `height` of the object's bounding box.


To verify and analyze a variety of metrics for the generated data, such as number of foreground objects in each frame and degree of representation for each foreground object (label), we will now use Unity's Dataset Insights framework. This will involve running a Jupyter notebook which is conveniently packaged within a Docker file that you can download from Unity. 

* **Action**: Download and install [Docker Desktop](https://www.docker.com/products/docker-desktop)
* **Action**: Open a command line interface (Command Prompt, Terminal, etc.) and type the following command to run the Dataset Insights Docker image: 
`docker run -p 8888:8888 -v <path to synthetic data>:/data -t unitytechnologies/datasetinsights:latest`, where the path is what we earlier found in Unity's console messages.

This will download a Docker image from Unity. If you get an error regarding the path to your dataset, make sure you have not included the enclosing `<` and `>` in the path and that the spaces are properly escaped.

* **Action**: The image is now running on your computer. Open a web browser and navigate to `http://localhost:8888` to open the Jupyter notebook:

<p align="center">
<img src="Images/jupyter1.png"/>
</p>

* **Action**: To make sure your data is properly mounted, navigate to the `data` folder. If you see the dataset's folders here, we are good to go.
* **Action**: Navigate to the `datasetinsights/notebooks` folder and open `Perception_Statistics.ipynb`.
* **Action**: Once in the notebook, replace the `<GUID>` in the `data_root = /data/<GUID>` with the name of the dataset folder inside your generated data. For example `Dataseta26351bc-1b72-46c5-9e0c-d7afd6df2974`.

<p align="center">
<img src="Images/jupyter2.png"/>
</p>

This notebook contains a variety of functions for generating plots, tables, and bounding box images that help you analyze your generated dataset. Certain parts of this notebook are currently not of use to us, such as the code meant for downloading data generated through Unity Simulation (coming later in this tutorial), or parts that deal with output from the semantic segmentation labeler (which we did not include in our `PerceptionCamera`). 

Each of the code blocks in this notebook can be executed by clicking on them to select them, and the clicking the _**Run**_  button at the top. When you run a code block, an **asterisk (\*)** will be shonw next to the code block on the left side, until the code finishes executing.

* **Action**: Follow the instructions laid out in the notebook and run each code block to view its outputs. Not how parts of the code that are relevant to Unity Simulation are commented.

Below, you can see a sample plot generated by the Dataset Insights notebook, depicting the number of times each of the 10 foreground objects appeared in the dataset. As shown in the histogram, there is a high level of uniformity between the labels, which is a desirable outcome. 


<p align="center">
<img src="Images/object_count_plot.png" width="600"/>
</p>


This concludes Phase 1 of the Perception tutoial. In the next phase, you will dive a little bit into randomization code and learn how to build your own custom Randomizer. [Click here to continue to Phase 2: Custom Randomizations](Phase2.md)
