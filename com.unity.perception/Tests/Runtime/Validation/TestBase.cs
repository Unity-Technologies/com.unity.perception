using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Perception.Content;

namespace UnityEngine.Perception
{
    public class ContentTestBaseSetup : MonoBehaviour
    {
        public static List<GameObject> selectionLists = new List<GameObject>();
        public static List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
        public static List<MeshFilter> meshFilters = new List<MeshFilter>();
        public static List<string> assets = new List<string>();

        public TestHelpers testHelpers;
        public ContentValidation tests; 

        [SetUp]
        public void Setup()
        {
            tests = new ContentValidation();
            testHelpers = new TestHelpers();
            selectionLists = new List<GameObject>();
            testHelpers.AssetListCreation();
        }

        [TearDown]
        public void TearDown()
        {
            selectionLists.Clear();
        }
    }

    public class TestHelpers : ContentTestBaseSetup
    {
        public void AssetListCreation()
        {
            assets = AssetDatabase.GetAllAssetPaths().Where(o => o.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (string o in assets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(o);

                if (asset != null)
                    selectionLists.Add(asset);
            }

            foreach (var gameObj in selectionLists)
            {
                var meshRender = gameObj.GetComponentsInChildren<MeshRenderer>();
                var meshFilter = gameObj.GetComponentsInChildren<MeshFilter>();

                foreach (var renderer in meshRender)
                {
                    meshRenderers.Add(renderer);
                }

                foreach (var filter in meshFilter)
                {
                    meshFilters.Add(filter);
                }
            }
        }

        public static IEnumerator SkipFrame(int frames)
        {
            Debug.Log(string.Format("Skipping {0} frames.", frames));
            for (int f = 0; f < frames; f++)
            {
                yield return null;
            }
        }

        public static IEnumerator SkipFrame()
        {
            yield return SkipFrame(1);
        }
    }
}
