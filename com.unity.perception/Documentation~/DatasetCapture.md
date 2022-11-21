# DatasetCapture

`DatasetCapture` tracks sensors, annotations, and metrics, and delivers data to an active [endpoint](outputs.md). It also controls the simulation time elapsed per frame to accommodate the active sensors.


## Sensor scheduling
While sensors are registered, `DatasetCapture` ensures that frame timing is deterministic and run at the appropriate simulation times to let each sensor render and capture at its own rate.

Using [Time.captureDeltaTime](https://docs.unity3d.com/ScriptReference/Time-captureDeltaTime.html), it also decouples wall clock time from simulation time, allowing the simulation to run as fast as possible.

Note that when using the [Accumulation feature](Accumulation.md) on the Perception Camera, the timings are frozen for the duration of the accumulation process.

## Custom sensors
You can register custom sensors using `DatasetCapture.RegisterSensor()`. The `simulationDeltaTime` you pass in at registration time is used as `Time.captureDeltaTime` and determines how often (in simulation time) frames should be simulated for the sensor to run. This and the `framesBetweenCaptures` value determine at which exact times the sensor should capture the simulated frames. The decoupling of simulation delta time and capture frequency based on frames simulated allows you to render frames in-between captures. If no in-between frames are desired, you can set `framesBetweenCaptures` to 0. When it is time to capture, the `ShouldCaptureThisFrame` check of the `SensorHandle` returns true. `SensorHandle.ReportCapture` should then be called in each of these frames to report the state of the sensor to populate the dataset.

`Time.captureDeltaTime` is set at every frame in order to precisely fall on the next sensor that requires simulation, and this includes multi-sensor simulations. For instance, if one sensor has a `simulationDeltaTime` of 2 and another 3, the first six values for `Time.captureDeltaTime` will be 2, 1, 1, 2, 2, and 1, meaning simulation will happen on the timestamps 0, 2, 3, 4, 6, 8, and 9.

## Custom annotations and metrics
In addition to the common annotations and metrics produced by [PerceptionCamera](PerceptionCamera.md), scripts can produce their own via `DatasetCapture`. You must first create and register annotation and metric definitions using `DatasetCapture.RegisterAnnotationDefinition()` or `DatasetCapture.RegisterMetric()`. These are used~~~~ to report values during runtime.

Annotations and metrics are always associated with the frame they are reported in. They may also be associated with a specific sensor by using the `Report*` methods on `SensorHandle`.

### Example

```csharp
using System;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Rendering;

public class CustomLabeler : CameraLabeler
{
    public override string description => "Demo labeler";
    public override string labelerId => "Demo labeler";
    protected override bool supportsVisualization => false;

    public GameObject targetLight;
    public GameObject target;

    MetricDefinition lightMetricDefinition;
    AnnotationDefinition targetPositionDef;

    class TargetPositionDef : AnnotationDefinition
    {
        public TargetPositionDef(string id)
            : base(id) { }

        public override string modelType => "targetPosDef";
        public override string description => "The position of the target in the camera's local space";
    }

    [Serializable]
    class TargetPosition : Annotation
    {
        public TargetPosition(AnnotationDefinition definition, string sensorId, Vector3 pos)
            : base(definition, sensorId)
        {
            position = pos;
        }

        public Vector3 position;

        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddFloatArray("position", MessageBuilderUtils.ToFloatVector(position));
        }

        public override bool IsValid() => true;

    }

    protected override void Setup()
    {
        lightMetricDefinition =
            new MetricDefinition(
                "LightMetric",
                "lightMetric1",
                "The world-space position of the light");
        DatasetCapture.RegisterMetric(lightMetricDefinition);

        targetPositionDef = new TargetPositionDef("target1");
        DatasetCapture.RegisterAnnotationDefinition(targetPositionDef);
    }

    protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
    {
        //Report the light's position by manually creating the json array string.
        var lightPos = targetLight.transform.position;
        var metric = new GenericMetric(new[] { lightPos.x, lightPos.y, lightPos.z }, lightMetricDefinition);
        DatasetCapture.ReportMetric(lightMetricDefinition, metric);

        //compute the location of the object in the camera's local space
        Vector3 targetPos = perceptionCamera.transform.worldToLocalMatrix * target.transform.position;

        //Report using the PerceptionCamera's SensorHandle if scheduled this frame
        var sensorHandle = perceptionCamera.SensorHandle;

        if (sensorHandle.ShouldCaptureThisFrame)
        {
            var annotation = new TargetPosition(targetPositionDef, sensorHandle.Id, targetPos);
            sensorHandle.ReportAnnotation(targetPositionDef, annotation);
        }
    }
}

// Example metric that is added each frame in the dataset:
// {
//   "capture_id": null,
//   "annotation_id": null,
//   "sequence_id": "9768671e-acea-4c9e-a670-0f2dba5afe12",
//   "step": 1,
//   "metric_definition": "lightMetric1",
//   "values": [
//      96.1856,
//      192.675964,
//      -193.838638
//    ]
// },

// Example annotation that is added to each capture in the dataset:
// {
//     "annotation_id": "target1",
//     "model_type": "targetPosDef",
//     "description": "The position of the target in the camera's local space",
//     "sensor_id": "camera",
//     "id": "target1",
//     "position": [
//         1.85350215,
//         -0.253945172,
//         -5.015307
//     ]
// }
```
