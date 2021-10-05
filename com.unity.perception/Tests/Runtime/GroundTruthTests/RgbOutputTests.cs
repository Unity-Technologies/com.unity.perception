using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
#if URP_PRESENT
using UnityEngine.Rendering.Universal;
#elif HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif

namespace GroundTruthTests
{
    /// <summary>
    /// Tests the captured RGB images across different Graphics/Editor/Project settings.
    /// </summary>
    /// <remarks>
    /// Notes on adding new variations:
    ///     1. URP and HDRP variations are handled independently (using conditional compilation)
    ///        as each of them has a separate RenderPipelineAsset (which contains project settings)
    ///     2. OneTimeSetup and Teardown are used "reset" project values back to their original value
    ///        as changes to the Quality/Graphics settings would persist even after playmode and across tests.
    ///     3. The <see cref="s_SceneVariations" /> dictionary maps names (so different variations show up clearly
    ///        in the TestRunner UI) to functors which change certain settings before a RGB Capture is requested.
    ///     4. NUnit parallelizes the test runs which does not work well with Global Settings. You can use
    ///        "[[Parallelizable(ParallelScope.None)]" to prevent that
    /// </remarks>
    public class RGBOutputTests : GroundTruthTestBase
    {
        static readonly Color32 k_ClearPixelValue = new Color32(0, 0, 0, 0);
        PerceptionCamera m_PerceptionCamera;

        #region Common
        [UnitySetUp]
        public IEnumerator Setup()
        {
            SetupCamera((cam =>
            {
                cam.captureRgbImages = true;
                cam.captureTriggerMode = CaptureTriggerMode.Manual;

                m_PerceptionCamera = cam;
            }));
            AddTestObjectForCleanup(TestHelper.CreateLabeledCube());
            yield return null;
        }

        static string GetSrpPrefix()
        {
            #if URP_PRESENT
            return "[URP]";
            #elif URP_PRESENT
            return "[HDRP]";
            #else
            return "[Unknown]";
            #endif
        }
        #endregion

        #region URP Configuration
#if URP_PRESENT
        // Project Settings (URP)
        static UniversalRenderPipelineAsset s_UrpAsset;
        static MsaaQuality s_InitialMsaaQuality;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            s_UrpAsset = GetRpAsset();
            s_InitialMsaaQuality = (MsaaQuality) s_UrpAsset.msaaSampleCount;
        }

        [UnityTearDown]
        public IEnumerator RpTeardown()
        {
            TearDown();
            // Reset project settings
            s_UrpAsset.msaaSampleCount = (int) s_InitialMsaaQuality;
            DatasetCapture.ResetSimulation();
            yield return null;
        }

        static Dictionary<string, Action<PerceptionCamera>> s_SceneVariations = new Dictionary<string, Action<PerceptionCamera>>()
        {
            ["Default Project Setup"] = ((camera) =>
            {
                s_UrpAsset.msaaSampleCount = (int)s_InitialMsaaQuality;
            }),
            ["MSAA 2x"] = ((camera) =>
            {
                s_UrpAsset.msaaSampleCount = (int)MsaaQuality._2x;
            }),
            ["MSAA Disabled"] = ((camera) =>
            {
                s_UrpAsset.msaaSampleCount = (int)MsaaQuality.Disabled;
            })
        };
        static string[] sceneVariationKeys => s_SceneVariations.Keys.ToArray();

        static UniversalRenderPipelineAsset GetRpAsset()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
                Debug.LogError("URP Asset is null. Failing RGB Output tests.");

            return urpAsset;
        }
#endif
#endregion

        #region HDRP Configuration
#if HDRP_PRESENT
        // Project Settings (HDRP)
        static HDRenderPipelineAsset s_HdrpAsset;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            s_HdrpAsset = GetRpAsset();
            // No variations for HDRP at the moment.
        }

        [TearDown]
        public void RpTeardown()
        {
            base.TearDown();
            // No variations for HDRP at the moment.
        }

        static Dictionary<string, Action<PerceptionCamera>> s_SceneVariations = new Dictionary<string, Action<PerceptionCamera>>()
        {
            ["Default Project Setup"] = ((camera) => { }),
        };
        static string[] sceneVariationKeys => s_SceneVariations.Keys.ToArray();

        static HDRenderPipelineAsset GetRpAsset()
        {
            var hdrpAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            if (hdrpAsset == null)
                Debug.LogError("HDRP Asset is null. Failing RGB Output tests.");

            return hdrpAsset;
        }
#endif
#endregion

        #region Tests
        [UnityTest]
        public IEnumerator RgbOutput_WithProjectSettingVariations_IsNotEmpty(
            [ValueSource(nameof(sceneVariationKeys))]
            string variationName
        )
        {
            var variationFunctor = s_SceneVariations[variationName];
            yield return RgbOutput_ParametricTest(cam =>
            {
                variationFunctor?.Invoke(cam);
            }, captureState =>
            {
                var colorBuffer = ArrayUtilities.Cast<byte>(captureState.data.colorBuffer as Array);
                var imageToColorDistance = ImageToColorDistance(k_ClearPixelValue, colorBuffer, 0);

                Assert.IsFalse(imageToColorDistance == 0, $"{GetSrpPrefix()} RGB Output was empty for variation: {variationName}");
            });
        }
        #endregion

        #region Test Setup & Helpers
        IEnumerator RgbOutput_ParametricTest(Action<PerceptionCamera> initSetup, Action<AsyncRequest<CaptureCamera.CaptureState>> validator)
        {
            // Setup the readback
            AsyncRequest<CaptureCamera.CaptureState> captureState = null;

            void RgbCaptureReadback(AsyncRequest<CaptureCamera.CaptureState> request)
            {
                captureState = request;
            }

            m_PerceptionCamera.RgbCaptureReadback += RgbCaptureReadback;

            // Initialize camera and request a frame for readback
            initSetup?.Invoke(m_PerceptionCamera);

            // Need to do this for the updated ScriptableRenderPipelineAsset to take effect..
            yield return new WaitForFixedUpdate();
            m_PerceptionCamera.RequestCapture();

            // Wait for the readback
            while (captureState == null || captureState.completed == false)
            {
                yield return null;
            }

            m_PerceptionCamera.RgbCaptureReadback -= RgbCaptureReadback;

            // Preliminary check on capture
            Assert.IsTrue(captureState.error == false, "Capture Request had an error.");

            // Custom validation of the readback data
            validator?.Invoke(captureState);
        }

        static int ImageToColorDistance(Color32 exemplar, byte[] inputs, int deviation)
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
        #endregion

    }
}
