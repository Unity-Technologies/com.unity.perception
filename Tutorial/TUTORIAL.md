<img src="../com.unity.perception/Documentation~/images/unity-wide.png" align="middle" width="3000"/>

# Perception Tutorial

The Perception package offers a variety of tools for generating synthetic datasets intended for use in perception-based machine learning tasks, such as object detection, semantic segmentation, and so on. These datasets are in the form of **frames** captured using simulated sensors. These frames are **annotated** with **ground-truth**, which means they are ready to be used for training and validating machine learning models. While the type of ground-truth bundled with this data will depend on your intentended machine learning task, the Perception package already comes with a number of common ground-truth labelers which will make it easier for you to generate synthetic data. This tutorial will guide you all the way from setting up Unity on your computer to generating a large-scale synthetic dataset for training an object-detection model. 

While this process may sound complicated, **you do not need to have any prior experience with Unity or C#** in order to follow the earlier stages of this tutorial and generate large-scale datasets using our provided samples and components. The tutorial will be divided into three high-level phases based on the complexity of the tasks involved. During these phases, you will be gradually introduced to more advanced tools and workflows that the Perception package enables you to perform. 

## Phase 1: Setup and Basic Simulations

This phase will cover essential tasks and skills such as:
 * Downloading Unity Editor and the Perception package
 * Fundamental interactions with Unity Editor (importing sample assets into your Unity project, working with prefabs and scenes, adding components to objects and prefabs, etc.)
 * Learning about essential components of the Perception package and creating a basic simulation with these essential elements.
 * Running your simulations on your computer and observing real-time visualizations of the Perception tools working.
 * Finding the synthetic data generated from your simulation and understanding its various pieces
 * Generating common statistics and visualizations for your synthetic data (e.g. number of objects in each frame, presence of each object in the whole data, etc.)
 
 ## Phase 2: Custom and Complex Randomizations
 
In order to get the best out of most perception-oriented machine learning models, the training data needs to contain a large-degree of variation. As a general rule of thumb, the more varied data you can feed to a model while training, the better it performs. This is achieved through randomizing various aspects of your simulation between frames. While you will use basic randomizations in Phase 1, **Phase 2** of the tutorial will help you learn how to randomize your simulations in more complex ways by guiding you through writing your first customized randomizer in C#. This phase is called **Custom and Complex Randomizations**, and once you complete it, you will know how to:
 * Create custom randomizers by extending our provided samples
 * Coordinate the operation of several randomizers by specifying their order of execution and the objects they affect
 * Have objects specify criteria (e.g. ranges, means, etc.) for their randomizable attributes.
 
## Phase 3: Cloud
 
You will generally require a large amount of data to train your intended model. Generating data in these practical sizes will take incredible amounts of time to finish if performed on typical workstation computers. This is where the cloud comes in. In this phase, you will learn how to:
 * Generate large-scale synthetic datasets containing hundreds of thousands of frames by leveraging the power of **Unity Simulation**. 
 * Download the cloud-generated data
 * Generate both common and custom statistics and visualizations for your cloud-generated data.
