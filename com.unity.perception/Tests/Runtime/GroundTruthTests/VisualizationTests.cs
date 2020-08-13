using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
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

            Assert.IsNotNull(GameObject.Find(nameof(VisualizedCamera_SetsUpCanvas) + "_segmentation_canvas"));
        }
        [Test]
        public void TwoCamerasVisualizing_CausesWarningAndDisablesVisualization()
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
            LogAssert.Expect(LogType.Warning, $"Currently only one PerceptionCamera may be visualized at a time. Disabling visualization on {nameof(TwoCamerasVisualizing_CausesWarningAndDisablesVisualization)}2.");
            object2.SetActive(true);
        }
        [UnityTest]
        public IEnumerator DestroyCamera_RemovesVisualization()
        {
            var object1 = SetupCameraSemanticSegmentation(nameof(DestroyCamera_RemovesVisualization));
            object1.SetActive(true);
            AddTestObjectForCleanup(object1);
            //wait a frame to make sure visualize is called once
            yield return null;
            Assert.IsNotNull(GameObject.Find(nameof(DestroyCamera_RemovesVisualization) + "_segmentation_canvas"));
            Object.DestroyImmediate(object1);
            //wait a frame to allow objects destroyed via Destroy() to be cleaned up
            yield return null;
            Assert.IsNull(GameObject.Find(nameof(DestroyCamera_RemovesVisualization) + "_segmentation_canvas"));
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

            Assert.IsNotNull(GameObject.Find(nameof(DestroyAndRecreateCamera_ProperlyVisualizes) + "2_segmentation_canvas"));
        }
    }
}
