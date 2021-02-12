using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
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
            var object1 = SetupCameraSemanticSegmentation(nameof(VisualizedCamera_SetsUpCanvas));
            object1.SetActive(true);
            AddTestObjectForCleanup(object1);

            // Need to wait to make sure a visualization call is made so that the canvas will be constructed
            yield return null;

            Assert.IsNotNull(GameObject.Find("overlay_canvas"));
        }
        [UnityTest]
        public IEnumerator TwoCamerasVisualizing_CausesWarningAndDisablesVisualization()
        {
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
        }
        [UnityTest]
        public IEnumerator DestroyCamera_RemovesVisualization()
        {
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
        }
        [UnityTest]
        public IEnumerator DestroyAndRecreateCamera_ProperlyVisualizes()
        {
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
        }

        [UnityTest]
        public IEnumerator TwoLabelersOfSameType_ProperlyStoredInHud()
        {
            var label = "label";
            var planeObject = TestHelper.CreateLabeledPlane(.1f, label);
            AddTestObjectForCleanup(planeObject);

            var object1 = new GameObject("PerceptionCamera");
            object1.SetActive(false);
            object1.AddComponent<Camera>();
            var perceptionCamera = object1.AddComponent<PerceptionCamera>();
            perceptionCamera.showVisualizations = true;

#if HDRP_PRESENT
            var hdAdditionalCameraData = object1.AddComponent<HDAdditionalCameraData>();
#endif
            var cfg = ScriptableObject.CreateInstance<IdLabelConfig>();

            cfg.Init(new List<IdLabelEntry>
            {
                new IdLabelEntry
                {
                    id = 1,
                    label = label
                }
            });

            var labeler1 = new ObjectCountLabeler(cfg);
            labeler1.objectCountMetricId = "a1da3c27-369d-4929-aea6-d01614635ce2";
            var labeler2 = new ObjectCountLabeler(cfg);
            labeler1.objectCountMetricId = "b1da3c27-369d-4929-aea6-d01614635ce2";

            perceptionCamera.AddLabeler(labeler1);
            perceptionCamera.AddLabeler(labeler2);

            object1.SetActive(true);
            AddTestObjectForCleanup(object1);

            //wait a couple of frames to make sure visualize has been called
            yield return null;
            yield return null;

            Assert.AreEqual(perceptionCamera.hudPanel.entryCount, 2);

            labeler2.visualizationEnabled = false;

            yield return null;

            Assert.AreEqual(perceptionCamera.hudPanel.entryCount, 1);

        }
    }
}
