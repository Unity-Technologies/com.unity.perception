#if PERFORMANCE_TESTING_PRESENT
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using Random = UnityEngine.Random;

namespace PerformanceTests
{
    static class TestHelper
    {
        public static (PerceptionCamera, IdLabelConfig, SemanticSegmentationLabelConfig, GameObject) CreateHundredBlockScene()
        {
            var labels = new List<string> { "Crate", "Cube", "Box" };
            var idConfig = CreateDefaultLabelConfig(labels);
            var ssConfig = CreateDefaultSemanticSegmentationLabelConfig(labels);
            var camPos = new Vector3(2, 5, -8);
            var camRot = Quaternion.Euler(20, 10, 0);
            var cam = CreatePerceptionCamera(position: camPos, rotation: camRot, orthoSize: 3.5f);

            const float scale = .5f;

            var root = new GameObject();
            for (int x = 0; x < 10; x++)
            {
                for (int z = 0; z < 10; z++)
                {
                    var cube = CreateLabeledCube(labels[(x * z) % 3], new Vector3(x, 0, z), scale: scale);
                    cube.transform.parent = root.transform;
                }
            }

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

        static SemanticSegmentationLabelConfig CreateDefaultSemanticSegmentationLabelConfig(List<string> labels = null)
        {
            var labelConfig = ScriptableObject.CreateInstance<SemanticSegmentationLabelConfig>();
            List<SemanticSegmentationLabelEntry> semanticSegmentationLabelEntries;
            if (labels == null)
            {
                semanticSegmentationLabelEntries = new List<SemanticSegmentationLabelEntry>()
                {
                    new SemanticSegmentationLabelEntry()
                    {
                        label = "label",
                        color = new Color32(0, 0, 255, 255)
                    }
                };
            }
            else
            {
                Random.InitState(0);
                semanticSegmentationLabelEntries = new List<SemanticSegmentationLabelEntry>(
                    labels.Select(l => new SemanticSegmentationLabelEntry()
                    {
                        label = l,
                        color = Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f)
                    }));
            }
            labelConfig.Init(semanticSegmentationLabelEntries);

            return labelConfig;
        }

        public static GameObject CreateLabeledCube(string label = "label", Vector3? position = null, Quaternion? rotation = null, float scale = 10)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetPositionAndRotation(position ?? Vector3.zero, rotation ?? Quaternion.identity);
            cube.transform.localScale = new Vector3(scale, scale, scale);
            var labeling = cube.AddComponent<Labeling>();
            labeling.labels.Add(label);
            return cube;
        }

        public static PerceptionCamera CreatePerceptionCamera(Vector3? position = null, Quaternion? rotation = null, float orthoSize = 1)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);

            cameraObject.transform.localPosition = position ?? Vector3.zero;
            cameraObject.transform.localRotation = rotation ?? Quaternion.identity;

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthoSize;

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;

            return perceptionCamera;
        }
    }
}
#endif
