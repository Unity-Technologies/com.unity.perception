using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class VisualizationTests : GroundTruthTestBase
    {
        [Test]
        public void VisualizedCamera_SetsUpCanvasAndSecondCamera()
        {
            var object1 = new GameObject();
            object1.name = nameof(VisualizedCamera_SetsUpCanvasAndSecondCamera);
            object1.SetActive(false);
            var camera = object1.AddComponent<Camera>();
            var perceptionCamera1 = object1.AddComponent<PerceptionCamera>();
            perceptionCamera1.visualizationEnabled = true;
            object1.SetActive(true);
            AddTestObjectForCleanup(object1);

            Assert.IsNotNull(camera.targetTexture);
            Assert.IsNotNull(GameObject.Find(nameof(VisualizedCamera_SetsUpCanvasAndSecondCamera) + "_VisualizationCamera"));
            Assert.IsNotNull(GameObject.Find(nameof(VisualizedCamera_SetsUpCanvasAndSecondCamera) + "_VisualizationCanvas"));
        }
        [Test]
        public void TwoCamerasVisualizing_CausesWarningAndDisablesVisualization()
        {
            var object1 = new GameObject();
            object1.name = nameof(TwoCamerasVisualizing_CausesWarningAndDisablesVisualization);
            object1.SetActive(false);
            object1.AddComponent<Camera>();
            var perceptionCamera1 = object1.AddComponent<PerceptionCamera>();
            perceptionCamera1.visualizationEnabled = true;
            AddTestObjectForCleanup(object1);

            var object2 = new GameObject();
            object2.SetActive(false);
            object2.name = nameof(TwoCamerasVisualizing_CausesWarningAndDisablesVisualization) + "2";
            object2.AddComponent<Camera>();
            var perceptionCamera2 = object2.AddComponent<PerceptionCamera>();
            perceptionCamera2.visualizationEnabled = true;
            AddTestObjectForCleanup(object2);

            object1.SetActive(true);
            LogAssert.Expect(LogType.Warning, $"Currently only one PerceptionCamera may be visualized at a time. Disabling visualization on {nameof(TwoCamerasVisualizing_CausesWarningAndDisablesVisualization)}2.");
            object2.SetActive(true);
        }
        [UnityTest]
        public IEnumerator DestroyCamera_RemovesVisualization()
        {
            var object1 = new GameObject();
            object1.name = nameof(DestroyCamera_RemovesVisualization);
            object1.SetActive(false);
            object1.AddComponent<Camera>();
            var perceptionCamera1 = object1.AddComponent<PerceptionCamera>();
            perceptionCamera1.visualizationEnabled = true;
            object1.SetActive(true);
            AddTestObjectForCleanup(object1);

            Assert.IsNotNull(GameObject.Find(nameof(DestroyCamera_RemovesVisualization) + "_VisualizationCamera"));
            Object.DestroyImmediate(object1);
            //wait a frame to allow objects destroyed via Destroy() to be cleaned up
            yield return null;
            Assert.IsNull(GameObject.Find(nameof(DestroyCamera_RemovesVisualization) + "_VisualizationCamera"));
        }
        [Test]
        public void DestroyAndRecreateCamera_ProperlyVisualizes()
        {
            var object1 = new GameObject();
            object1.name = nameof(DestroyAndRecreateCamera_ProperlyVisualizes);
            object1.SetActive(false);
            object1.AddComponent<Camera>();
            var perceptionCamera1 = object1.AddComponent<PerceptionCamera>();
            perceptionCamera1.visualizationEnabled = true;
            object1.SetActive(true);
            AddTestObjectForCleanup(object1);
            Object.DestroyImmediate(object1);

            var object2 = new GameObject();
            object2.name = nameof(DestroyAndRecreateCamera_ProperlyVisualizes) + "2";
            object2.SetActive(false);
            var camera2 = object2.AddComponent<Camera>();
            var perceptionCamera2 = object2.AddComponent<PerceptionCamera>();
            perceptionCamera2.visualizationEnabled = true;
            object2.SetActive(true);
            AddTestObjectForCleanup(object2);

            Assert.IsNotNull(camera2.targetTexture);
            Assert.IsNotNull(GameObject.Find(nameof(DestroyAndRecreateCamera_ProperlyVisualizes) + "2_VisualizationCamera"));
        }
    }
}
