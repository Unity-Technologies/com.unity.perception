using System;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

[RequireComponent(typeof(PerceptionCamera))]
public class CustomAnnotationAndMetricReporter : MonoBehaviour
{
    public GameObject targetLight;
    public GameObject target;

    MetricDefinition lightMetricDefinition;
    AnnotationDefinition boundingBoxAnnotationDefinition;
    SensorHandle cameraSensorHandle;

    public void Start()
    {
        //Metrics and annotations are registered up-front
        lightMetricDefinition = DatasetCapture.RegisterMetricDefinition(
            "Light position",
            "The world-space position of the light",
            Guid.Parse("1F6BFF46-F884-4CC5-A878-DB987278FE35"));
        boundingBoxAnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition(
            "Target bounding box",
            "The position of the target in the camera's local space",
            id: Guid.Parse("C0B4A22C-0420-4D9F-BAFC-954B8F7B35A7"));
    }

    public void Update()
    {
        //Report the light's position by manually creating the json array string.
        var lightPos = targetLight.transform.position;
        DatasetCapture.ReportMetric(lightMetricDefinition,
            $@"[{{ ""x"": {lightPos.x}, ""y"": {lightPos.y}, ""z"": {lightPos.z} }}]");
        //compute the location of the object in the camera's local space
        Vector3 targetPos = transform.worldToLocalMatrix * target.transform.position;
        //Report using the PerceptionCamera's SensorHandle if scheduled this frame
        var sensorHandle = GetComponent<PerceptionCamera>().SensorHandle;
        if (sensorHandle.ShouldCaptureThisFrame)
        {
            sensorHandle.ReportAnnotationValues(
                boundingBoxAnnotationDefinition,
                new[] { targetPos });
        }
    }
}

// Example metric that is added each frame in the dataset:
// {
//   "capture_id": null,
//   "annotation_id": null,
//   "sequence_id": "9768671e-acea-4c9e-a670-0f2dba5afe12",
//   "step": 1,
//   "metric_definition": "1f6bff46-f884-4cc5-a878-db987278fe35",
//   "values": [{ "x": 96.1856, "y": 192.676, "z": -193.8386 }]
// },

// Example annotation that is added to each capture in the dataset:
// {
//   "id": "33f5a3aa-3e5e-48f1-8339-6cbd64ed4562",
//   "annotation_definition": "c0b4a22c-0420-4d9f-bafc-954b8f7b35a7",
//   "values": [
//     [
//       -1.03097284,
//       0.07265166,
//       -6.318692
//     ]
//   ]
// }
