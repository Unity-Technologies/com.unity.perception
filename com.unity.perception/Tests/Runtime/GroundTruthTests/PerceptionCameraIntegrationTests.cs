using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
#if HDRP_PRESENT
    [Ignore("Ignoring in HDRP because of a rendering issue in the first frame. See issue AISV-455.")]
#endif
    public class PerceptionCameraIntegrationTests : GroundTruthTestBase
    {
        [UnityTest]
        [UnityPlatform(RuntimePlatform.LinuxPlayer, RuntimePlatform.WindowsPlayer)]
        public IEnumerator EnableBoundingBoxes_GeneratesCorrectDataset()
        {
            //set resolution to ensure we don't have rounding in rendering leading to bounding boxes to change height/width
            Screen.SetResolution(400, 400, false);
            //give the screen a chance to resize
            yield return null;

            var jsonExpected = $@"            {{
              ""label_id"": 0,
              ""label_name"": ""label"",
              ""instance_id"": 1,
              ""x"": 0.0,
              ""y"": {Screen.height / 4:F1},
              ""width"": {Screen.width:F1},
              ""height"": {Screen.height / 2:F1}
            }}";
            var labelingConfiguration = CreateLabelingConfiguration();
            SetupCamera(labelingConfiguration, pc =>
            {
                pc.produceBoundingBoxAnnotations = true;
            });

            var plane = TestHelper.CreateLabeledPlane();
            AddTestObjectForCleanup(plane);
            //a plane is 10x10 by default, so scale it down to be 10x1 to cover the center half of the image
            plane.transform.localScale = new Vector3(10f, -1f, .1f);
            yield return null;
            SimulationManager.ResetSimulation();

            var capturesPath = Path.Combine(SimulationManager.OutputDirectory, "captures_000.json");
            var capturesJson = File.ReadAllText(capturesPath);
            StringAssert.Contains(jsonExpected, capturesJson);
        }

        [UnityTest]
        public IEnumerator EnableSemanticSegmentation_GeneratesCorrectDataset()
        {
            var labelingConfiguration = CreateLabelingConfiguration();
            SetupCamera(labelingConfiguration, pc => pc.produceSegmentationImages = true);

            string expectedImageFilename = $"segmentation_{Time.frameCount}.png";

            this.AddTestObjectForCleanup(TestHelper.CreateLabeledPlane());
            yield return null;
            SimulationManager.ResetSimulation();

            var capturesPath = Path.Combine(SimulationManager.OutputDirectory, "captures_000.json");
            var capturesJson = File.ReadAllText(capturesPath);
            var imagePath = Path.Combine("SemanticSegmentation", expectedImageFilename).Replace(@"\", @"\\");
            StringAssert.Contains(imagePath, capturesJson);
        }

        static LabelingConfiguration CreateLabelingConfiguration()
        {
            var label = "label";
            var labelingConfiguration = ScriptableObject.CreateInstance<LabelingConfiguration>();

            labelingConfiguration.LabelEntries = new List<LabelEntry>
            {
                new LabelEntry
                {
                    id = 1,
                    label = label,
                    value = 500
                }
            };
            return labelingConfiguration;
        }

        GameObject SetupCamera(LabelingConfiguration labelingConfiguration, Action<PerceptionCamera> initPerceptionCamera)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.produceSegmentationImages = false;
            perceptionCamera.produceRenderedObjectInfoMetric = false;
            perceptionCamera.produceBoundingBoxAnnotations = false;
            perceptionCamera.produceObjectCountAnnotations = false;
            perceptionCamera.captureRgbImages = false;
            perceptionCamera.LabelingConfiguration = labelingConfiguration;
            initPerceptionCamera(perceptionCamera);

            cameraObject.SetActive(true);
            AddTestObjectForCleanup(cameraObject);
            return cameraObject;
        }
    }
}
