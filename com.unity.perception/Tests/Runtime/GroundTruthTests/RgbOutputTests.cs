using System;
using System.Collections;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
#if URP_PRESENT
using UnityEngine.Rendering.Universal;
#endif
#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif

namespace GroundTruthTests.RgbOutputTests
{
    public abstract class RgbOutputTestBase : GroundTruthTestBase
    {
        protected const int k_ColorStructSize = 4;
        internal static readonly Color32 clearPixelValue = new Color32(0, 0, 0, 0);
        internal PerceptionCamera perceptionCamera;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            PerceptionCamera.useAsyncReadbackIfSupported = false;
            base.Init();
            yield return new WaitForSeconds(2f);
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            base.TearDown();
            yield return new WaitForSeconds(2f);
            PerceptionCamera.useAsyncReadbackIfSupported = true;
        }

        internal IEnumerator GenerateRgbOutputAndValidateData(Action<NativeArray<Color32>> validator)
        {
            // Setup the readback. This should happen synchronously since
            // PerceptionCamera.useAsyncReadbackIfSupported was set to false.
            perceptionCamera.RgbCaptureReadback += (frame, pixels) =>
            {
                validator?.Invoke(pixels);
            };

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

#if URP_PRESENT
    public class UrpRgbOutputTests : RgbOutputTestBase
    {
        static UniversalRenderPipelineAsset urpAsset => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        // Initial Project Settings
        static int s_InitialMsaaSampleCount;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            s_InitialMsaaSampleCount = urpAsset.msaaSampleCount;
        }
        [UnityTearDown]
        public IEnumerator UrpTeardown()
        {
            base.TearDown();

            // Reset project settings
            urpAsset.msaaSampleCount = s_InitialMsaaSampleCount;

            DatasetCapture.ResetSimulation();
            yield return null;
        }

        #region MSAA Test
        static MsaaQuality[] s_MSAAVariations = {
            MsaaQuality.Disabled,
            MsaaQuality._2x,
            MsaaQuality._8x
        };
        [UnityTest]
        public IEnumerator RgbOutput_MsaaVariation_IsNotEmpty(
            [ValueSource(nameof(s_MSAAVariations))]
            MsaaQuality msaa
        )
        {
            // Setup the camera and scene
            var camera = SetupCamera(cam =>
            {
                cam.captureRgbImages = true;
                cam.captureTriggerMode = CaptureTriggerMode.Manual;
                perceptionCamera = cam;
                cam.transform.position = new Vector3(0, 0, -3);

                // Change the MSAA Graphics setting
                urpAsset.msaaSampleCount = (int)msaa;
            });

            AddTestObjectForCleanup(camera);
            AddTestObjectForCleanup(TestHelper.CreateLabeledCube(1f));

            // Validate RGB output image by checking if its empty
            yield return GenerateRgbOutputAndValidateData(imagePixels =>
                {
                    // Check if color buffer is all zeros
                    var colorBuffer = imagePixels.Reinterpret<byte>(k_ColorStructSize).ToArray();
                    var imageToColorDistance = ImageToColorDistance(clearPixelValue, colorBuffer, 0);
                    Assert.IsFalse(imageToColorDistance == 0, $"[URP] RGB Output was empty for MSAA (${msaa.ToString()})");
                }
            );
        }
        #endregion
    }
#endif

    public class GenericRgbOutputTests : RgbOutputTestBase
    {
        [UnityTearDown]
        public IEnumerator GenericRgbOutputTeardown()
        {
            base.TearDown();

            // Reset project settings

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
                perceptionCamera = cam;
            });
            AddTestObjectForCleanup(camera);
            AddTestObjectForCleanup(TestHelper.CreateLabeledCube());

            // Validate RGB output image by checking if its empty
            yield return GenerateRgbOutputAndValidateData(imagePixels =>
                {
                    // Check if color buffer is all zeros
                    var colorBuffer = imagePixels.Reinterpret<byte>(k_ColorStructSize).ToArray();
                    var imageToColorDistance = ImageToColorDistance(clearPixelValue, colorBuffer, 0);
                    Assert.IsFalse(imageToColorDistance == 0, "[HDRP] RGB Output was empty for default project settings.");
                }
            );
        }
        #endregion

        [UnityTest]
        public IEnumerator RgbOutput_VerticalOrientationCorrect([Values(false, true)] bool useCameraTargetTexture)
        {
            // Setup the camera and scene
            var camera = SetupCamera(pcam =>
            {
                pcam.captureRgbImages = true;
                pcam.captureTriggerMode = CaptureTriggerMode.Manual;
                var camera = pcam.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 1f;
                if (useCameraTargetTexture)
                {
                    camera.GetComponent<Camera>().targetTexture =
                        new RenderTexture(100, 100, 16);
                }
                perceptionCamera = pcam;

            });
            AddTestObjectForCleanup(camera);
            //position camera to point straight at the top edge of plane1, such that plane1 takes up the bottom half of
            //the image and plane2 takes up the top half
            camera.transform.localPosition = Vector3.up * 10f;

            //the colors are chosen specifically such that they are not
            var plane1 = TestHelper.CreateLabeledPlane(2f);
            var colorBottom = new Color32(0, 0, 200, 255);
            SetColor(plane1, colorBottom);
            var plane2 = TestHelper.CreateLabeledPlane(2f);
            var colorTop = new Color32(200, 0, 0, 255);
            SetColor(plane2, colorTop);
            plane2.transform.localPosition = plane2.transform.localPosition + Vector3.up * 20f;
            AddTestObjectForCleanup(plane1);
            AddTestObjectForCleanup(plane2);



            //TestHelper.LoadAndStartRenderDocCapture();
            try
            {
                Color32 bottomLeft = new Color32();
                Color32 topRight = new Color32();
                // Validate RGB output image by checking if its empty
                yield return GenerateRgbOutputAndValidateData(imagePixels =>
                    {
                        bottomLeft = imagePixels[0];
                        topRight = imagePixels[imagePixels.Length - 1];
                    }
                );

                //Accomodate for the rendering pipeline causing colors to change slightly during conversions
                Assert.Greater(4, ColorDistance(colorBottom, bottomLeft), $"Expected {colorBottom}, got {bottomLeft}");
                Assert.Greater(4, ColorDistance(colorTop, topRight), $"Expected {colorTop}, got {topRight}");
            }
            finally
            {
                //TestHelper.EndCaptureRenderDoc();
            }
        }

        private int ColorDistance(Color32 color1, Color32 color2)
        {
            return Math.Abs(color1.a - color2.a) +
                   Math.Abs(color1.r - color2.r) +
                   Math.Abs(color1.g - color2.g) +
                   Math.Abs(color1.b - color2.b);
        }

        private static void SetColor(GameObject gameObject, Color color)
        {
            var renderer = gameObject.GetComponent<MeshRenderer>();
            string shaderName = null;
#if HDRP_PRESENT
            shaderName = "HDRP/Unlit";
#endif
#if URP_PRESENT
            shaderName = "Universal Render Pipeline/Unlit";
#endif
            var material = new Material(Shader.Find(shaderName));
            material.color = color;
            renderer.sharedMaterial = material;
        }
    }
}
