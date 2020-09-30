# Perception Tutorial
## Phase 3: Cloud

In this phase of the tutorial, we will learn how to run our Scene on _**Unity Simulation (USim)**_. This will allow us to generate a much larger dataset than what is typically plausible on a workstation computer.

In order to use Unity Simulation you need to first create a Unity account or login with your existing one. Once logged in, you will also need to sign-up for Unity Simulation. 

* **Action** Click on the _**Cloud**_ button at the top-right corner of Unity Editor to open the _**Services**_ tab. 

<p align="center">
<img src="Images/cloud_icon.png" width="400"/>
</p>

If you have not logged in yet, the _**Services**_ tab will display a message noting that you are offline:

<p align="center">
<img src="Images/signin.png" width="400"/>
</p>

* **Action**: Click _**Sign in...**_ and follow the steps within the window that opens to sign in or create an account.
* **Action**: Sign up for a free trial of Unity Simulation [here](https://unity.com/products/unity-simulation).

Unity Simulation is a cloud-based service that makes it possible for you run thousands of instances of Unity builds in order to generate massive amounts of data. 

The USim service is billed on a per-usage basis, and the free trial offers up to $100 of free credit per month. In order to access the free trial, you will need to provide credit card information. **This information will be used to charge your account if you exceed the $100 monthly credit.** A list of hourly and daily rates for various computational resources is available in the page where you first register for USim.

Once you have registered for a free trial, you will be taken to your USim dashboard, where you will be able to observe your usage and billing invoices.

It is now time connect your local Unity project to a cloud project and your simulation on USim.

* **Action**: Return to Unity Editor. In the _**Services**_ tab click _**Select Organization**_ and choose the only available option (which typically has the same name as your Unity username).

If you have used Unity before, you might have set-up multiple organizations for your account. In that case, choose whichever you would like to associate with this project.

<p align="center">
<img src="Images/create_proj.png" width="400"/>
</p>

* **Action**: Click _**Create**_ to create a new cloud project and connect your local project to it.

The process of running a project on Unity Simulation involves building it for Linux and then uploading this build, along with a set of parameters, to Unity Simulation. The Perception package simplifies this process by including a dedicated _**Run in USim**_ window that accepts a small number of required parameters and handles everything else automatically.

In order to make sure our builds are compatible with USim, we need to set our project's scripting backend to _**Mono**_ rather than _**IL2CPP**_. The latter is the default option for projects created with newer versions of Unity.

* **Action**: From the top menu bar, select _**Edit -> Project Settings**_.
* **Action**: In the window that opens, navigate to the _**Player**_ tab, find the _**Scripting Backend**_ settings and change it to _**Mono**_:

<p align="center">
<img src="Images/mono.png"/>
</p>

sudo spctl --master-disable


need to put in credit card