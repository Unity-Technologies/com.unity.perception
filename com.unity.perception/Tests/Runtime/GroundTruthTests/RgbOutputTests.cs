using System;
using System.Collections;
using NUnit.Framework;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
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
        internal static readonly Color32 clearPixelValue = new Color32(0, 0, 0, 0);
        internal PerceptionCamera perceptionCamera;

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            base.TearDown();
            yield return new WaitForSeconds(2f);
        }

        internal IEnumerator GenerateRgbOutputAndValidateData(
            Action<AsyncRequest<CaptureCamera.CaptureState>> validator = null
        )
        {
            // Setup the readback
            AsyncRequest<CaptureCamera.CaptureState> captureState = null;

            void RgbCaptureReadback(AsyncRequest<CaptureCamera.CaptureState> request)
            {
                captureState = request;
            }

            perceptionCamera.RgbCaptureReadback += RgbCaptureReadback;

            // Initialize camera and request a frame for readback
            perceptionCamera.RequestCapture();
            yield return null;

            // perceptionCamera.RgbCaptureReadback -= RgbCaptureReadback;

            // Preliminary check on capture
            Assert.IsTrue(captureState.error == false, "Capture Request had an error.");

            // Custom validation of the readback data
            validator?.Invoke(captureState);
        }

        internal static int ImageToColorDistance(Color32 exemplar, byte[] inputs, int deviation)
        {
            var numItems = ArrayUtilities.Count(inputs);
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
            var camera = SetupCamera((cam =>
            {
                cam.captureRgbImages = true;
                cam.captureTriggerMode = CaptureTriggerMode.Manual;
                perceptionCamera = cam;

                // Change the MSAA Graphics setting
                urpAsset.msaaSampleCount = (int)msaa;
            }));
            AddTestObjectForCleanup(camera);
            AddTestObjectForCleanup(TestHelper.CreateLabeledCube());

            // Validate RGB output image by checking if its empty
            yield return GenerateRgbOutputAndValidateData(
                validator: (captureState) =>
                {
                    // Check if color buffer is all zeros
                    var colorBuffer = ArrayUtilities.Cast<byte>(captureState.data.colorBuffer as Array);
                    var imageToColorDistance = ImageToColorDistance(clearPixelValue, colorBuffer, 0);
                    Assert.IsFalse(imageToColorDistance == 0, $"[URP] RGB Output was empty for MSAA (${msaa.ToString()})");
                }
            );
        }
        #endregion
    }
#endif

#if HDRP_PRESENT
    public class HdrpRgbOutputTests : RgbOutputTestBase
    {
        static HDRenderPipelineAsset urpAsset => GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;

        // Initial Project Settings

        [OneTimeSetUp]
        public void OneTimeSetup()
        {

        }
        [UnityTearDown]
        public IEnumerator UrpTeardown()
        {
            base.TearDown();

            // Reset project settings

            DatasetCapture.ResetSimulation();
            yield return null;
        }

        #region Blank Image Test

        [UnityTest]
        public IEnumerator RgbOutput_DefaultProjectSettings_IsNotEmpty()
        {
            // Setup the camera and scene
            var camera = SetupCamera((cam =>
            {
                cam.captureRgbImages = true;
                cam.captureTriggerMode = CaptureTriggerMode.Manual;
                perceptionCamera = cam;
            }));
            AddTestObjectForCleanup(camera);
            AddTestObjectForCleanup(TestHelper.CreateLabeledCube());

            // Validate RGB output image by checking if its empty
            yield return GenerateRgbOutputAndValidateData(
                validator: (captureState) =>
                {
                    // Check if color buffer is all zeros
                    var colorBuffer = ArrayUtilities.Cast<byte>(captureState.data.colorBuffer as Array);
                    var imageToColorDistance = ImageToColorDistance(clearPixelValue, colorBuffer, 0);
                    Assert.IsFalse(imageToColorDistance == 0, $"[HDRP] RGB Output was empty for default project settings.");
                }
            );
        }
        #endregion
    }
#endif

}
