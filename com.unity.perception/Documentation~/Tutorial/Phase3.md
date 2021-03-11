# Perception Tutorial
## Phase 3: Cloud

In this phase of the tutorial, we will learn how to run our Scene on _**Unity Simulation**_ and analyze the generated dataset using _**Dataset Insights**_. Unity Simulation will allow us to generate a much larger dataset than what is typically plausible on a workstation computer.

Steps included in this phase of the tutorial:

* [Step 1: Setup Unity Account, Unity Simulation, and Cloud Project](#step-1)
* [Step 2: Run Project on Unity Simulation](#step-2)
* [Step 3: Keep Track of Your Runs Using the Unity Simulation Command-Line Interface](#step-3)
* [Step 4: Analyze the Dataset using Dataset Insights](#step-4)

### <a name="step-1">Step 1: Setup Unity Account, Unity Simulation, and Cloud Project</a> 

In order to use Unity Simulation, you need to first create a Unity account or login with your existing one. Once logged in, you will also need to sign-up for Unity Simulation. 

* **:green_circle: Action** Click on the _**Cloud**_ button at the top-right corner of Unity Editor to open the _**Services**_ tab. 

<p align="center">
<img src="Images/cloud_icon.png" width="400"/>
</p>

* **:green_circle: Action** Click the ***General settings***.

This will open the ***Services*** tab of the ***Project Settings*** window. If you have not logged in yet, you will see a message noting that you are signed out:

<p align="center">
<img src="Images/signin.png" width="600"/>
</p>

* **:green_circle: Action**: Click _**Sign in...**_ and follow the steps in the window that opens to sign in or create a Unity account.
* **:green_circle: Action**: Sign up for a free trial of Unity Simulation [here](https://unity.com/products/unity-simulation).

Unity Simulation is a cloud-based service that makes it possible for you to run hundreds of instances of Unity builds in order to generate massive amounts of data. The Unity Simulation service is billed on a per-usage basis, and the free trial offers up to $100 of free credit per month. In order to access the free trial, you will need to provide credit card information. **This information will be used to charge your account if you exceed the $100 monthly credit.** A list of hourly and daily rates for various computational resources is available in the page where you first register for Unity Simulation.

Once you have registered for a free trial, you will be taken to your Unity Simulation dashboard, where you will be able to observe your usage and billing information.

It is now time to connect your local Unity project to a cloud project.

* **:green_circle: Action**: Return to Unity Editor. Click _**Select Organization**_ and choose the only available option (which typically has the same name as your Unity username).

If you have used Unity before, you might have set up multiple organizations for your account. In that case, choose whichever you would like to associate with this project.

<p align="center">
<img src="Images/create_proj.png" width="600"/>
</p>

* **:green_circle: Action**: Click _**Create Project ID**_ to create a new cloud project and connect your local project to it.

### <a name="step-2">Step 2: Run Project on Unity Simulation</a> 

The process of running a project on Unity Simulation involves building it for Linux and then uploading this build, along with a set of parameters, to Unity Simulation. The Perception package simplifies this process by including a dedicated _**Run in Unity Simulation**_ window that accepts a small number of required parameters and handles everything else automatically.

For performance reasons, it is best to disable real-time visualizations before carrying on with the Unity Simulation run. 

* **:green_circle: Action**: From the _**Inspector**_ view of `Perception Camera`, disable real-time visualizations.

In order to make sure our builds are compatible with Unity Simulation, we need to set our project's scripting backend to _**Mono**_ rather than _**IL2CPP**_ (if not already set). We will also need to switch to _**Windowed**_ mode.

* **:green_circle: Action**: From the top menu bar, open _**Edit -> Project Settings**_.
* **:green_circle: Action**: In the window that opens, navigate to the _**Player**_ tab, find the _**Scripting Backend**_ setting (under _**Other Settings**_), and change it to _**Mono**_:

<p align="center">
<img src="Images/mono.png" width="800"/>
</p>

* **:green_circle: Action**: Change _**Fullscreen Mode**_ to _**Windowed**_ and set a width and height of 800 by 600.

<p align="center">
<img src="Images/windowed.png" width="600"/>
</p>

* **:green_circle: Action**: Close _**Project Settings**_. 
* **:green_circle: Action**: From the top menu bar, open _**Window -> Run in Unity Simulation**_.

<p align="center">
<img src="Images/runinusim.png" width="600"/>
</p>

Here, you can also specify a name for the run, the number of Iterations the Scenario will execute for, and the number of _**Instances**_ (number of nodes the work will be distributed across) for the run. This window automatically picks the currently active Scene and Scenario to run in Unity Simulation.

* **:green_circle: Action**: Name your run `FirstRun`, set the number of Iterations to `1000`, and Instances to `20`. 
* **:green_circle: Action**: Click _**Build and Run**_.

> :information_source: You can ignore the ***Optional Configuration*** section for now. This is useful if you plan to specify a configuration for your Scenario (including the Randomizers) that will override the values set in the Scenario UI, in Unity Simulation. To generate a configuration, you can click on the ***Generate JSON Config*** button provided in the ***Inspector*** view of Scenario components.

Your project will now be built and then uploaded to Unity Simulation and run. This may take a few minutes to complete, during which the editor may become frozen; this is normal behaviour.

* **:green_circle: Action**: Once the operation is complete, you can find the **Execution ID** of this Unity Simulation run in the **Console** tab and the ***Run in Unity Simulation** Window: 

<p align="center">
<img src="Images/build_uploaded.png" width="600"/>
</p>

### <a name="step-3">Step 3: Keep Track of Your Runs Using the Unity Simulation Command-Line Interface</a> 

To keep track of the progress of your Unity Simulation run, you will need to use Unity Simulation's command-line interface (CLI). Detailed instructions for this CLI are provided [here](https://github.com/Unity-Technologies/Unity-Simulation-Docs/blob/master/doc/quickstart.md#download-unity-simulation-quickstart-materials). For the purposes of this tutorial, we will only go through the most essential commands, which will help us know when our Unity Simulation run is complete and where to find the produced dataset.

* **:green_circle: Action**: Download the latest version of `unity_simulation_bundle.zip` from [here](https://github.com/Unity-Technologies/Unity-Simulation-Docs/releases).

> :information_source: If you are using a MacOS computer, we recommend using the _**curl**_ command from the Terminal to download the file, in order to avoid issues caused by the MacOS Gatekeeper when using the CLI. You can use these commands:
```
curl -Lo ~/Downloads/unity_simulation_bundle.zip <URL-unity_simulation_bundle.zip>
unzip ~/Downloads/unity_simulation_bundle.zip -d ~/Downloads/unity_simulation_bundle
```
The `<URL-unity_simulation_bundle.zip>` address can be found at the same page linked above.

* **:green_circle: Action**: Extract the zip archive you downloaded.
* **:green_circle: Action**: Open a command-line interface (Terminal on MacOS, cmd on Windows, etc.) and navigate to the extracted folder.

If you downloaded the zip archive in the default location in your downloads folder, you can use these commands to navigate to it from the command-line:

MacOS:
`cd ~/Downloads/unity_simulation_bundle`

Windows:
`cd C:\Users\UserName\Downloads\unity_simulation_bundle`

You will now be using the _**usim**_ executable to interact with Unity Simulation through commands. 

* **:green_circle: Action** To see a list of available commands, simply run `usim` once:

MacOS:
`USimCLI/mac/usim`

Windows:
`USimCLI\windows\usim`

The first step is to login.

* **:green_circle: Action**: Login to Unity Simulation using the `usim login auth` command.

MacOS:
`USimCLI/mac/usim login auth`

Windows:
`USimCLI\windows\usim login auth`

This command will ask you to press Enter to open a browser for you to login to your Unity account:

`Press [ENTER] to open your browser to ...`

* **:green_circle: Action**: Press Enter to open a browser window for logging in.

Once you have logged you will see this page:

<p align="center">
<img src="Images/usim_login.png" width="400"/>
</p>

> :warning: On MacOS, you might get errors related to permissions. If that is the case, modify the permissions on the `~/.usim` folder and its contents to give your user full read and write permission.

> :information_source: From this point on we will only include MacOS formatted commands in the tutorial, but all the `usim` commands we use will work in all supported operating systems.**

* **:green_circle: Action**: Return to your command-line interface. Get a list of cloud projects associated with your Unity account using the `usim get projects` command:

MacOS:
`USimCLI/mac/usim get projects`
<!--Windows:
`USimCLI\windows\usim get projects`-->

Example output:

```
 name                  id                                       creation time             
--------------------- ---------------------------------------- --------------------------- 
 Perception Tutorial   acd31956-582b-4138-bec8-6670be150f09 *   2020-09-30T00:33:41+00:00 
 SynthDet              9ec23417-73cd-becd-9dd6-556183946153     2020-08-12T19:46:20+00:00  
 ```

In case you have more than one cloud project, you will need to "activate" the one corresponding with your Perception Tutorial project. If there is only one project, it is already activated, and you will not need to execute the command below (note: replace `<project-id>` with the id of your desired project).

* **:green_circle: Action**: Activate the relevant project:

MacOS:
`USimCLI/mac/usim activate project <project-id>`
<!--Windows:
`USimCLI\windows\usim get projects <project-id>` -->

When asked if you are sure you want to change the active project, enter "**y**" and press **Enter**.

Now that we have made sure the correct project is active, we can get a list of all the current and past runs for the project. 

* **:green_circle: Action**: Use the `usim get runs` command to obtain a list of current and past runs:

MacOS:
`USimCLI/mac/usim get runs`

<!--Windows:
`USimCLI\windows\usim get runs`-->

An example output with 3 runs would look like this:

```
Active Project ID: acd31956-582b-4138-bec8-6670be150f09
name        id        creation time         executions                                    
----------- --------- --------------------- -----------------------------------------------
 FirstRun    1tLbZxL   2020-10-01 23:17:50    id        status        created_at           
                                             --------- ------------- --------------------- 
                                              yegz4WN   In_Progress   2020-10-01 23:17:54  
 Run2        klvfxgT   2020-10-01 21:46:39    id        status        created_at           
                                             --------- ------------- --------------------- 
                                              kML3i50   In_Progress   2020-10-01 21:46:42  
 Test        4g9xmW7   2020-10-01 02:27:06    id        status      created_at             
                                             --------- ----------- ---------------------   
                                              xBv3arj   Completed   2020-10-01 02:27:11    
```

As seen above, each run has a name, an ID, a creation time, and a list of executions. Note that each "run" can have more than one "execution", as you can manually execute runs again using the CLI.

You can also obtain a list of all the builds you have uploaded to Unity Simulation using the `usim get builds` command.

You may notice that the IDs seen above for the run named `FirstRun` match those we saw earlier in Unity Editor's _**Console**_. You can see here that the single execution for our recently uploaded build is `In_Progress` and that the execution ID is `yegz4WN`.

Unity Simulation utilizes the ability to run simulation Instances in parallel. If you enter a number larger than 1 for the number of Instances in the _**Run in Unity Simulation**_ window, your run will be parallelized, and multiple simulation Instances will simultaneously execute. You can view the status of all simulation Instances using the `usim summarize run-execution <execution-id>` command. This command will tell you how many Instances have succeeded, failed, have not run yet, or are in progress. Make sure to replace `<execution-id>` with the execution ID seen in your run list. In the above example, this ID would be `yegz4WN`.

* **:green_circle: Action**: Use the `usim summarize run-execution <execution-id>` command to observe the status of your execution nodes:

MacOS:
`USimCLI/mac/usim summarize run-execution <execution-id>`
<!--Windows:
`USimCLI\windows\usim summarize run-execution <execution-id>`-->

Here is an example output of this command, indicating that there is only one node, and that the node is still in progress:

```
 state         count 
------------- -------
 Successes     0     
 In Progress   1     
 Failures      0     
 Not Run       0    
 ```

 At this point, we will need to wait until the execution is complete. Check your run with the above command periodically until you see a 1 for `Successes` and 0 for `In Progress`.
 Given the relatively small size of our Scenario (1,000 Iterations), this should take less than 5 minutes.

 * **:green_circle: Action**: Use the `usim summarize run-execution <execution-id>` command periodically to check the progress of your run.
 * **:green_circle: Action**: When execution is complete, use the `usim download manifest <execution-id>` command to download the execution's manifest:

 MacOS:
 `USimCLI/mac/usim download manifest <execution-id>`

 The manifest is a `.csv` formatted file and will be downloaded to the same location from which you execute the above command, which is the `unity_simulation_bundle` folder.
 This file does **not**** include actual data, rather, it includes links to the generated data, including the JSON files, the logs, the images, and so on.
 
 * **:green_circle: Action**: Open the manifest file to check it. Make sure there are links to various types of output and check a few of the links to see if they work.


### <a name="step-4">Step 4: Analyze the Dataset using Dataset Insights</a> 

 In order to download the actual data from your run, we will now use Dataset Insights again. This time though, we will utilize some of the lines that were commented in our previous use with locally generated data.

 * **:green_circle: Action**: Open the Dataset Insights Jupyter notebook again, using the command below:
 
 `docker run -p 8888:8888 -v <download path>/data:/data -t unitytechnologies/datasetinsights:latest`

> :information_source: If you get an error about the format of the command, try the command again **with quotation marks** around the folder mapping argument, i.e. `"<download path>/data:/data"`.

In the above command, replace `<download path>` with the location on your computer in which you wish to download your data.

Once the Docker image is running, the rest of the workflow is quite similar to what we did in Phase 1, with certain differences caused by the need to download the data from Unity Simulation.

* **:green_circle: Action**: Open a web browser and navigate to `http://localhost:8888` to open the Jupyter notebook.
* **:green_circle: Action**: Navigate to the `datasetinsights/notebooks` folder and open `Perception_Statistics.ipynb`.
* **:green_circle: Action**: In the `data_root = /data/<GUID>` line, the `<GUID>` part will be the location inside your `<download path>` where the data will be downloaded. Therefore, you can just remove it so as to have data downloaded directly to the path you previously specified:

<p align="center">
<img src="Images/di_usim_1.png" width="900"/>
</p>

The next few lines of code pertain to setting up your notebook for downloading data from Unity Simulation. 

* **:green_circle: Action**: In the block of code titled "Unity Simulation [Optional]", uncomment the lines that assign values to variables, and insert the correct values, based on information from your Unity Simulation run. 

We have previously learned how to obtain the `run_execution_id` and `project_id`. You can remove the value already present for `annotation_definition_id` and leave it blank. What's left is the `access_token`.

* **:green_circle: Action**: Return to your command-line interface and run the `usim inspect auth` command.

MacOS:
 `USimCLI/mac/usim inspect auth`

If you receive errors regarding authentication, your token might have timed out. Repeat the login step (`usim login auth`) to login again and fix this issue.

A sample output from `usim inspect auth` will look like below:

```
Protect your credentials. They may be used to impersonate your requests.
access token: Bearer 0CfQbhJ6gjYIHjC6BaP5gkYn1x5xtAp7ZA9I003fTNT1sFp
expires in: 2:00:05.236227
expired: False
refresh token: FW4c3YRD4IXi6qQHv3Y9W-rwg59K7k0Te9myKe7Zo6M003f.k4Dqo0tuoBdf-ncm003fX2RAHQ
updated: 2020-10-02 14:50:11.412979
```

The `access_token` you need for your Dataset Insights notebook is the access token shown by the above command, minus the `'Bearer '` part. So, in this case, we should input `0CfQbhJ6gjYIHjC6BaP5gkYn1x5xtAp7ZA9I003fTNT1sFp` in the notebook. 

* **:green_circle: Action**: Copy the access token excluding the `'Bearer '` part to the corresponding field in the Dataset Insights notebook.

Once you have entered all the information, the block of code should look like the screenshot below (the actual values you input will be different):

<p align="center">
<img src="Images/di_usim_2.png" width="800"/>
</p>


* **:green_circle: Action**: Continue to the next code block and run it to download all the metadata files from the generated dataset. This includes JSON files and logs but does not include images (which will be downloaded later). 

You will see a progress bar while the data downloads:

<p align="center">
<img src="Images/di_usim_3.png" width="800"/>
</p>


The next couple of code blocks (under "Load dataset metadata") analyze the downloaded metadata and display a table containing annotation-definition-ids for the various metrics defined in the dataset.

* **:green_circle: Action**: Once you reach the code block titled "Built-in Statistics", make sure the value assigned to the field `rendered_object_info_definition_id` matches the id displayed for this metric in the table output by the code block immediately before it. The screenshot below demonstrates this (note that your ids might differ from the ones here):

<p align="center">
<img src="Images/di_usim_4.png" width="800"/>
</p>

Follow the rest of the steps inside the notebook to generate a variety of plots and stats. Keep in mind that this notebook is provided just as an example, and you can modify and extend it according to your own needs using the tools provided by the [Dataset Insights framework](https://datasetinsights.readthedocs.io/en/latest/).

The next step in this workflow, which is out of the scope of this tutorial, is to train an object-detection model using our synthetic dataset. It is important to note that the 1000 large dataset we generated here is probably not sufficiently large for training most models. We chose this number here so that the Unity Simulation run would finish quickly, allowing us to move on to learning how to analyze the statistics of the dataset. In order to generate data for training, we recommend a minimum dataset size of around 50,000 captures with a large degree of randomization. 

This concludes the Perception Tutorial. In case of any issues or questions, please feel free to open a GitHub issue on the `com.unity.perception` repository so that the Unity Computer Vision team can get back to you as soon as possible.
