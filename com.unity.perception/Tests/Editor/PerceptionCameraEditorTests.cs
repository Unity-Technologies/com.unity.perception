using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if MOQ_PRESENT
using Moq;
using Moq.Protected;
#endif

namespace EditorTests
{
    [TestFixture]
    public class PerceptionCameraEditorTests
    {
        [UnityTest]
        public IEnumerator EditorPause_DoesNotLogErrors()
        {
            ResetScene();
            var cameraObject = SetupCamera(p =>
            {
                var idLabelConfig = ScriptableObject.CreateInstance<IdLabelConfig>();
                p.captureRgbImages = true;
                p.AddLabeler(new BoundingBox2DLabeler(idLabelConfig));
                p.AddLabeler(new RenderedObjectInfoLabeler(idLabelConfig));
            });
            cameraObject.name = "Camera";
            yield return new EnterPlayMode();
            var expectedFirstFrame = Time.frameCount;
            yield return null;
            EditorApplication.isPaused = true;
            //Wait a few editor frames to ensure the issue has a chance to trigger.
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            EditorApplication.isPaused = false;
            var expectedLastFrame = Time.frameCount;
            yield return null;

            DatasetCapture.ResetSimulation();

            var capturesPath = Path.Combine(DatasetCapture.OutputDirectory, "captures_000.json");
            var capturesJson = File.ReadAllText(capturesPath);
            for (int iFrameCount = expectedFirstFrame; iFrameCount <= expectedLastFrame; iFrameCount++)
            {
                var imagePath = $"{GameObject.Find("Camera").GetComponent<PerceptionCamera>().rgbDirectory}/rgb_{iFrameCount}";
                StringAssert.Contains(imagePath, capturesJson);
            }

            yield return new ExitPlayMode();
        }

        static void ResetScene()
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = sceneCount - 1; i >= 0; i--)
            {
                EditorSceneManager.CloseScene(SceneManager.GetSceneAt(i), true);
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }

#if MOQ_PRESENT
        [UnityTest]
        public IEnumerator AddLabelerAfterStart_ShouldInitialize()
        {
            ResetScene();
            yield return new EnterPlayMode();
            var camera = SetupCamera(null);
            var mockLabeler = new Mock<CameraLabeler>();
            yield return null;
            camera.GetComponent<PerceptionCamera>().AddLabeler(mockLabeler.Object);
            yield return null;
            mockLabeler.Protected().Verify("Setup", Times.Once());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator Labeler_ShouldSetupAndUpdateAndOnBeginRenderingInFirstFrame()
        {
            ResetScene();
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            var camera = SetupCamera(null);
            camera.GetComponent<PerceptionCamera>().AddLabeler(mockLabeler.Object);
            yield return null;
            mockLabeler.Protected().Verify("Setup", Times.Once());
            mockLabeler.Protected().Verify("OnUpdate", Times.Once());
            mockLabeler.Protected().Verify("OnBeginRendering", Times.Once());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator AddAndRemoveLabelerInSameFrame_ShouldDoNothing()
        {
            ResetScene();
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            var cameraObject = SetupCamera(null);
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();
            perceptionCamera.AddLabeler(mockLabeler.Object);
            perceptionCamera.RemoveLabeler(mockLabeler.Object);
            yield return null;
            mockLabeler.Protected().Verify("Setup", Times.Never());
            mockLabeler.Protected().Verify("OnUpdate", Times.Never());
            mockLabeler.Protected().Verify("OnBeginRendering", Times.Never());
            mockLabeler.Protected().Verify("Cleanup", Times.Never());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator RemoveLabeler_ShouldCallCleanup()
        {
            ResetScene();
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            var cameraObject = SetupCamera(null);
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();
            perceptionCamera.AddLabeler(mockLabeler.Object);
            yield return null;
            Assert.IsTrue(perceptionCamera.RemoveLabeler(mockLabeler.Object));
            mockLabeler.Protected().Verify("Cleanup", Times.Once());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator RemoveLabeler_OnLabelerNotAdded_ShouldNotCallCleanup()
        {
            ResetScene();
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            var cameraObject = SetupCamera(null);
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();
            yield return null;
            Assert.IsFalse(perceptionCamera.RemoveLabeler(mockLabeler.Object));
            mockLabeler.Protected().Verify("Cleanup", Times.Never());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator DestroyPerceptionCameraObject_ShouldCallCleanup()
        {
            ResetScene();
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            var cameraObject = SetupCamera(null);
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();
            perceptionCamera.AddLabeler(mockLabeler.Object);
            yield return null;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            mockLabeler.Protected().Verify("Cleanup", Times.Once());
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator SetupThrows_ShouldDisable()
        {
            ResetScene();
            yield return new EnterPlayMode();
            var mockLabeler = new Mock<CameraLabeler>();
            mockLabeler.Protected().Setup("Setup").Throws<InvalidOperationException>();
            var labeler = mockLabeler.Object;
            var camera = SetupCamera(null);
            camera.GetComponent<PerceptionCamera>().AddLabeler(labeler);
            LogAssert.Expect(LogType.Exception, "InvalidOperationException: Operation is not valid due to the current state of the object.");
            yield return null;
            mockLabeler.Protected().Verify("Setup", Times.Once());
            mockLabeler.Protected().Verify("OnUpdate", Times.Never());
            mockLabeler.Protected().Verify("OnBeginRendering", Times.Never());
            Assert.IsFalse(labeler.enabled);
            yield return new ExitPlayMode();
        }
#endif

        static GameObject SetupCamera(Action<PerceptionCamera> initPerceptionCameraCallback)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;
#if HDRP_PRESENT
            cameraObject.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
#endif

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            initPerceptionCameraCallback?.Invoke(perceptionCamera);

            cameraObject.SetActive(true);
            return cameraObject;
        }
    }
}
