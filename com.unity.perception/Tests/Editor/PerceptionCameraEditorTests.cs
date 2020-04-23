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

namespace EditorTests
{
    [TestFixture]
    public class PerceptionCameraEditorTests
    {
        [UnityTest]
        public IEnumerator EditorPause_DoesNotLogErrors()
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = sceneCount - 1; i >= 0; i--)
            {
                EditorSceneManager.CloseScene(SceneManager.GetSceneAt(i), true);
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            SetupCamera(ScriptableObject.CreateInstance<LabelingConfiguration>());
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

            SimulationManager.ResetSimulation();

            var capturesPath = Path.Combine(SimulationManager.OutputDirectory, "captures_000.json");
            var capturesJson = File.ReadAllText(capturesPath);
            for (int iFrameCount = expectedFirstFrame; iFrameCount <= expectedLastFrame; iFrameCount++)
            {
                var imagePath = Path.Combine(PerceptionCamera.RgbDirectory, $"rgb_{iFrameCount}").Replace(@"\", @"\\");
                StringAssert.Contains(imagePath, capturesJson);
            }

            yield return new ExitPlayMode();
        }

        static void SetupCamera(LabelingConfiguration labelingConfiguration)
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
            perceptionCamera.LabelingConfiguration = labelingConfiguration;
            perceptionCamera.captureRgbImages = true;
            perceptionCamera.produceBoundingBoxAnnotations = true;
            perceptionCamera.produceObjectCountAnnotations = true;

            cameraObject.SetActive(true);
        }
    }
}
