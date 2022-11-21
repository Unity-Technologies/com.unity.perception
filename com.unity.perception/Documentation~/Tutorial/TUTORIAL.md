<img src="../images/unity-wide-whiteback.png" align="middle" width="3000"/>

</br>

<h1 align="center">🌟 Perception Synthetic Data Tutorial 🌟</h1> 

The Perception package offers a variety of tools for generating synthetic datasets intended for use in perception-based machine learning tasks such as object detection, semantic segmentation, pose estimation, and so on. These datasets are in the form of **frames** captured using simulated sensors. These frames are **annotated** with **ground-truth** and are thus ready to be used for training and validating machine learning models. While the type of ground-truth bundled with this data will depend on your intended machine learning task, the Perception package comes with over nine common ground-truth labelers which will make it easier for you to generate and utilize synthetic data. This tutorial will guide you all the way from setting up Unity on your computer to generating a large-scale synthetic dataset for training an object-detection model.

**You do not need any prior experience with Unity or C#** to follow the this tutorial. We will generate a complete dataset using only samples, components, and assets included with the Perception package. The tutorial is divided into two high-level phases based on the complexity of the tasks involved. You will be gradually introduced to more advanced tools and workflows that the Perception package enables you to perform as you progress in the tutorial. Here is a high-level overview of the phases:

## 🔸 [Phase 1: Setup and Basic Randomizations](Phase1.md)

This phase will cover essential tasks and skills such as:
 * Downloading the Unity Editor and the Perception package
 * Fundamental interactions within the Unity Editor – importing sample assets into your Unity project, working with prefabs and scenes, adding components to objects and prefabs, etc.
 * Learning about basic components of the Perception package and creating a basic simulation with these components.
 * Running simulations on your computer and observing real-time visualizations of various ground-truth data.
 * Locating synthetic data generated from your simulation in your file explorer and understanding its various pieces.

## 🔸 [Phase 2: Custom Randomizations](Phase2.md)

To train robust and performant computer vision models, the training data needs to contain a large degree of variation. This technique is broadly called [Domain Randomization](https://lilianweng.github.io/posts/2019-05-05-domain-randomization/#what-is-domain-randomization) and is achieved by randomizing various aspects of your simulation. While you will use basic randomizations in Phase 1, Phase 2 of the tutorial will help you learn how to randomize your simulations in more complex ways by guiding you through writing a custom Randomizer in C# code that hooks up to our randomization framework. Once you complete this phase, you will know how to:
 * Create custom Randomizers by extending our provided samples
 * Orchestrate the operation of several Randomizers by specifying their order of execution and the objects they affect.
 * Define specific criteria (e.g. ranges, means, etc.) and logic (e.g. unique behaviors) for the randomizable attributes of objects.
 
### 👉 [Start Phase 1: Setup and Basic Randomizations!](Phase1.md)
