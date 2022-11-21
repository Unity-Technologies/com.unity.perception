using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    public class GroundTruthTestBase : IPrebuildSetup, IPostBuildCleanup
    {
        readonly List<string> m_TestScenePaths = new List<string>
        {
            "Packages/com.unity.perception/Tests/Runtime/TestAssets/AnimatedCubeScene.unity",
            "Packages/com.unity.perception/Tests/Runtime/TestAssets/NonAnimatedCubeScene.unity",
            "Packages/com.unity.perception/Tests/Runtime/TestAssets/CubeScene.unity",
            "Packages/com.unity.perception/Tests/Runtime/TestAssets/UnlitObject.unity",
            "Packages/com.unity.perception/Tests/Runtime/TestAssets/Keypoint_Null_Check_On_Animator.unity",
            "Packages/com.unity.perception/Tests/Runtime/TestAssets/Keypoint_Null_Check_On_Animator_Foreground.unity",
            "Packages/com.unity.perception/Tests/Runtime/TestAssets/AnimatedSkinnedMeshRenderer.unity"
        };
        List<Object> m_ObjectsToDestroy = new List<Object>();
        List<string> m_ScenesToUnload = new List<string>();

        public void Setup()
        {
#if UNITY_EDITOR
            var scenes = UnityEditor.EditorBuildSettings.scenes.ToList();
            scenes.AddRange(m_TestScenePaths.Select(s => new UnityEditor.EditorBuildSettingsScene(s, true)));
            UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            scenes = scenes.Where(s => !m_TestScenePaths.Contains(s.path)).ToArray();
            UnityEditor.EditorBuildSettings.scenes = scenes;
#endif
        }

        [UnitySetUp]
        public virtual IEnumerator Init()
        {
            DatasetCapture.OverrideEndpoint(new NoOutputEndpoint());
            DatasetCapture.ResetSimulation();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in m_ObjectsToDestroy)
                Object.DestroyImmediate(o);

            m_ObjectsToDestroy.Clear();

            foreach (var s in m_ScenesToUnload)
                SceneManager.UnloadSceneAsync(s);

            m_ScenesToUnload.Clear();

            DatasetCapture.ResetSimulation();

            Time.timeScale = 1;
        }

        public void AddTestObjectForCleanup(Object @object) => m_ObjectsToDestroy.Add(@object);

        public void AddSceneForCleanup(string sceneName) => m_ScenesToUnload.Add(sceneName);

        public void DestroyTestObject(Object @object)
        {
            Object.DestroyImmediate(@object);
            m_ObjectsToDestroy.Remove(@object);
        }

        public GameObject SetupCamera(Action<PerceptionCamera> initPerceptionCamera, bool activate = true)
        {
            var cameraObject = new GameObject("Camera");
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;

#if HDRP_PRESENT
            //disable postprocessing on HDRP to ensure unlit objects have precise RGB colors
            var hdAdditionalCameraData = cameraObject.AddComponent<HDAdditionalCameraData>();
            hdAdditionalCameraData.customRenderingSettings = true;

            hdAdditionalCameraData.renderingPathCustomFrameSettingsOverrideMask
                .mask[(uint)FrameSettingsField.Postprocess] = true;

            hdAdditionalCameraData.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Postprocess, false);
#endif

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            initPerceptionCamera?.Invoke(perceptionCamera);

            if (activate)
                cameraObject.SetActive(true);

            AddTestObjectForCleanup(cameraObject);
            return cameraObject;
        }
    }
}
