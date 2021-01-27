using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace PerformanceTests
{
    static class TestHelper
    {
        public static (PerceptionCamera, IdLabelConfig, SemanticSegmentationLabelConfig, GameObject) CreateThreeBlockScene()
        {
            var labels = new List<string> { "Crate", "Cube", "Box" };
            var idConfig = CreateDefaultLabelConfig();
            var ssConfig = CreateDefaultSemanticSegmentationLabelConfig();
            var camPos = new Vector3(198.0188f, 126.5455f, -267.4195f);
            var camRot = Quaternion.Euler(20.834f, -36.337f, 0);
            var camScale = new Vector3(36.24997f, 36.24997f, 36.24997f);
            var cam = CreatePerceptionCamera(position: camPos, rotation: camRot, scale: camScale);

            const float scale = 36.24997f;
            const float y = 82.92603f;

            var crate = CreateLabeledCube(labels[0], new Vector3(155.9981f, y, -149.9762f), scale: scale);
            var cube = CreateLabeledCube(labels[1], new Vector3(92.92311f, y, -136.2012f), scale: scale);
            var box = CreateLabeledCube(labels[2], new Vector3(96.1856f, y, -193.8386f), scale: scale);

            var root = new GameObject();
            crate.transform.parent = root.transform;
            cube.transform.parent = root.transform;
            box.transform.parent = root.transform;

            return (cam, idConfig, ssConfig, root);
        }

        static IdLabelConfig CreateDefaultLabelConfig(List<string> labels = null)
        {
            var entries = new List<IdLabelEntry>();

            if (labels == null)
            {
                entries.Add(new IdLabelEntry { id = 1, label = "label" });
            }
            else
            {
                var id = 1;
                entries.AddRange(labels.Select(l => new IdLabelEntry { id = id++, label = l }));
            }

            var config = ScriptableObject.CreateInstance<IdLabelConfig>();
            config.Init(entries);

            return config;
        }

        static SemanticSegmentationLabelConfig CreateDefaultSemanticSegmentationLabelConfig()
        {
            var labelConfig = ScriptableObject.CreateInstance<SemanticSegmentationLabelConfig>();
            labelConfig.Init(new List<SemanticSegmentationLabelEntry>()
            {
                new SemanticSegmentationLabelEntry()
                {
                    label = "label",
                    color = new Color32(0, 0, 255, 255)
                }
            });

            return labelConfig;
        }
        
        //public static GameObject CreateLabeledCube(float scale = 10, string label = "label", float x = 0, float y = 0, float z = 0, float roll = 0, float pitch = 0, float yaw = 0)
        public static GameObject CreateLabeledCube(string label = "label", Vector3? position = null, Quaternion? rotation = null, float scale = 10)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetPositionAndRotation(position ?? Vector3.zero, rotation ?? Quaternion.identity);
            cube.transform.localScale = new Vector3(scale, scale, scale);
            var labeling = cube.AddComponent<Labeling>();
            labeling.labels.Add(label);
            return cube;
        }

        public static PerceptionCamera CreatePerceptionCamera(Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);

            cameraObject.transform.localPosition = position ?? Vector3.zero;
            cameraObject.transform.localRotation = rotation ?? Quaternion.identity;
            cameraObject.transform.localScale = scale ?? Vector3.one;

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;

            return perceptionCamera;
        }
    }
}
