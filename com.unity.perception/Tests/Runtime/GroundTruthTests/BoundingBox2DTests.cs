using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Perception;
using UnityEngine.Perception.Sensors;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class BoundingBox2DTests : PassTestBase
    {
        public class ProducesCorrectBoundingBoxesData
        {
            public uint[] classCountsExpected;
            public RenderedObjectInfo[] boundingBoxesExpected;
            public uint[] data;
            public int stride;
            public string name;
            public ProducesCorrectBoundingBoxesData(uint[] data, RenderedObjectInfo[] boundingBoxesExpected, uint[] classCountsExpected, int stride, string name)
            {
                this.data = data;
                this.boundingBoxesExpected = boundingBoxesExpected;
                this.classCountsExpected = classCountsExpected;
                this.stride = stride;
                this.name = name;
            }

            public override string ToString()
            {
                return name;
            }
        }
        public static IEnumerable ProducesCorrectBoundingBoxesTestCases()
        {
            yield return new ProducesCorrectBoundingBoxesData(
                new uint[]
                {
                    1, 1,
                    1, 1
                }, new[]
                {
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(0, 0, 2, 2),
                        instanceId = 1,
                        labelId = 0,
                        pixelCount = 4
                    }
                }, new uint[]
                {
                    1,
					0
                },
                2,
                "SimpleBox");
            yield return new ProducesCorrectBoundingBoxesData(
                new uint[]
                {
                    1, 0, 2,
                    1, 0, 0
                }, new[]
                {
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(0, 0, 1, 2),
                        instanceId = 1,
                        labelId = 0,
                        pixelCount = 2
                    },
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(2, 0, 1, 1),
                        instanceId = 2,
                        labelId = 1,
                        pixelCount = 1
                    }
                }, new uint[]
                {
                    1,
                    1
                },
                3,
                "WithGaps");
            yield return new ProducesCorrectBoundingBoxesData(
                new uint[]
                {
                    1, 2, 1,
                    1, 2, 1
                }, new[]
                {
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(0, 0, 3, 2),
                        instanceId = 1,
                        labelId = 0,
                        pixelCount = 4
                    },
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(1, 0, 1, 2),
                        instanceId = 2,
                        labelId = 1,
                        pixelCount = 2
                    }
                }, new uint[]
                {
                    1,
                    1
                },
                3,
                "Interleaved");
        }

        [UnityTest]
        public IEnumerator ProducesCorrectBoundingBoxes([ValueSource(nameof(ProducesCorrectBoundingBoxesTestCases))]ProducesCorrectBoundingBoxesData producesCorrectBoundingBoxesData)
        {
            var label = "label";
            var label2 = "label2";
            var labelingConfiguration = ScriptableObject.CreateInstance<LabelingConfiguration>();

            labelingConfiguration.LabelingConfigurations = new List<LabelingConfigurationEntry>
            {
                new LabelingConfigurationEntry
                {
                    label = label,
                    value = 500
                },
                new LabelingConfigurationEntry
                {
                    label = label2,
                    value = 500
                }
            };

            var renderedObjectInfoGenerator = new RenderedObjectInfoGenerator(labelingConfiguration);
            var groundTruthLabelSetupSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<GroundTruthLabelSetupSystem>();
            groundTruthLabelSetupSystem.Activate(renderedObjectInfoGenerator);

            //Put a plane in front of the camera
            AddTestObjectForCleanup(TestHelper.CreateLabeledPlane(.1f, label));
            AddTestObjectForCleanup(TestHelper.CreateLabeledPlane(.1f, label2));
            yield return null;

            var dataNativeArray = new NativeArray<uint>(producesCorrectBoundingBoxesData.data, Allocator.Persistent);

            renderedObjectInfoGenerator.Compute(dataNativeArray, producesCorrectBoundingBoxesData.stride, BoundingBoxOrigin.BottomLeft, out var boundingBoxes, out var classCounts, Allocator.Temp);

            CollectionAssert.AreEqual(producesCorrectBoundingBoxesData.boundingBoxesExpected, boundingBoxes.ToArray());
            CollectionAssert.AreEqual(producesCorrectBoundingBoxesData.classCountsExpected, classCounts.ToArray());

            dataNativeArray.Dispose();
            boundingBoxes.Dispose();
            classCounts.Dispose();
            groundTruthLabelSetupSystem.Deactivate(renderedObjectInfoGenerator);
            renderedObjectInfoGenerator.Dispose();
        }
    }
}
