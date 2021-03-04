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
            public Color32[] data;
            public BoundingBoxOrigin boundingBoxOrigin;
            public int stride;
            public string name;
            public ProducesCorrectObjectInfoData(Color32[] data, RenderedObjectInfo[] renderedObjectInfosExpected, int stride, BoundingBoxOrigin boundingBoxOrigin, string name)
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
            InstanceIdToColorMapping.TryGetColorFromInstanceId(1, out var color1);
            InstanceIdToColorMapping.TryGetColorFromInstanceId(2, out var color2);
            var empty = Color.black;

            yield return new ProducesCorrectObjectInfoData(
                new Color32[]
                {
                    color1, color1,
                    color1, color1
                }, new[]
                {
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(0, 0, 2, 2),
                        instanceId = 1,
                        pixelCount = 4,
                        instanceColor = color1

                    }
                },
                2,
                BoundingBoxOrigin.BottomLeft,
                "SimpleBox");
            yield return new ProducesCorrectObjectInfoData(
                new Color32[]
                {
                    color1, empty, color2,
                    color1, empty, empty
                }, new[]
                {
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(2, 0, 1, 1),
                        instanceId = 2,
                        pixelCount = 1,
                        instanceColor = color2
                    },
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(0, 0, 1, 2),
                        instanceId = 1,
                        pixelCount = 2,
                        instanceColor = color1
                    }
                },
                3,
                BoundingBoxOrigin.BottomLeft,
                "WithGaps");
            yield return new ProducesCorrectObjectInfoData(
                new Color32[]
                {
                    color1, color2, color1,
                    color1, color2, color1
                }, new[]
                {
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(1, 0, 1, 2),
                        instanceId = 2,
                        pixelCount = 2,
                        instanceColor = color2
                    },
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(0, 0, 3, 2),
                        instanceId = 1,
                        pixelCount = 4,
                        instanceColor = color1
                    }
                },
                3,
                BoundingBoxOrigin.BottomLeft,
                "Interleaved");
            yield return new ProducesCorrectObjectInfoData(
                new Color32[]
                {
                    empty, empty,
                    empty, empty,
                    empty, color1
                }, new[]
                {
                    new RenderedObjectInfo()
                    {
                        boundingBox = new Rect(1, 0, 1, 1),
                        instanceId = 1,
                        pixelCount = 1,
                        instanceColor = color1
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

            var dataNativeArray = new NativeArray<Color32>(producesCorrectObjectInfoData.data, Allocator.Persistent);

            var idLabelMap = IdLabelConfig.GetIdLabelCache();
            renderedObjectInfoGenerator.Compute(dataNativeArray, producesCorrectObjectInfoData.stride, producesCorrectObjectInfoData.boundingBoxOrigin, idLabelMap, out var boundingBoxes, Allocator.Temp);

            CollectionAssert.AreEqual(producesCorrectObjectInfoData.renderedObjectInfosExpected, boundingBoxes.ToArray());

            dataNativeArray.Dispose();
            boundingBoxes.Dispose();
            idLabelMap.Dispose();
        }
    }
}
