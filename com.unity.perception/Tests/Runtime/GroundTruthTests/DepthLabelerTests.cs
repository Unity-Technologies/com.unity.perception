using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    public class DepthLabelerTests : GroundTruthTestBase
    {
        const int k_QuadDistance = 10;

        [UnityTest]
        public IEnumerator DifferentCameraProjectionsProduceValidOutput(
            [Values(true, false)]
            bool isOrthographic,
            [Values(DepthMeasurementStrategy.Depth, DepthMeasurementStrategy.Range)]
            DepthMeasurementStrategy measurementStrategy)
        {
            // Create a new perception camera with the given camera projection (orthographic or perspective).
            var perceptionCamera = SetupCamera(isOrthographic);

            // Plane a large quad in front of the camera. This quad should take up the entire fov of the camera.
            CreateQuadAtDistance(k_QuadDistance);

            // Readback the depth channel's output texture to validation the captured depth values.
            EnableDepthChannel(perceptionCamera, measurementStrategy, (_, pixelData) =>
            {
                // Identify all unique depth values in the depth image.
                var uniqueDepthValues = new HashSet<float4>();
                foreach (var value in pixelData)
                    uniqueDepthValues.Add(value);

                if (measurementStrategy == DepthMeasurementStrategy.Depth)
                {
                    // Confirm that all the captured depth values are the same.
                    Assert.AreEqual(1, uniqueDepthValues.Count);
                    Assert.IsTrue(uniqueDepthValues.Contains(new float4(10f, 0f, 0f, 1f)));

                    // Confirm that all depth values are equal to the distance of the quad from the camera.
                    Assert.IsTrue(pixelData.ToArray().All(a => Math.Abs(a.x - k_QuadDistance) < float.Epsilon));
                }
                else
                {
                    // Confirm that all the captured depth values are not the same.
                    // The captured depth image should look like a radial gradient.
                    Assert.Greater(uniqueDepthValues.Count, 1);

                    // Confirm that all depth values are greater than or equal to the distance of the quad from the camera.
                    Assert.IsTrue(pixelData.ToArray().Any(a => a.x >= k_QuadDistance));
                }
            });

            yield return null;
        }

        void CreateQuadAtDistance(float distance)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "quad";
            quad.transform.position = new Vector3(0, 0, distance);
            quad.transform.localScale = new Vector3(100, 100, 1);
            AddTestObjectForCleanup(quad);
        }

        void EnableDepthChannel(
            PerceptionCamera perceptionCamera,
            DepthMeasurementStrategy measurementStrategy,
            Action<int, NativeArray<float4>> callback)
        {
            if (measurementStrategy == DepthMeasurementStrategy.Depth)
            {
                var channel = perceptionCamera.EnableChannel<DepthChannel>();
                channel.outputTextureReadback += callback;
            }
            else
            {
                var channel = perceptionCamera.EnableChannel<RangeChannel>();
                channel.outputTextureReadback += callback;
            }
        }

        PerceptionCamera SetupCamera(bool enableOrthographic)
        {
            var cameraObject = new GameObject();
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = enableOrthographic;
            camera.orthographicSize = 1;
            camera.targetTexture = new RenderTexture(32, 32, 32, GraphicsFormat.R8G8B8A8_SRGB);

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;

            AddTestObjectForCleanup(cameraObject);
            return perceptionCamera;
        }
    }
}
