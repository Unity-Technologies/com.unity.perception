using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
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

            var labelingConfiguration = CreateLabelingConfiguration();
            SetupCamera(pc =>
            {
                pc.AddLabeler(new BoundingBox2DLabeler(labelingConfiguration));
            });

            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var plane = TestHelper.CreateLabeledPlane();
            var instanceId = plane.GetComponent<Labeling>().instanceId;
            AddTestObjectForCleanup(plane);
            //a plane is 10x10 by default, so scale it down to be 10x1 to cover the center half of the image
            plane.transform.localScale = new Vector3(10f, -1f, .1f);
            plane.transform.localPosition = new Vector3(0, 0, 10);

            var plane2 = TestHelper.CreateLabeledPlane(label: "nonmatching");
            AddTestObjectForCleanup(plane2);
            //place a smaller plane in front to test non-matching objects
            plane2.transform.localScale = new Vector3(.1f, -1f, .1f);
            plane2.transform.localPosition = new Vector3(0, 0, 5);
            yield return null;

            DatasetCapture.ResetSimulation();
            Assert.AreEqual(1, collector.currentRun.frames.Count);
            var f = collector.currentRun.frames[0];
            Assert.NotNull(f);

            Assert.AreEqual(1, f.sensors.Count);
            var s = f.sensors[0];
            Assert.NotNull(s);

            Assert.AreEqual(1, s.annotations.Count);
            var annotation = s.annotations[0];
            Assert.NotNull(annotation);

            Assert.AreEqual("type.unity.com/unity.solo.BoundingBox2DAnnotation", annotation.modelType);
            var boxes = (BoundingBoxAnnotation)annotation;
            Assert.NotNull(boxes);

            Assert.AreEqual(1, boxes.boxes.Count);
            var box = boxes.boxes[0];
            Assert.AreEqual(100, box.labelId);
            Assert.AreEqual("label", box.labelName);
            Assert.AreEqual(instanceId, box.instanceId);
            Assert.AreEqual(Screen.width, box.dimension.x);
            Assert.AreEqual(Screen.height / 2, box.dimension.y);
            Assert.AreEqual(0, box.origin.x);
            Assert.AreEqual(Screen.height / 4, box.origin.y);
        }

        [UnityTest]
        public IEnumerator EnableSemanticSegmentation_GeneratesCorrectDataset([Values(true, false)] bool enabled)
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            SemanticSegmentationLabeler semanticSegmentationLabeler = null;
            SetupCamera(pc =>
            {
                semanticSegmentationLabeler = new SemanticSegmentationLabeler(CreateSemanticSegmentationLabelConfig());
                pc.AddLabeler(semanticSegmentationLabeler);
            }, enabled);

            AddTestObjectForCleanup(TestHelper.CreateLabeledPlane());
            yield return null;
            DatasetCapture.ResetSimulation();

            if (enabled)
            {
                Assert.NotNull(collector.currentRun);
                Assert.AreEqual(1, collector.currentRun.frames.Count);
                Assert.AreEqual(1, collector.currentRun.frames[0].sensors.Count());
                var rgb = collector.currentRun.frames[0].sensors.First() as RgbSensor;
                Assert.NotNull(rgb);
                Assert.AreEqual(1, rgb.annotations.Count());
                var ann = rgb.annotations.First() as SemanticSegmentationAnnotation;
                Assert.NotNull(ann);
                Assert.NotZero(ann.buffer.Length);
            }
            else
            {
                Assert.Null(collector.currentRun.frames);
            }
        }

        [UnityTest]
        public IEnumerator Disabled_GeneratesCorrectDataset()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            SemanticSegmentationLabeler semanticSegmentationLabeler = null;
            SetupCamera(pc =>
            {
                semanticSegmentationLabeler = new SemanticSegmentationLabeler(CreateSemanticSegmentationLabelConfig());
                pc.AddLabeler(semanticSegmentationLabeler);
            });

            AddTestObjectForCleanup(TestHelper.CreateLabeledPlane());
            yield return null;
            DatasetCapture.ResetSimulation();

            Assert.NotNull(collector.currentRun);
            Assert.AreEqual(1, collector.currentRun.frames.Count);
            Assert.AreEqual(1, collector.currentRun.frames[0].sensors.Count());
            var rgb = collector.currentRun.frames[0].sensors.First() as RgbSensor;
            Assert.NotNull(rgb);
            Assert.AreEqual(1, rgb.annotations.Count());
            var ann = rgb.annotations.First() as SemanticSegmentationAnnotation;
            Assert.NotNull(ann);
            Assert.NotZero(ann.buffer.Length);
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
    }
}
