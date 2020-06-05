using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

[RequireComponent(typeof(PerceptionCamera))]
public class CustomAnnotationAndMetricExample : MonoBehaviour
{
    public GameObject light;
    public GameObject target;

    MetricDefinition lightPositionMetricDefinition;
    AnnotationDefinition targetBoundingBoxAnnotationDefinition;
    SensorHandle cameraSensorHandle;

    public void Start()
    {
        //Metrics and annotations are registered up-front and are referenced later when values are reported
        lightPositionMetricDefinition = SimulationManager.RegisterMetricDefinition(
            "Light position",
            "The world-space position of the light",
            Guid.Parse("1F6BFF46-F884-4CC5-A878-DB987278FE35"));
        targetBoundingBoxAnnotationDefinition = SimulationManager.RegisterAnnotationDefinition(
            "Target bounding box",
            "The axis-aligned bounding box of the target in the camera's local space",
            id: Guid.Parse("C0B4A22C-0420-4D9F-BAFC-954B8F7B35A7"));
    }

    public void Update()
    {
        
        //Report the light's position by manually creating the json array string.
        var lightPosition = light.transform.position;
        SimulationManager.ReportMetric(lightPositionMetricDefinition,
            $@"[{{ ""x"": {lightPosition.x}, ""y"": {lightPosition.y}, ""z"": {lightPosition.z} }}]");
        //compute the location of the object in the camera's local space
        var targetCameraLocalPosition = transform.worldToLocalMatrix * target.transform.position;
        //Report the annotation on the camera SensorHandle exposed by the PerceptionCamera
        GetComponent<PerceptionCamera>().SensorHandle.ReportAnnotationValues(targetBoundingBoxAnnotationDefinition,new[] { targetCameraLocalPosition });
    }
}

//
// {
//   "id": "71265896-2a46-405a-a3d9-e587cdfac631",
//   "annotation_definition": "c0b4a22c-0420-4d9f-bafc-954b8f7b35a7",
//   "values": [
//     {
//       "Center": {
//         "x": -85.386672973632813,
//         "y": 84.000732421875,
//         "z": 112.38008880615234
//       },
//       "Extents": {
//         "x": 0.64206844568252563,
//         "y": 0.71592754125595093,
//         "z": 0.66213905811309814
//       }
//     }
//   ]
// },
