# Randomizers

Randomizers encapsulate specific randomization activities to perform during the execution of a randomized simulation. For example, Randomizers exist for spawning objects, repositioning lights, varying the color of objects, etc. Randomizers expose random parameters to their inspector interface to further customize these variations. Users can add a set of Randomizers to a Scenario in order to define an ordered list of randomization activities to perform during the lifecycle of a simulation. 

To define an entirely new Randomizer, derive the Randomizer class and implement one or more of the methods listed in the section below to randomize GameObjects during the runtime of a simulation.


## Randomizer Hooks

1. OnCreate() - called when the Randomizer is added or loaded to a Scenario
2. OnIterationStart() - called at the start of a new Scenario Iteration
3. OnIterationEnd() - called the after a Scenario Iteration has completed
4. OnScenarioComplete() - called the after the entire Scenario has completed
5. OnStartRunning() - called on the first frame a Randomizer is enabled
6. OnStopRunning() - called on the first frame a disabled Randomizer is updated
7. OnUpdate() - executed every frame for enabled Randomizers


## Randomizer Coding Example

Below is the code for the sample rotation Randomizer included with the Perception package:

```
[Serializable]
[AddRandomizerMenu("Perception/Rotation Randomizer")]
public class RotationRandomizer : Randomizer
{
    public Vector3Parameter rotation = new Vector3Parameter();

    protected override void OnIterationStart()
    {
        var taggedObjects = tagManager.Query<RotationRandomizerTag>();
        foreach (var taggedObject in taggedObjects)
            taggedObject.transform.rotation = Quaternion.Euler(rotation.Sample());
    }
}
```

There are a few key things to note from this example:
1. Make sure to add the [Serializable] tag to all Randomizer implementations to ensure that the Randomizer can be customized and saved within the Unity Editor.
2. The [AddRandomizerMenu] attribute customizes the "Add Randomizer" sub menu path in the Scenario inspector for a particular Randomizer. In this example, the RotationRandomizer can be added to a Scenario by opening the _**Add Randomizer**_ menu and clicking `Perception -> Rotation Randomizer`.
3. The line `var taggedObjects = tagManager.Query<RotationRandomizerTag>();` uses RandomizerTags in combination with the current Scenario's tagManager to query for all objects with RotationRandomizerTags and obtain the subset of GameObjects within the simulation that need to have their rotations randomzied. To learn more about how RandomizerTags work, visit the [RandomizerTags documentation page](RandomizerTags.md).
