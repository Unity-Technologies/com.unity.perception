using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    public class VertexNormalTests : GroundTruthTestBase
    {
        static readonly float4 k_NormalPixelValue = new float4(0.5f, 0.5f, 1, 1);

        [UnityTest]
        public IEnumerator NormalLabelerOutputTest()
        {
            var timesNormalImageReceived = 0;

            void OnNormalImageReceived(int frameCount, NativeArray<float4> data)
            {
                timesNormalImageReceived++;
                Assert.IsTrue(data.ToArray().All(pixel =>
                    Mathf.Approximately(pixel.x, k_NormalPixelValue.x) &&
                    Mathf.Approximately(pixel.y, k_NormalPixelValue.y) &&
                    Mathf.Approximately(pixel.z, k_NormalPixelValue.z)));
            }

            var cameraObject = SetupCameraNormalLabeler(OnNormalImageReceived, false);

            // Put a plane in front of the camera
            var planeObject = CreatePlaneAtDistanceAndRotation(10, Quaternion.Euler(90, 0, 0));

            // Wait 3 frames
            for (var i = 0; i < 3; i++)
                yield return null;

            // Destroy the camera to force all pending segmentation image readbacks and subsequent callbacks to finish
            DestroyTestObject(cameraObject);
            Object.DestroyImmediate(planeObject);

            Assert.AreEqual(3, timesNormalImageReceived);
        }

        static GameObject CreatePlaneAtDistanceAndRotation(int distance, Quaternion quaternion)
        {
            var planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            planeObject.transform.SetPositionAndRotation(new Vector3(0, 0, distance), quaternion);
            planeObject.transform.localScale = new Vector3(5, -1, 5);
            return planeObject;
        }

        GameObject SetupCameraNormalLabeler(
            Action<int, NativeArray<float4>> onNormalImageReceived, bool showVisualizations)
        {
            var cameraObject = SetupCamera(out var perceptionCamera, showVisualizations);
            var normalLabeler = new NormalLabeler();
            var channel = perceptionCamera.EnableChannel<VertexNormalsChannel>();
            channel.outputTextureReadback += onNormalImageReceived;
            perceptionCamera.AddLabeler(normalLabeler);
            return cameraObject;
        }

        GameObject SetupCamera(out PerceptionCamera perceptionCamera, bool showVisualizations)
        {
            var cameraObject = new GameObject();
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;
            perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            perceptionCamera.showVisualizations = showVisualizations;

            AddTestObjectForCleanup(cameraObject);
            return cameraObject;
        }
    }
}
