# Overview

*NOTE: The Perception package's randomization toolset is currently marked as experimental and is subject to change.*

The randomization toolset simplifies randomizing aspects of generating synthetic data. It facilitates exposing parameters for randomization, offers samplers to pick random values from parameters, and provides scenarios to coordinate a full randomization process. Each of these also allows for custom implementations to fit particular randomization needs.

#### What is Domain Randomization?

Domain randomization is used to create variability in synthetic datasets to help ML models trained in a synthetic domain (Unity) work well in real world applications. The intuition is that the real world is complex and varies widely, while synthetic datasets have limited variation. By randomizing parts of the synthetic domain the ML model will be exposed to enough variability to perform well when deployed. Domain randomization techniques vary widely in what they randomize and how they choose the randomization to apply. The randomization toolset is intended to facilitate a broad variety of implementations and applications.

Our use of domain randomization draws from Tobin et al. (2017) work training robotic pick and place using purely synthetic data.

#### How can a Unity project be randomized using the Perception Randomization toolset?

Randomizing a project involves the following steps:
1. Create a scenario
2. Define and add randomizers to the scenario
3. Customize parameters and samplers in the randomizers
4. Generate randomized perception data

Beginning with step 1, add a scenario component to your simulation. This scenario will act as the central hub for all randomization activities that occur when your scene is executed.

Next, add a few randomizers to the scenario. The randomizers, in conjunction with the scenario, will perform the actual randomization activities within the simulation.

After adding the necessary randomizers, configure the random parameters assigned to each randomizer to further customize how the simulation is randomized. The random parameters and samplers exposed in each randomizer's inspector can be manipulated to specify different probabilty distributions to use when generating random values.

Once the project has been randomized and your scene has been configured with the data capture tools available in the perception package, enter play mode in the editor or execute your scenario through the Unity Simulation Cloud service to generate domain randomized perception data.

Continue reading for more details concerning the primary components driving randomizations in the perception package, including:
1. Scenarios
2. Randomizers
3. Randomizer Tags
4. Parameters
5. Samplers


## Scenarios

Within a randomized simulation, the scenario component has three responsibilities:
1. Controlling the execution flow of your simulation
2. Defining a list of randomizers
3. Defining constants that can be configured externally from a built Unity player 

The fundamental principle of domain randomization is to simulate environments under a variety of randomized conditions. Each **iteration** of a scenario is intended to encapsulate one complete run of a simulated environment under uniquely randomized conditions. Scenarios futher define what conditions determine the end of an iteration and how many iterations to perform.

To actually randomize a simulation, randomizers can be added to a scenario to vary different simulation properties. At runtime, the scenario will execute each randomizer according to its place within the randomizers list.

Scenarios can also define constants from which to expose global simulation behaviors automatically. By modifying serialized constants externally, users can customize their simulation runtime even after their project has been built.

To read more about scenarios and how to customize them, navigate over to the [scenarios doc](Scenarios.md).


## Randomizers

Randomizers encapsulate specific randomization activities to perform during the lifecycle of a randomized simulation. For example, randomizers exist for spawning objects, repositioning lights, varying the color of objects, etc. Randomizers expose random parameters to their inspector interface to further customize these variations.

To read more about how to create custom parameter types, navigate over to the [randomizers doc](Randomizers.md).


## Randomizer Tags

RandomizerTags are the primary mechanism by which randomizers query for a certain subset of GameObjects to randoize within a simulation. For example, a rotation randomizer could query for all GameObjects with a RotationRandomizerTag component to obtain an array of all objects the randomizer should vary for the given simulation iteration.

To read more about how to use RandomizerTags, navigate over to the [RandomizerTags doc](RandomizerTags.md).


## Parameters

Parameters are classes that utilize samplers to deterministically generate random typed values. Parameters are often exposed within the inspector interface of randomizers to allow users to customize said randomizer's behavior. To accomplish this, parameters combine and transform the float values produced by one or more samplers into various C# types. For example, a Vector3 parameter can be used to map three samplers to the x, y, and z dimensions of a GameObject. Or a material parameter can utilize a sampler to randomly select one material from a list of possible options.

To read more about how to create custom parameter types, navigate over to the [parameters doc](Parameters.md).


## Samplers

Samplers generate bounded random float values by sampling from probability distributions. They are considered bounded since each random sampler generates float values within a range defined by a minumum and maximum value.

Take a look at the [samplers doc](Samplers.md) to learn more about implementing custom probability distributions and samplers that can integrate with the perception package.


## Getting Started

Visit the [Perception Tutorial](../Tutorial/TUTORIAL.md) to get started using the perception package's randomization tools in an example project.
