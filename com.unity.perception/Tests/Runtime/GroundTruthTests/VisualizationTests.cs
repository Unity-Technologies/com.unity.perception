using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    [TestFixture]
    public class VisualizationTests : GroundTruthTestBase
    {
        GameObject SetupCameraSemanticSegmentation(string name)
        {
            var object1 = new GameObject(name);
            object1.SetActive(false);
            var camera = object1.AddComponent<Camera>();
            var perceptionCamera1 = object1.AddComponent<PerceptionCamera>();
            perceptionCamera1.showVisualizations = true;

#if HDRP_PRESENT
            var hdAdditionalCameraData = object1.AddComponent<HDAdditionalCameraData>();
#endif

            var labelConfig = ScriptableObject.CreateInstance<SemanticSegmentationLabelConfig>();

            labelConfig.Init(new List<SemanticSegmentationLabelEntry>()
            {
                new SemanticSegmentationLabelEntry()
                {
                    label = "label",
                    color = new Color32(10, 20, 30, System.Byte.MaxValue)
                }
            });

            var semanticSegmentationLabeler = new SemanticSegmentationLabeler(labelConfig);

            perceptionCamera1.AddLabeler(semanticSegmentationLabeler);
            return object1;
        }

        [UnityTest]
        public IEnumerator VisualizedCamera_SetsUpCanvas()
        {
            //we are not worried about timing out it has happening because we are not actually making a
            // capture...
            LogAssert.ignoreFailingMessages = true;
            DatasetCapture.ResetSimulation();

            var object1 = SetupCameraSemanticSegmentation(nameof(VisualizedCamera_SetsUpCanvas));
            object1.SetActive(true);
            AddTestObjectForCleanup(object1);

            // Need to wait to make sure a visualization call is made so that the canvas will be constructed
            yield return null;

            Assert.IsNotNull(GameObject.Find("overlay_canvas"));

            DatasetCapture.ResetSimulation();
        }

        [UnityTest]
        public IEnumerator TwoCamerasVisualizing_CausesWarningAndDisablesVisualization()
        {
            //we are not worried about timing out it has happening because we are not actually making a
            // capture...
            LogAssert.ignoreFailingMessages = true;
            DatasetCapture.ResetSimulation();

            var object1 = new GameObject();
            object1.name = nameof(TwoCamerasVisualizing_CausesWarningAndDisablesVisualization);
            object1.SetActive(false);
            object1.AddComponent<Camera>();
            var perceptionCamera1 = object1.AddComponent<PerceptionCamera>();
            perceptionCamera1.showVisualizations = true;
            AddTestObjectForCleanup(object1);

            var object2 = new GameObject();
            object2.SetActive(false);
            object2.name = nameof(TwoCamerasVisualizing_CausesWarningAndDisablesVisualization) + "2";
            object2.AddComponent<Camera>();
            var perceptionCamera2 = object2.AddComponent<PerceptionCamera>();
            perceptionCamera2.showVisualizations = true;
            AddTestObjectForCleanup(object2);

            object1.SetActive(true);
            yield return null;
            LogAssert.Expect(LogType.Warning, $"Currently only one PerceptionCamera may be visualized at a time. Disabling visualization on {nameof(TwoCamerasVisualizing_CausesWarningAndDisablesVisualization)}2.");
            LogAssert.ignoreFailingMessages = true;
            object2.SetActive(true);
            yield return null;

            DatasetCapture.ResetSimulation();
        }

        [UnityTest]
        public IEnumerator DestroyCamera_RemovesVisualization()
        {
            //we are not worried about timing out it has happening because we are not actually making a
            // capture...
            LogAssert.ignoreFailingMessages = true;
            DatasetCapture.ResetSimulation();

            var object1 = SetupCameraSemanticSegmentation(nameof(DestroyCamera_RemovesVisualization));
            object1.SetActive(true);
            AddTestObjectForCleanup(object1);
            //wait a frame to make sure visualize is called once
            yield return null;
            Assert.IsNotNull(GameObject.Find("overlay_canvas"));
            Object.DestroyImmediate(object1);
            //wait a frame to allow objects destroyed via Destroy() to be cleaned up
            yield return null;
            Assert.IsNull(GameObject.Find("overlay_segmentation_canvas"));

            DatasetCapture.ResetSimulation();
        }

        [UnityTest]
        public IEnumerator DestroyAndRecreateCamera_ProperlyVisualizes()
        {
            //we are not worried about timing out it has happening because we are not actually making a
            // capture...
            LogAssert.ignoreFailingMessages = true;
            DatasetCapture.ResetSimulation();

            var object1 = SetupCameraSemanticSegmentation(nameof(DestroyAndRecreateCamera_ProperlyVisualizes));
            object1.SetActive(true);
            AddTestObjectForCleanup(object1);
            //wait a frame to make sure visualize is called once
            yield return null;
            Object.DestroyImmediate(object1);

            var object2 = SetupCameraSemanticSegmentation(nameof(DestroyAndRecreateCamera_ProperlyVisualizes) + "2");
            object2.SetActive(true);
            AddTestObjectForCleanup(object2);

            //wait a frame to make sure visualize is called once
            yield return null;

            Assert.IsNotNull("overlay_canvas");

            DatasetCapture.ResetSimulation();
        }

        [UnityTest]
        public IEnumerator TwoLabelersOfSameType_ProperlyStoredInHud()
        {
            // We are not worried about timing out. It has happening because we are not actually making a capture.
            // LogAssert.ignoreFailingMessages = true;
            DatasetCapture.ResetSimulation();

            var planeObject = TestHelper.CreateLabeledPlane(.1f);
            AddTestObjectForCleanup(planeObject);

            var object1 = new GameObject("PerceptionCamera");
            object1.SetActive(false);
            object1.AddComponent<Camera>();
#if HDRP_PRESENT
            object1.AddComponent<HDAdditionalCameraData>();
#endif
            var perceptionCamera = object1.AddComponent<PerceptionCamera>();
            perceptionCamera.showVisualizations = true;

            var cfg = ScriptableObject.CreateInstance<IdLabelConfig>();
            cfg.Init(new List<IdLabelEntry> { new() { id = 1, label = "label" } });

            var labeler1 = new ObjectCountLabeler(cfg);
            var labeler2 = new ObjectCountLabeler(cfg);
            perceptionCamera.AddLabeler(labeler1);
            perceptionCamera.AddLabeler(labeler2);

            object1.SetActive(true);
            AddTestObjectForCleanup(object1);

            // Wait a couple of frames to make sure visualize has been called.
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            if (perceptionCamera.hudPanel != null)
            {
                Assert.AreEqual(perceptionCamera.hudPanel.entryCount, 2);
            }

            labeler2.visualizationEnabled = false;

            yield return null;

            if (perceptionCamera.hudPanel != null)
            {
                Assert.AreEqual(perceptionCamera.hudPanel.entryCount, 1);
            }

            DatasetCapture.ResetSimulation();
        }

        [UnityTest]
        public IEnumerator EnablingVisualizationsSynchronizesReadbacks([Values(true, false)] bool visualizationsEnabled)
        {
            var startFrame = Time.frameCount;
            var capturedFrame = -1;
            var camera = SetupCamera(cam => cam.showVisualizations = visualizationsEnabled);

            var perceptionCamera = camera.GetComponent<PerceptionCamera>();
            perceptionCamera.EnableChannel<InstanceIdChannel>();
            perceptionCamera.RenderedObjectInfosCalculated += (_, _, _) =>
            {
                if (capturedFrame == -1)
                    capturedFrame = Time.frameCount;
            };

            yield return null;
            yield return null;

            // Destroy the perception camera to force readbacks to complete.
            DestroyTestObject(camera);

            // If visualizations are enabled, a readback event's callback should be invoked during the same frame.
            // If visualizations are not enabled, a readback event's callback should have occured in a following frame.
            if (visualizationsEnabled)
                Assert.AreEqual(startFrame, capturedFrame);
            else
                Assert.Greater(capturedFrame, startFrame);
        }
    }
}
