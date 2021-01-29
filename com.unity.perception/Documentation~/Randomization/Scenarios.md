# Scenarios

Scenarios have three responsibilities:
1. Controlling the execution flow of your simulation
2. Defining a list of Randomizers
3. Defining constants that can be configured externally from a built Unity player 

By default, the Perception package includes one ready-made Scenario, the `FixedLengthScenario` class. This Scenario runs each Iteration for a fixed number of frames and is compatible with the Run in Unity Simulation window for cloud simulation execution.

## Scenario Cloud Execution (Unity Simulation)

Users can utilize Unity's Unity Simulation service to execute a Scenario in the cloud through the perception package's Run in Unity Simulation window. To open this window from the Unity editor using the top menu bar, navigate to `Window -> Run in Unity Simulation`.

From the newly opened editor window, customize the following settings to configure a new Unity Simulation run:
1. **Run Name** - the name of the Unity Simulation run (example: TestRun0)
2. **Total Iterations** - The number of Scenario Iterations to complete during the run
3. **Instance Count** - The number of Unity Simulation worker instances to distribute execution between
4. **Main Scene** - The Unity scene to execute
5. **Scenario** - The Scenario to execute
6. **Sys-Param** - The system parameters or the hardware configuration of Unity Simulation worker instances to execute the Scenario with. Determines per instance specifications such as the number of CPU cores, amount of memory, and presence of a GPU for accelerated execution.

NOTE: To execute a Scenario using the Run in Unity Simulation window, the Scenario class must implement the UnitySimulationScenario class.


## Custom Scenarios

For use cases where the Scenario should run for an arbitrary number of frames, implementing a custom Scenario may be necessary. Below are the two most common Scenario properties a user might want to override to implement custom Scenario Iteration conditions:
1. **isIterationComplete** - determines the conditions that cause the end of a Scenario Iteration
2. **isScenarioComplete** - determines the conditions that cause the end of a Scenario



## JSON Configuration

Scenarios can be serialized to JSON, modified, and reimported at runtime to configure simulation behavior even after a Unity player has been built. Constants and Randomizer Sampler settings are the two primary sections generated when serializing a Scenario. Note that currently, only numerical samplers are serialized. Below is the contents of a JSON configuration file created when serializing the Scenario used in Phase 1 of the [Perception Tutorial](../Tutorial/TUTORIAL.md):
```
{
  "constants": {
    "framesPerIteration": 1,
    "totalIterations": 100,
    "instanceCount": 1,
    "instanceIndex": 0,
    "randomSeed": 123456789
  },
  "randomizers": {
    "HueOffsetRandomizer": {
      "hueOffset": {
        "value": {
          "range": {
            "minimum": -180.0,
            "maximum": 180.0
          }
        }
      }
    },
    "RotationRandomizer": {
      "rotation": {
        "x": {
          "range": {
            "minimum": 0.0,
            "maximum": 360.0
          }
        },
        "y": {
          "range": {
            "minimum": 0.0,
            "maximum": 360.0
          }
        },
        "z": {
          "range": {
            "minimum": 0.0,
            "maximum": 360.0
          }
        }
      }
    }
  }
}
``` 


### Constants
Constants can include properties such as starting Iteration value or total Iteration count, and you can always add your own custom constants. Below is an example of the Constants class used in the `FixedLengthScenario` class:
```
[Serializable]
public class Constants : UnitySimulationScenarioConstants
{
    public int framesPerIteration = 1;
}
```

There are a few key things to note here:
1. The Constants class will need to inherit from `UnitySimulationScenarioConstants` to be compatible with the Run in Unity Simulation window. Deriving from `UnitySimulationScenarioConstants` will add a few key properties to the Constants class that are needed to coordinate a Unity Simulation run.
2. Make sure to include the `[Serializable]` attribute on a constant class. This will ensure that the Constants can be manipulated from the Unity inspector.
3. A Scenario class's `SerializeToJson()` and `DeserializeFromJson()` methods can be overridden to implement custom serialization strategies.


Follow the instructions below to generate a Scenario configuration file to modify your Scenario Constants and Randomizers in a built player:
1. Click the _**Serialize Constants**_ button in the Scenario's inspector window. This will generate a `scenario_configuration.json` file and place it in the project's Assets/StreamingAssets folder.
2. Build your player. The new player will have a [ProjectName]_Data/StreamingAssets folder. A copy of the `scenario_configuration.json` file previously constructed in the editor will be found in this folder.
3. Change the contents of the `scenario_configuration.json` file. Any running player thereafter will utilize the newly authored values.
