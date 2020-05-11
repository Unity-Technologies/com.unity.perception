using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class BoundingBox2DTests : GroundTruthTestBase
    {
        public class ProducesCorrectBoundingBoxesData
        {
            public uint[] classCountsExpected;
            public RenderedObjectInfo[] boundingBoxesExpected;
            public uint[] data;
            public BoundingBoxOrigin boundingBoxOrigin;
            public int stride;
            public string name;
            public ProducesCorrectBoundingBoxesData(uint[] data, RenderedObjectInfo[] boundingBoxesExpected, uint[] classCountsExpected, int stride, BoundingBoxOrigin boundingBoxOrigin, string name)
            {
                this.data = data;
                this.boundingBoxesExpected = boundingBoxesExpected;
                this.classCountsExpected = classCountsExpected;
                this.stride = stride;
                this.name = name;
                this.boundingBoxOrigin = boundingBoxOrigin;
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
                        labelId = 1,
                        pixelCount = 4
                    }
                }, new uint[]
                {
                    1,
                    0
                },
                2,
                BoundingBoxOrigin.BottomLeft,
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
                        labelId = 1,
                        pixelCount = 2
                    },
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(2, 0, 1, 1),
                        instanceId = 2,
                        labelId = 2,
                        pixelCount = 1
                    }
                }, new uint[]
                {
                    1,
                    1
                },
                3,
                BoundingBoxOrigin.BottomLeft,
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
                        labelId = 1,
                        pixelCount = 4
                    },
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(1, 0, 1, 2),
                        instanceId = 2,
                        labelId = 2,
                        pixelCount = 2
                    }
                }, new uint[]
                {
                    1,
                    1
                },
                3,
                BoundingBoxOrigin.BottomLeft,
                "Interleaved");
            yield return new ProducesCorrectBoundingBoxesData(
                new uint[]
                {
                    0, 0,
                    0, 0,
                    0, 1
                }, new[]
                {
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(1, 0, 1, 1),
                        instanceId = 1,
                        labelId = 1,
                        pixelCount = 1
                    },
                }, new uint[]
                {
                    1,
                    0
                },
                2,
                BoundingBoxOrigin.TopLeft,
                "TopLeft");
        }

        [UnityTest]
        public IEnumerator ProducesCorrectBoundingBoxes([ValueSource(nameof(ProducesCorrectBoundingBoxesTestCases))] ProducesCorrectBoundingBoxesData producesCorrectBoundingBoxesData)
        {
            var label = "label";
            var label2 = "label2";
            var labelingConfiguration = ScriptableObject.CreateInstance<LabelingConfiguration>();

            labelingConfiguration.LabelEntries = new List<LabelEntry>
            {
                new LabelEntry
                {
                    id = 1,
                    label = label,
                    value = 500
                },
                new LabelEntry
                {
                    id = 2,
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

            renderedObjectInfoGenerator.Compute(dataNativeArray, producesCorrectBoundingBoxesData.stride, producesCorrectBoundingBoxesData.boundingBoxOrigin, out var boundingBoxes, out var classCounts, Allocator.Temp);

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
