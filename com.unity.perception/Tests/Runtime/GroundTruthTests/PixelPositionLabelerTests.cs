using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    public class PixelPositionLabelerTests : GroundTruthTestBase
    {
        const int k_PlaneDistanceFromCamera = 10;

        [UnityTest]
        public IEnumerator PixelPositionLabelerBlackboxTest([Values(true, false)] bool orthographic)
        {
            var cameraObject = SetupCameraPixelPositionLabeler(false, orthographic);
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();

            var channel = perceptionCamera.EnableChannel<PixelPositionChannel>();
            channel.outputTextureReadback += (frame, data) =>
            {
                var sensor = perceptionCamera.cameraSensor;
                var imageWidth = sensor.pixelWidth;
                var imageHeight = sensor.pixelHeight;

                // See if the red channel for every pixel at (X,Y) is greater than (X-1, Y)
                // Screen space x position increases left to right
                for (var y = 0; y < imageHeight; y++)
                {
                    for (var x = 1; x < imageWidth; x++)
                    {
                        var pixel = data[(y * imageWidth) + x];
                        var pixelToLeft = data[(y * imageWidth) + (x - 1)];

                        Assert.IsTrue(pixel.x > pixelToLeft.x, "Image X values should be in increasing order.");
                    }
                }

                // See if the green channel for every pixel at (X,Y) is greater than (X, Y-1)
                // Screen space y position increases bottom to top
                for (var x = 0; x < imageWidth; x++)
                {
                    for (var y = 1; y < imageHeight; y++)
                    {
                        var pixel = data[(y * imageWidth) + x];
                        var pixelAbove = data[((y - 1) * imageWidth) + x];

                        Assert.IsTrue(pixel.y > pixelAbove.y, "Image Y values should be in increasing order.");
                    }
                }

                var firstDepth = data[0].z;
                Assert.IsTrue(data.All(p => Math.Abs(p.z - firstDepth) <= 0.05f), "Image depth should be the same all over.");
            };

            // Put a plane in front of the camera
            var planeObject = CreatePlaneAtDistanceAndRotation(k_PlaneDistanceFromCamera, Quaternion.Euler(90, 0, 0));

            // Wait a frame
            yield return null;

            // Destroy the camera to force all pending segmentation image readbacks and subsequent callbacks to finish
            DestroyTestObject(cameraObject);
            Object.DestroyImmediate(planeObject);
        }

        static GameObject CreatePlaneAtDistanceAndRotation(int distance, Quaternion quaternion)
        {
            var planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            planeObject.transform.SetPositionAndRotation(new Vector3(0, 0, distance), quaternion);
            planeObject.transform.localScale = new Vector3(10, -1, 10);
            return planeObject;
        }

        GameObject SetupCameraPixelPositionLabeler(bool showVisualizations, bool enableOrthographic)
        {
            var cameraObject = new GameObject();
            var camera = cameraObject.AddComponent<Camera>();
            if (enableOrthographic)
            {
                camera.orthographic = true;
                camera.orthographicSize = 1;
            }
            else
            {
                camera.orthographic = false;
            }

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            perceptionCamera.showVisualizations = showVisualizations;

            AddTestObjectForCleanup(cameraObject);

            var pixelPositionLabeler = new PixelPositionLabeler();
            perceptionCamera.AddLabeler(pixelPositionLabeler);
            return cameraObject;
        }
    }
}
