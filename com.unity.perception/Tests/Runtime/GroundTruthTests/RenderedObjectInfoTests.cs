using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class RenderedObjectInfoTests : GroundTruthTestBase
    {
        public class ProducesCorrectObjectInfoData
        {
            public RenderedObjectInfo[] renderedObjectInfosExpected;
            public uint[] data;
            public BoundingBoxOrigin boundingBoxOrigin;
            public int stride;
            public string name;
            public ProducesCorrectObjectInfoData(uint[] data, RenderedObjectInfo[] renderedObjectInfosExpected, int stride, BoundingBoxOrigin boundingBoxOrigin, string name)
            {
                this.data = data;
                this.renderedObjectInfosExpected = renderedObjectInfosExpected;
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
            yield return new ProducesCorrectObjectInfoData(
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
                        pixelCount = 4
                    }
                },
                2,
                BoundingBoxOrigin.BottomLeft,
                "SimpleBox");
            yield return new ProducesCorrectObjectInfoData(
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
                        pixelCount = 2
                    },
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(2, 0, 1, 1),
                        instanceId = 2,
                        pixelCount = 1
                    }
                },
                3,
                BoundingBoxOrigin.BottomLeft,
                "WithGaps");
            yield return new ProducesCorrectObjectInfoData(
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
                        pixelCount = 4
                    },
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(1, 0, 1, 2),
                        instanceId = 2,
                        pixelCount = 2
                    }
                },
                3,
                BoundingBoxOrigin.BottomLeft,
                "Interleaved");
            yield return new ProducesCorrectObjectInfoData(
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
                        pixelCount = 1
                    },
                },
                2,
                BoundingBoxOrigin.TopLeft,
                "TopLeft");
        }

        [UnityTest]
        public IEnumerator ProducesCorrectBoundingBoxes([ValueSource(nameof(ProducesCorrectBoundingBoxesTestCases))] ProducesCorrectObjectInfoData producesCorrectObjectInfoData)
        {
            var label = "label";
            var label2 = "label2";
            var labelingConfiguration = ScriptableObject.CreateInstance<IdLabelConfig>();

            labelingConfiguration.Init(new List<IdLabelEntry>
            {
                new IdLabelEntry
                {
                    id = 1,
                    label = label
                },
                new IdLabelEntry
                {
                    id = 2,
                    label = label2
                }
            });

            var renderedObjectInfoGenerator = new RenderedObjectInfoGenerator();

            //Put a plane in front of the camera
            AddTestObjectForCleanup(TestHelper.CreateLabeledPlane(.1f, label));
            AddTestObjectForCleanup(TestHelper.CreateLabeledPlane(.1f, label2));
            yield return null;

            var dataNativeArray = new NativeArray<uint>(producesCorrectObjectInfoData.data, Allocator.Persistent);

            renderedObjectInfoGenerator.Compute(dataNativeArray, producesCorrectObjectInfoData.stride, producesCorrectObjectInfoData.boundingBoxOrigin, out var boundingBoxes, Allocator.Temp);

            CollectionAssert.AreEqual(producesCorrectObjectInfoData.renderedObjectInfosExpected, boundingBoxes.ToArray());

            dataNativeArray.Dispose();
            boundingBoxes.Dispose();
        }
    }
}
