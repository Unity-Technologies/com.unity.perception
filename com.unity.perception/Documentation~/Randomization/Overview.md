# Overview

The perception package provides a set of tools to simplify incorporating domain randomization (DR) principles into Unity projects intended for synthetic training data generation. The central notion of DR is to create a variety of simulated environments with randomized properties and train a model that works across all of them. Models trained on randomized data sets are more likely to adapt to real-world enviroments as a real enviroment is expected to be one permutation within the distribution of generated variations.


The perception package offers the following constructs to help facilitate the randomization of Unity simulations:
1. Parameters
2. Samplers
3. Scenarios


## Parameters

Parameters in the perception package are used to map common types of simulation properties to random variables. For example, a Vector3 size parameter can be used to randomize the x, y, and z dimensions of an obstacle. Or a material parameter can be used to swap between different terrain surface materials.

Parameters are configured and organized within a scene from a parameter configuration inspector UI from the Unity Editor. Users can create new parameters, search for an existing parameter, and even assign a parameter to control a property on a target GameObject. Additionally, parameter sub-properties can be modified in playmode within the Unity Editor to explore how different configurations impact the randomizations applied to the simulation at runtime.

Parameters often consist of multiple sub components that should be randomized independently. To this end, samplers are also configured from within their parent parameters in the parameter configuration UI to modify how parameters behave at runtime.

To read more about how to create your own custom parameter types, [navigate over to our parameters doc]().


## Samplers

Samplers are classes that generate random float values from bounded probability distributions. Samplers are considered bounded since each random sampler generates float values within a range defined by a minumum and maximum value. The values generated from samplers are used to randomize the sub components of parameters.

For example, a color parameter has four independently randomizable components: hue, saturation, value, and alpha. Each of the four samplers attached to a color parameter can employ a unique probability distribution to customize how new colors are sampled within a simulation. Out of the box, the perception package supports uniform and normal distribution sampling. So in our color example, a user may choose a normal distribution for their hue, a uniform distribution for saturation, and a constant value sampler for the value and alpha color components.

Follow [this link]() to learn more about implementing your own custom probability distributions and samplers that can integrate with the perception package.


## Scenarios

 Scenarios have three responsibilities:
 1. Controlling the execution flow of your simulation 
 2. Customizing the application of random parameters in your project
 3. Defining constants that can be configured externally from a built Unity player 

The fundamental principle of DR is to simulate environments under a variety of randomized conditions. To this end, scenarios have a concept of iterations. Each iteration of a scenario is intended to encapsulate one complete run of the simulated environment under uniquely sampled randomized conditions. Scenarios determine how to setup a new run, what conditions determine the end of a run, how to clean up a run, and finally how many runs to perform. All of these behaviors can be customed for a new scenario by deriving the perception package's scenario class.

It was mentioned before in the parameter section of this doc that from that you can configure parameters to affect one simulation property directly from the parameter configuration. While useful, this feature is constrained to a limited set of use cases. For more complex parameter applications, a user can reference existing parameters in their scenario to implement more intricate randomizations at specific points of the simulation. For example, from the scenario, a user can reference a `SpawnCount` integer parameter to control how many objects are created during the setup step of a scenario.

Finally, scenarios define constants from which to expose global simulation behaviors like a starting iteration value or a total iteration count. Users can configure their simulation to serialize these scenario constants to JSON, modify them in an external program, and finally reimport the JSON constants at runtime to customize their simulation runtime even after the project has been built.

To learn more about creating custom scenarios, [visit our scenarios doc here]().


## Getting Started

To get started with using parameters, samplers, and scenarios to randomize your own simulations, visit our [randomization getting started guide]().
