# Scenarios

Scenarios have three responsibilities:
1. Controlling the execution flow of your simulation 
2. Customizing the application of random parameters in your project
3. Defining constants that can be configured externally from a built Unity player

By default, the perception package includes one ready-made scenario, the `FixedFrameLengthScenario` class. This scenario is useful for when all created parameters have target GameObjects configured directly in the `ParameterConfiguration` and the scenario execution requires little modification.

More commonly, users will find the need to create their own Scenario class. Below is an overview of the more common scenario properties and methods a user can override:
1. **isIterationComplete** - determines the conditions that cause the end of a scenario iteration
2. **isScenarioComplete** - determines the conditions that cause the end of a scenario
3. **Initialize** - actions to complete before the scenario has begun iterating
4. **Setup** - actions to complete at the beginning of each iteration
5. **Teardown** - actions to complete at the end of each iteration
6. **OnComplete** - actions to complete after the scenario as completed

## Constants
Scenarios define constants from which to expose global simulation behaviors like a starting iteration value or a total iteration count. Users can serialize these scenario constants to JSON, modify them in an external program, and finally reimport the JSON constants at runtime to configure their simulation even after their project has been built. Below is an example of the constants used in the `FixedLengthScenario` class:
```
[Serializable]
public class Constants
{
    public int iterationFrameLength = 1;
    public int startingIteration;
    public int totalIterations = 1000;
}
```
A few key things to note here:
1. Make sure to include the [Serializable] attribute on a constant class. This will ensure that the constants can be manipulated from the Unity inspector.
2. By default, UnityEngine.Object class references cannot be serialized to JSON in a meaningful way. This includes Monobehaviors and SerializedObjects. For more information on what can and can't be serialized, take a look at the [Unity JsonUtility manual](https://docs.unity3d.com/ScriptReference/JsonUtility.html).
3. A scenario class's Serialize() and Deserialized() methods can be overriden to implement custom serialization strategies.
