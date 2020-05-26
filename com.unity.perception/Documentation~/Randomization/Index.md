# Overview

The perception package's randomization toolset enables users to incorporate domain randomization (DR) principles into Unity projects intended for synthetic training data generation.

What is Domain Randomization?

Domain Randomization (DR) is a technique involving the creation of a variety of simulated environments with randomized properties to train a model over a wider domain of environment conditions. The hypothesis behind DR is that models trained on randomized data sets are more likely to adapt to real-world enviroments than their non-randomized counterparts. It is expected that the larger domain of environment conditions generated through DR will encompase more characteristics of actual enviroments than non-randomization data sets.

To this end, the perception package offers the following constructs to help facilitate the randomization of simulations:
1. Parameters
2. Samplers
3. Scenarios


## Parameters

Parameters are used to map common types of simulation properties to random variables. For example, a Vector3 size parameter can be used to randomize the x, y, and z dimensions of an obstacle. Or a material parameter can be used to swap between different terrain surface materials.

![Example Parameters](./Images/ParameterConfiguration.png)

Parameters are configured and organized within a scene using a parameter configuration. Users can create new parameters, modify parameter randomization properties, and even assign target GameObjects to manipulate simulation properties directly from the inspector. Additionally, parameter sub-properties can be modified in playmode better visualize the impact of different randomization settings.

To read more about how to create custom parameter types, navigate over to the [parameters doc](Parameters.md).


## Samplers

Samplers are classes that deterministically generate random float values from bounded probability distributions. Samplers are considered bounded since each random sampler generates float values within a range defined by a minumum and maximum value. The values generated from samplers are often used to randomize the sub components of parameters.

![Example Parameters](./Images/ColorParameter.png)

For example, a color parameter has four independently randomizable components: hue, saturation, value, and alpha. Each of the four samplers attached to a color parameter can employ a unique probability distribution to customize how new colors are sampled within a simulation. Out of the box, the perception package supports uniform and normal distribution sampling. So in our color example, a user may choose a normal distribution for their hue, a uniform distribution for saturation, and a constant value sampler for the value and alpha color components.

Take a look at the [samplers doc](Samplers.md) to learn more about implementing custom probability distributions and samplers that can integrate with the perception package.


## Scenarios

 Scenarios have three responsibilities:
 1. Controlling the execution flow of your simulation 
 2. Customizing the application of random parameters in your project
 3. Defining constants that can be configured externally from a built Unity player 

The fundamental principle of DR is to simulate environments under a variety of randomized conditions. To this end, scenarios have a concept of iterations. Each iteration of a scenario is intended to encapsulate one complete run of a simulated environment under uniquely randomized conditions. Scenarios determine how to setup a new iteration, what conditions determine the end of an iteration, how to clean up a completed iteration, and finally how many iterations to perform. Each of these behaviors can be customed for a new scenario by deriving the perception package's scenario class.

It was mentioned before in the parameter section of this doc that you can configure parameters to affect simulation properties directly from the parameter configuration. While useful, this feature is constrained to a particular set of use cases. Instead, a user can reference existing parameters in their scenario to implement more intricate randomizations. For example, a user can reference a `SpawnCount` parameter and a `ObjectPosition` parameter to randomize the positions of a dynamic number of objects during the setup step of a scenario.

![Example Parameters](./Images/TestScenario.png)

Finally, scenarios define constants from which to expose global simulation behaviors automatically. By modifying serialized constants externally, users can customize their simulation runtime even after their project has been built.

Take a look at the [scenarios doc](Scenarios.md) to learn more about creating custom scenarios.


## Getting Started

Visit our [randomization tutorial doc](Tutorial.md) to get started using the perception package's randomization tools in an example project.
