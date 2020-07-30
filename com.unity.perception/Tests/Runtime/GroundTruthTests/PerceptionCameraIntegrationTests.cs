using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;

#if MOQ_PRESENT
using Moq;
#endif

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
              ""label_id"": 100,
              ""label_name"": ""label"",
              ""instance_id"": 1,
              ""x"": 0.0,
              ""y"": {Screen.height / 4:F1},
              ""width"": {Screen.width:F1},
              ""height"": {Screen.height / 2:F1}
            }}";
            var labelingConfiguration = CreateLabelingConfiguration();
            SetupCamera(pc =>
            {
                pc.AddLabeler(new BoundingBox2DLabeler(labelingConfiguration));
            });

            var plane = TestHelper.CreateLabeledPlane();
            AddTestObjectForCleanup(plane);
            //a plane is 10x10 by default, so scale it down to be 10x1 to cover the center half of the image
            plane.transform.localScale = new Vector3(10f, -1f, .1f);
            yield return null;
            DatasetCapture.ResetSimulation();

            var capturesPath = Path.Combine(DatasetCapture.OutputDirectory, "captures_000.json");
            var capturesJson = File.ReadAllText(capturesPath);
            StringAssert.Contains(jsonExpected, capturesJson);
        }

        [UnityTest]
        public IEnumerator EnableSemanticSegmentation_GeneratesCorrectDataset()
        {
            SetupCamera(pc =>
            {
                pc.AddLabeler(new SemanticSegmentationLabeler(CreateSemanticSegmentationLabelConfig()));
            });

            string expectedImageFilename = $"segmentation_{Time.frameCount}.png";

            this.AddTestObjectForCleanup(TestHelper.CreateLabeledPlane());
            yield return null;
            DatasetCapture.ResetSimulation();

            var capturesPath = Path.Combine(DatasetCapture.OutputDirectory, "captures_000.json");
            var capturesJson = File.ReadAllText(capturesPath);
            var imagePath = $"SemanticSegmentation/{expectedImageFilename}";
            StringAssert.Contains(imagePath, capturesJson);
        }

        static IdLabelConfig CreateLabelingConfiguration()
        {
            var label = "label";
            var labelConfig = ScriptableObject.CreateInstance<IdLabelConfig>();

            labelConfig.Init(new List<IdLabelEntry>
            {
                new IdLabelEntry
                {
                    id = 100,
                    label = label
                }
            });
            return labelConfig;
        }
        static SemanticSegmentationLabelConfig CreateSemanticSegmentationLabelConfig()
        {
            var label = "label";
            var labelingConfiguration = ScriptableObject.CreateInstance<SemanticSegmentationLabelConfig>();

            labelingConfiguration.Init(new List<SemanticSegmentationLabelEntry>
            {
                new SemanticSegmentationLabelEntry()
                {
                    label = label,
                    color = Color.blue
                }
            });
            return labelingConfiguration;
        }

        void SetupCamera(Action<PerceptionCamera> initPerceptionCamera)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            initPerceptionCamera?.Invoke(perceptionCamera);

            cameraObject.SetActive(true);
            AddTestObjectForCleanup(cameraObject);
        }
    }
}
