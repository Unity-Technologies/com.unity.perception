# SimulationManager

`SimulationManager` tracks egos, sensors, annotations, and metrics, combining them into a unified [JSON-based dataset](Schema/Synthetic_Dataset_Schema.md) on disk. It also controls the simulation time elapsed per frame to accommodate the active sensors.

## Sensor scheduling
 While sensors are registered, `SimulationManager` ensures that frame timing is deterministic and run at the appropriate simulation times to let each sensor run at its own rate.
 
 Using [Time.CaptureDeltaTime](https://docs.unity3d.com/ScriptReference/Time-captureDeltaTime.html), it also decouples wall clock time from simulation time, allowing the simulation to run as fast as possible.

## Custom annotations and metrics

```csharp
//example MonoBehaviour 
```
## Custom sensors
