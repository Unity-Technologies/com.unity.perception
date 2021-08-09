using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EditorTests
{
    [TestFixture]
    public class PerceptionCameraEditorUrpTests
    {
        [TearDown]
        public void TearDown()
        {
            GraphicsSettings.renderPipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRPAsset.asset");
        }

        [UnityTest]
        public IEnumerator MissingRendererFeature_ProducesLogError()
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = sceneCount - 1; i >= 0; i--)
            {
                EditorSceneManager.CloseScene(SceneManager.GetSceneAt(i), true);
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/NoGroundTruthURPAsset.asset");
            GraphicsSettings.renderPipelineAsset = urpAsset;

            yield return new EnterPlayMode();

            var gameObject = new GameObject();
            gameObject.SetActive(false);
            gameObject.AddComponent<Camera>();
            gameObject.AddComponent<UniversalAdditionalCameraData>();
            var perceptionCamera = gameObject.AddComponent<PerceptionCamera>();
            gameObject.SetActive(true);
            LogAssert.Expect(LogType.Error, "GroundTruthRendererFeature must be present on the ScriptableRenderer associated with the camera. The ScriptableRenderer can be accessed through Edit -> Project Settings... -> Graphics -> Scriptable Render Pipeline Settings -> Renderer List.");
            yield return null;
            Assert.IsFalse(perceptionCamera.enabled);
            yield return new ExitPlayMode();
            GraphicsSettings.renderPipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRPAsset.asset");
        }
    }
}
