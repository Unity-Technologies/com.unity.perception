# Randomizers

Randomizers encapsulate specific randomization activities to perform during the execution of a randomized simulation. For example, randomizers exist for spawning objects, repositioning lights, varying the color of objects, etc. Randomizers expose random parameters to their inspector interface to further customize these variations. Users can add a set of randomizers to a scenario in order to define an ordered list randomization activities to perform during the lifecycle of a simulation. 

To define an entirely new randomizer, derive the Randomizer class and implement one or more of the methods listed in the section below to randomize GameObjects during the runtime of a simulation.


## Randomizer Hooks

1. OnCreate() - called when the Randomizer is added or loaded to a scenario
2. OnIterationStart() - called at the start of a new scenario iteration
3. OnIterationEnd() - called the after a scenario iteration has completed
4. OnScenarioComplete() - called the after the entire scenario has completed
5. OnStartRunning() - called on the first frame a Randomizer is enabled
6. OnStopRunning() - called on the first frame a disabled Randomizer is updated
7. OnUpdate() - executed every frame for enabled Randomizers


## Randomizer Coding Example

Below is the code for the sample rotation randomizer included with the perception package:

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
1. Make sure to add the [Serializable] tag to all randomizer implementations to ensure that the randomizer can be customized and saved within the Unity Editor.
2. The [AddRandomizerMenu] attribute customizes the "Add Randomizer" sub menu path in the scenario inspector for a particular randomizer. In this example, the RotationRandomizer can be added to a scenario by opening the add randomizer menu and clicking `Perception -> Rotation Randomizer`.
3. The line `var taggedObjects = tagManager.Query<RotationRandomizerTag>();` uses RandomizerTags in combination with the current Scenario's tagManager to query for all objects with RotationRandomizerTags to obtain the subset of GameObjects within the simulation that need to have their rotations randomzied. To learn more about how RandomizerTags work, visit the [RandomizerTags doc](RandomizerTags.md).
