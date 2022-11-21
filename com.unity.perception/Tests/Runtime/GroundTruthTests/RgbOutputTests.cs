using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.TestTools;

namespace GroundTruthTests.RgbOutputTests
{
    public abstract class RgbOutputTestBase : GroundTruthTestBase
    {
        protected const int k_ColorStructSize = 4;
        internal static readonly Color32 clearPixelValue = new Color32(0, 0, 0, 0);

        internal IEnumerator GenerateRgbOutputAndValidateData(
            PerceptionCamera perceptionCamera, Action<NativeArray<Color32>> validator)
        {
            // Setup the readback. This should happen synchronously since
            // PerceptionCamera.useAsyncReadbackIfSupported was set to false.
            var channel = perceptionCamera.EnableChannel<RGBChannel>();
            channel.outputTextureReadback += (frame, pixels) => validator(pixels);

            // Initialize camera and request a frame for readback
            perceptionCamera.RequestCapture();

            // Wait for readback and validation to complete
            yield return null;
        }

        internal static int ImageToColorDistance(Color32 exemplar, byte[] inputs, int deviation)
        {
            var numItems = inputs.Length;
            var count = 0;
            for (var i = 0; i < numItems; i += 4)
            {
                Color32 c;
                c.r = inputs[i + 0];
                c.g = inputs[i + 1];
                c.b = inputs[i + 2];
                c.a = inputs[i + 3];
                var redDelta = Math.Abs(exemplar.r - c.r);
                var greenDelta = Math.Abs(exemplar.g - c.g);
                var blueDelta = Math.Abs(exemplar.b - c.b);
                var alphaDelta = Math.Abs(exemplar.a - c.a);
                if (redDelta > deviation || greenDelta > deviation || blueDelta > deviation || alphaDelta > deviation)
                    ++count;
            }

            return count;
        }
    }

    public class GenericRgbOutputTests : RgbOutputTestBase
    {
        [UnityTearDown]
        public IEnumerator GenericRgbOutputTeardown()
        {
            TearDown();
            DatasetCapture.ResetSimulation();
            yield return null;
        }

        #region Blank Image Test

        [UnityTest]
        public IEnumerator RgbOutput_DefaultProjectSettings_IsNotEmpty([Values(false, true)] bool useCameraTargetTexture)
        {
            // Setup the camera and scene
            var camera = SetupCamera(cam =>
            {
                cam.captureRgbImages = true;
                cam.captureTriggerMode = CaptureTriggerMode.Manual;
                if (useCameraTargetTexture)
                {
                    cam.GetComponent<Camera>().targetTexture =
                        new RenderTexture(100, 100, 16);
                }
            });
            var perceptionCamera = camera.GetComponent<PerceptionCamera>();
            AddTestObjectForCleanup(camera);
            AddTestObjectForCleanup(TestHelper.CreateLabeledCube());

            // Validate RGB output image by checking if its empty
            yield return GenerateRgbOutputAndValidateData(perceptionCamera, imagePixels =>
            {
                // Check if color buffer is all zeros
                var colorBuffer = imagePixels.Reinterpret<byte>(k_ColorStructSize).ToArray();
                var imageToColorDistance = ImageToColorDistance(clearPixelValue, colorBuffer, 0);
                Assert.IsFalse(imageToColorDistance == 0, "[HDRP] RGB Output was empty for default project settings.");
            });
        }

        #endregion

        [UnityTest]
        public IEnumerator RgbOutput_VerticalOrientationCorrect([Values(false, true)] bool useCameraTargetTexture)
        {
            // Setup the camera and scene.
            var camera = SetupCamera(perceptionCamera =>
            {
                perceptionCamera.captureRgbImages = true;
                perceptionCamera.captureTriggerMode = CaptureTriggerMode.Manual;
                var camera = perceptionCamera.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 0.1f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 2f;
                if (useCameraTargetTexture)
                    camera.targetTexture = new RenderTexture(100, 100, 16);
            });
            var perceptionCamera = camera.GetComponent<PerceptionCamera>();

            // Create a blue quad and position it in front of the camera
            // such that it occupies the bottom half of the screen.
            var expectedBottomColor = new Color32(0, 0, 200, 255);
            var quadBottom = CreateLabeledQuad();
            quadBottom.name = "BottomBlueQuad";
            TestHelper.SetColor(quadBottom, expectedBottomColor);
            quadBottom.transform.position = new Vector3(0, -0.5f, 1);

            // Create a red quad and position it in front of the camera
            // such that it occupies the top half of the screen.
            var expectedTopColor = new Color32(200, 0, 0, 255);
            var quadTop = CreateLabeledQuad();
            quadTop.name = "TopRedQuad";
            TestHelper.SetColor(quadTop, expectedTopColor);
            quadTop.transform.position = new Vector3(0, 0.5f, 1);

            // Readback the RGB output texture and record the bottom left pixel and the top right pixel.
            yield return GenerateRgbOutputAndValidateData(perceptionCamera, imagePixels =>
            {
#if UNITY_STANDALONE_OSX
                if (useCameraTargetTexture)
                {
                    var array = imagePixels.ToArray();
                    Array.Reverse(array);
                    imagePixels = new NativeArray<Color32>(array, Allocator.Persistent);
                }
#endif

                var capturedBottomColor = imagePixels[0];
                var capturedTopColor = imagePixels[imagePixels.Length - 1];

                // Confirm that the the two captured corner pixel colors match their expected color values.
                // Note: We have to accomodate for the rendering pipeline shifting colors slightly during conversions.
                var colorDistBottomLeft = ColorDistance(expectedBottomColor, capturedBottomColor);
                var colorDistTopRight = ColorDistance(expectedTopColor, capturedTopColor);
                Assert.Greater(4, colorDistBottomLeft, $"Expected {expectedBottomColor}, got {capturedBottomColor}");
                Assert.Greater(4, colorDistTopRight, $"Expected {expectedTopColor}, got {capturedTopColor}");
#if UNITY_STANDALONE_OSX
                if (useCameraTargetTexture)
                {
                    imagePixels.Dispose();
                }
#endif
            });
        }

        GameObject CreateLabeledQuad()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "quad";
            var labeling = quad.AddComponent<Labeling>();
            labeling.labels.Add("label");
            AddTestObjectForCleanup(quad);
            return quad;
        }

        static int ColorDistance(Color32 color1, Color32 color2)
        {
            return Math.Abs(color1.a - color2.a) +
                Math.Abs(color1.r - color2.r) +
                Math.Abs(color1.g - color2.g) +
                Math.Abs(color1.b - color2.b);
        }
    }
}
