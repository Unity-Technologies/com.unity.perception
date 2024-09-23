using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class InstanceSegmentationHierarchyTests : GroundTruthTestBase
    {
        IEnumerator ExecuteTest(bool aggregate, List<InstanceSegmentationEntry> expected)
        {
            Screen.SetResolution(640, 480, false);
            yield return null;

            var labelingConfiguration = CreateLabelingConfiguration();
            var labeler = new InstanceSegmentationLabeler(labelingConfiguration)
            {
                aggregateChildren = aggregate
            };

            var camera = SetupCamera(pc =>
            {
                pc.AddLabeler(labeler);
            });

            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            DatasetCapture.ResetSimulation();

            CreateSceneComponents();

            yield return null;
            DatasetCapture.ResetSimulation();

            Assert.AreEqual(1, collector.currentRun.frames.Count);
            var f = collector.currentRun.frames.First();
            Assert.NotNull(f);

            Assert.AreEqual(1, f.sensors.Count);
            var s = f.sensors[0];
            Assert.NotNull(s);

            Assert.AreEqual(1, s.annotations.Count);
            var annotation = s.annotations[0];
            Assert.NotNull(annotation);

            Assert.AreEqual("type.unity.com/unity.solo.InstanceSegmentationAnnotation", annotation.modelType);
            var instanceAnnotation = (InstanceSegmentationAnnotation)annotation;
            Assert.NotNull(instanceAnnotation);

            var instances = instanceAnnotation.instances.ToList();
            Assert.NotNull(instances);
            Assert.AreEqual(expected.Count, instances.Count);

            foreach (var e in expected)
            {
                var tList = instances.Where(x => x.labelId == e.labelId);
                Assert.NotNull(tList);

                Assert.AreEqual(1, tList.Count());
                var t = tList.First();

                Assert.AreEqual(e.labelId, t.labelId);
                Assert.AreEqual(e.labelName, t.labelName);
            }
        }

        [UnityTest]
        public IEnumerator InstanceSegmentation_Hierarchy_AggregationOn()
        {
            var expected = new List<InstanceSegmentationEntry>
            {
                new()
                {
                    labelId = 1,
                    labelName = "the_big_one"
                }
            };

            return ExecuteTest(true, expected);
        }

        [UnityTest]
        public IEnumerator InstanceSegmentation_Hierarchy_AggregationOff()
        {
            var expected = new List<InstanceSegmentationEntry>
            {
                new()
                {
                    labelId = 2,
                    labelName = "box1"
                },
                new()
                {
                    labelId = 3,
                    labelName = "box2"
                },
                new()
                {
                    labelId = 4,
                    labelName = "box3"
                },
                new()
                {
                    labelId = 5,
                    labelName = "box4"
                }
            };

            return ExecuteTest(false, expected);
        }

        GameObject CreateSceneComponents()
        {
            var go = new GameObject("Props");
            AddTestObjectForCleanup(go);

            var theBigOne = new GameObject("the_big_one");
            AddTestObjectForCleanup(theBigOne);
            theBigOne.transform.position = new Vector3(0, 1.17f, 0);
            theBigOne.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            theBigOne.AddComponent<Labeling>().labels.Add("the_big_one");
            theBigOne.transform.parent = go.transform;

            var box1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            AddTestObjectForCleanup(box1);
            box1.transform.parent = theBigOne.transform;
            box1.transform.localPosition = new Vector3(-0.5f, -1f, 0);
            box1.AddComponent<Labeling>().labels.Add("box1");

            var box2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            AddTestObjectForCleanup(box2);
            box2.transform.parent = theBigOne.transform;
            box2.transform.localPosition = new Vector3(0.75f, -1f, 0);
            box2.AddComponent<Labeling>().labels.Add("box2");

            var box3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            AddTestObjectForCleanup(box3);
            box3.transform.parent = theBigOne.transform;
            box3.transform.localPosition = new Vector3(0.75f, 0.5f, 0);
            box3.AddComponent<Labeling>().labels.Add("box3");

            var box4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            AddTestObjectForCleanup(box4);
            box4.transform.parent = theBigOne.transform;
            box4.transform.localPosition = new Vector3(-0.5f, 0.5f, 0);
            box4.AddComponent<Labeling>().labels.Add("box4");

            return go;
        }

        static IdLabelConfig CreateLabelingConfiguration()
        {
            var labelConfig = ScriptableObject.CreateInstance<IdLabelConfig>();

            labelConfig.Init(new List<IdLabelEntry>
            {
                new()
                {
                    id = 1,
                    label = "the_big_one"
                },
                new()
                {
                    id = 2,
                    label = "box1"
                },
                new()
                {
                    id = 3,
                    label = "box2"
                },
                new()
                {
                    id = 4,
                    label = "box3"
                },
                new()
                {
                    id = 5,
                    label = "box4"
                }
            });

            return labelConfig;
        }
    }
}
