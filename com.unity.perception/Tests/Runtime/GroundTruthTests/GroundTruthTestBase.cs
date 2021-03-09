using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    public class GroundTruthTestBase
    {
        List<Object> m_ObjectsToDestroy = new List<Object>();
        List<string> m_ScenesToUnload = new List<string>();
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
            if (Directory.Exists(DatasetCapture.OutputDirectory))
                Directory.Delete(DatasetCapture.OutputDirectory, true);
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
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;

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
