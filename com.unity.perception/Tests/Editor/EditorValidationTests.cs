using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception;

namespace ValidationTests
{
    //[Ignore("Test Freezing right now")]
    public class EditorValidationTests : ContentTestBaseSetup
    {
        [Test]
        public void AssetsUVDataTests()
        {
            Assert.IsTrue(tests.UVDataTest(meshFilters.ToArray()), "UV Data contains errors or is empty");
        }

        [Test]
        public void AssetsUVRangeTests()
        {
            var test = tests.UVRangeTest(meshFilters.ToArray());
            Assert.IsTrue(test, string.Format("UV(s) have range test has failed due to coordinates not in a range of 0 , 1"));
        }

        [Test]
        public void AssetsInvertedNormalsTests()
        {
            var failedMeshfilters = new List<string>();
            var test = tests.MeshInvertedNormalsTest(meshFilters.ToArray(), out failedMeshfilters);
            foreach (var mesh in failedMeshfilters)
            {
                Debug.Log(string.Format("{0} mesh has inverted normals", mesh));
            }
            Assert.IsTrue(test, "Normals are inverted");
        }

        [Test]
        public void AssetsScaleTest()
        {
            Assert.IsTrue(tests.TestScale(meshRenderers.ToArray()), "Asset scale is outside the bounds");
        }

        [Ignore("Test Freezing right now")]
        [Test]
        public void AssetMeshUnweldedVerts()
        {
            var fail = new List<string>();
            tests.MeshDetectUnWeldedVerts(meshFilters.ToArray(), out fail);

            foreach (var mesh in fail)
            {
                Debug.Log(string.Format("{0} mesh has un-welded vertices", mesh));
            }

            Assert.AreEqual(0, fail.Count, "Meshes have un-welded vertices!");
        }

        [Test]
        public void AssetTransformsTest()
        {
            var failedGameObjects = new List<GameObject>();
            foreach (var o in selectionLists)
            {
                var transformsResult = tests.TransformTest(o, out failedGameObjects);
            }

            foreach (var fail in failedGameObjects)
            {
                var failedCount = fail.GetComponentsInParent<Component>();
                Debug.Log(string.Format("{0} Parent Transforms are not correct", fail.name));
            }

            Assert.AreEqual(0, failedGameObjects.Count, "GameObjects failed transforms test");
        }

        [Test]
        public void AssetsParentComponentTest()
        {
            var failedGameObjects = new List<GameObject>();
            foreach (var o in selectionLists)
            {
                var transformsResult = tests.EmptyComponentsTest(o);
                if (!transformsResult)
                {
                    failedGameObjects.Add(o);
                }
            }

            foreach (var fail in failedGameObjects)
            {
                Debug.Log(string.Format("{0} Parent GameObject is not empty", fail.name));
            }

            Assert.AreEqual(0, failedGameObjects.Count, "Assets Parent GameObjects Contain more then Transform and Metadata components");
        }

        [Test]
        public void AssetPivotPoint()
        {
            var failed = new List<GameObject>();
            foreach (var o in selectionLists)
            {
                var pivotPoints = tests.AssetCheckPivotPoint(o, out failed);
            }

            if (failed.Count != 0)
            {
                foreach (var o in failed)
                {
                    Debug.Log(string.Format("{0} Pivot point is not in the correct position!", o.name));
                }
            }

            Assert.IsEmpty(failed, "Assets pivot point is not in the correct position!");
        }

        [Ignore("Test Freezing right now")]
        [Test]
        public void AssetTexelDensity()
        {
            var scale = 4;
            var targetResolution = 2048;
            var failedMeshes = new List<GameObject>();

            var texelDensity = tests.AssetTexelDensity(meshFilters.ToArray(), meshRenderers.ToArray(), out failedMeshes, scale, targetResolution);

            for (int i = 0; i < failedMeshes.Count; i++)
            {
                Debug.Log(string.Format("{0}doesn't support 20.48 texel density!", failedMeshes[i].name));
            }

            Assert.IsTrue(texelDensity, "Assets currently don't support 2048 texel density");
        }

        [Ignore("Test Freezing right now")]
        [Test]
        public void AssetsOpenMeshTest()
        {
            var failed = new List<string>();
            var openMesh = tests.MeshOpenFaceTest(meshFilters.ToArray(), out failed);

            if (failed.Count != 0)
            {
                foreach (var mesh in failed)
                {
                    Debug.Log(string.Format("{0} has open faces in the mesh!", mesh));
                }
            }

            Assert.IsTrue(openMesh, "Assets currently have holes in the Mesh");
        }
    }
}
