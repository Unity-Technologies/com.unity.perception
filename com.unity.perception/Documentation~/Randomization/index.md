# Overview

The randomization toolset simplifies randomizing aspects of generating synthetic data. It facilitates exposing parameters for randomization, offers samplers to pick random values from parameters, and provides Scenarios to coordinate a full randomization process. Each of these also allows for custom implementations to fit particular randomization needs.

#### What is Domain Randomization?

Domain randomization is used to create variability in synthetic datasets to help ML models trained in a synthetic domain (Unity) work well in real world applications. The intuition is that the real world is complex and varies widely, while synthetic datasets have limited variation. By randomizing parts of the synthetic domain the ML model will be exposed to enough variability to perform well when deployed. Domain randomization techniques vary widely in what they randomize and how they choose the randomization to apply. The randomization toolset is intended to facilitate a broad variety of implementations and applications.

Our use of domain randomization draws from Tobin et al.'s (2017) work on training robotic pick and place using purely synthetic data.

#### How can a Unity project be randomized using the Perception Randomization toolset?

Randomizing a project involves the following steps:
1. Create a Scenario
2. Define and add Randomizers to the Scenario
3. Customize Parameters and Samplers in the Randomizers
4. Generate randomized computer vision training data

Beginning with step 1, add a Scenario component to your simulation. This Scenario will act as the central hub for all randomization activities that occur when your scene is executed.

Next, add a few Randomizers to the Scenario. The Randomizers, in conjunction with the Scenario, will perform the actual randomization activities within the simulation.

After adding the necessary Randomizers, configure the random Parameters assigned to each Randomizer to further customize how the simulation is Randomized. The random Parameters and Samplers exposed in each Randomizer's inspector can be manipulated to specify different probability distributions to use when generating random values.

Once the project has been randomized and your scene has been configured with the data capture tools available in the Perception package, enter play mode in the editor or execute your Scenario through the Unity Simulation cloud service to generate domain randomized perception data.

### Randomizers Included with Perception

The Perception package comes with a plethora of randomizers that you can use to randomize your dataset projects – swap materials, textures, randomize lighting, material properties and more! For a description of what kinds of randomizers we have to offer, take a look at the [Randomizer Library](RandomizerLibrary.md)!


## Further Reading

Continue reading for more details concerning the primary components driving randomizations in the Perception package, including:
1. Scenarios
2. Randomizers
3. Randomizer Tags
4. Parameters
5. Samplers

<br>
<p align="center">
<img src="../images/Randomization/randomization_uml.png" width="900"/>
  <br><i>Class diagram for the randomization framework included in the Perception package</i>
</p>



## Scenarios

Within a randomized simulation, the Scenario component has three responsibilities:
1. Controlling the execution flow of your simulation
2. Defining a list of Randomizers
3. Defining constants that can be configured externally from a built Unity player 

The fundamental principle of domain randomization is to simulate environments under a variety of randomized conditions. Each Iteration of a Scenario is intended to encapsulate one complete run of a simulated environment under uniquely randomized conditions. Scenarios further define what conditions determine the end of an Iteration and how many Iterations to perform.

To actually randomize a simulation, Randomizers can be added to a Scenario to vary different simulation properties. At runtime, the Scenario will execute each Randomizer according to its place within the Randomizer list.

Scenarios can also define constants from which to expose global simulation behaviors automatically. By modifying serialized constants externally, users can customize their simulation runtime even after their project has been built.

To read more about Scenarios and how to customize them, navigate over to the **[Scenarios documentation](Scenarios.md)**.


## Randomizers

Randomizers encapsulate specific randomization activities to perform during the lifecycle of a randomized simulation. For example, Randomizers exist for spawning objects, repositioning lights, varying the color of objects, etc. Randomizers expose random Parameters to their inspector interface to further customize these variations.

To read more about how to create custom Parameter types, navigate over to the **[Randomizers documentation](Randomizers.md)**.


## Randomizer Tags

RandomizerTags are the primary mechanism by which Randomizers query for a certain subset of GameObjects to randomize within a simulation. For example, a rotation Randomizer could query for all GameObjects with a RotationRandomizerTag component to obtain an array of all objects the Randomizer should vary for the given simulation Iteration.

To read more about how to use RandomizerTags, navigate over to the **[RandomizerTags documentation](RandomizerTags.md)**.


## Parameters

Parameters are classes that utilize Samplers to deterministically generate random typed values. Parameters are often exposed within the inspector interface of Randomizers to allow users to customize said Randomizer's behavior. To accomplish this, Parameters combine and transform the float values produced by one or more Samplers into various C# types. For example, a Vector3 Parameter can be used to map three Samplers to the x, y, and z dimensions of a GameObject. Or a material Parameter can utilize a Sampler to randomly select one material from a list of possible options.

To read more about how to create custom Parameter types, navigate over to the **[Parameters documentation](Parameters.md)**.


## Samplers

Samplers generate bounded random float values by sampling from probability distributions. They are considered bounded since each random sampler generates float values within a range defined by a minimum and maximum value.

Take a look at the **[Samplers doc](Samplers.md)** to learn more about implementing custom probability distributions and samplers that can integrate with the Perception package.


## Getting Started

Visit the [Perception Tutorial](../Tutorial/TUTORIAL.md) to get started using the Perception package's randomization tools in an example project.
