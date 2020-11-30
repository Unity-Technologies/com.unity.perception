# Scenarios

Scenarios have three responsibilities:
1. Controlling the execution flow of your simulation
2. Defining a list of randomizers
3. Defining constants that can be configured externally from a built Unity player 

By default, the perception package includes one ready-made scenario, the `FixedLengthScenario` class. This scenario runs each iteration for a fixed number of frames and is compatible with the Run in USim window for cloud simulation execution.

## Scenario Cloud Execution (USim)

Users can utilize Unity's Unity Simulation (USim) service to execute a scenario in the cloud through the perception package's Run in USim window. To open this window from the Unity editor using the top menu bar, navigate to `Window -> Run in USim`.

From the newly opened editor window, customize the following settings to configure a new USim run:
1. **Run Name** - the name of the USim run (example: TestRun0)
2. **Total Iterations** - The number of scenario iterations to complete during the run
3. **Instance Count** - The number of USim worker instances to distribute execution between
4. **Main Scene** - The Unity scene to execute
5. **Scenario** - The scenario to execute
6. **USim Worker Config** - the type of USim worker instance to execute the scenario with. Determines per instance specifications such as the number of CPU cores, amount of memory, and presence of a GPU for accelerated execution.

NOTE: To execute a scenario using the Run in USim window, the scenario class must implement the USimScenario class.


## Custom Scenarios

For use cases where the scenario should run for an arbitrary number of frames, implementing a custom scenario may be necessary. Below are the two most common scenario properties a user might want to override to implement custom scenario iteration conditions:
1. **isIterationComplete** - determines the conditions that cause the end of a scenario iteration
2. **isScenarioComplete** - determines the conditions that cause the end of a scenario


## Constants
Scenarios define constants from which to expose global simulation behaviors like a starting iteration value or a total iteration count. Users can serialize these scenario constants to JSON, modify them in an external program, and finally reimport the JSON constants at runtime to configure their simulation even after their project has been built. Below is an example of the constants class used in the `FixedLengthScenario` class:
```
[Serializable]
public class Constants : USimConstants
{
    public int framesPerIteration = 1;
}
```

There are a few key things to note here:
1. The constants class will need to inherit from USimConstants to be compatible with the Run in USim window. Deriving from USimConstants will add a few key properties to the constants class that are needed to coordinate a USim run.
2. Make sure to include the [Serializable] attribute on a constant class. This will ensure that the constants can be manipulated from the Unity inspector.
3. By default, UnityEngine.Object class references cannot be serialized to JSON in a meaningful way. This includes Monobehaviors and SerializedObjects. For more information on what can and can't be serialized, take a look at the [Unity JsonUtility manual](https://docs.unity3d.com/ScriptReference/JsonUtility.html).
4. A scenario class's Serialize() and Deserialized() methods can be overriden to implement custom serialization strategies.

Follow the instructions below to generate a constants configuration file to modify your scenario constants in a built player:
1. Click the serialize constants button in the scenario's inspector window. This will generate a constants.json file and place it in the project's Assets/StreamingAssets folder.
2. Build your player. The new player will have a [ProjectName]_Data/StreamingAssets folder. A copy of the constants.json file previously constructed in the editor will be found in this folder.
3. Change the contents of the constants file. Any running player thereafter will utilize the newly authored constants values.
