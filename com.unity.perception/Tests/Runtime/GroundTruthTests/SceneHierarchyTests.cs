using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace GroundTruthTests
{
    static class SceneHierarchyTestsExtensions
    {
        public static void AssertInHierarchy(this SceneHierarchyInformation info, GameObject obj)
        {
            Assert.IsTrue(info.hierarchy.ContainsKey(obj.GetComponent<Labeling>().instanceId), $"\"{obj.name}\" not in output hierarchy");
        }

        public static void AssertNotInHierarchy(this SceneHierarchyInformation info, GameObject obj)
        {
            var labeling = obj.GetComponent<Labeling>();
            if (labeling == null)
                return;

            Assert.IsTrue(!info.hierarchy.ContainsKey(labeling.instanceId), $"unexpected \"{obj.name}\" in output hierarchy");
        }

        public static void AssertHasParent(this SceneHierarchyInformation info, GameObject obj, GameObject parent)
        {
            var parentInstanceId = info.hierarchy[obj.GetComponent<Labeling>().instanceId].parentInstanceId;
            Assert.IsTrue(parentInstanceId.HasValue, $"{obj.name} has no parent in output hierarchy.");
            Assert.AreEqual(parent.GetComponent<Labeling>().instanceId, parentInstanceId.Value, $"parent instance id is incorrect in output hierarchy");
        }

        public static void AssertNoParent(this SceneHierarchyInformation info, GameObject obj)
        {
            Assert.IsTrue(!info.hierarchy[obj.GetComponent<Labeling>().instanceId].parentInstanceId.HasValue, $"{obj.name} has unexpected parent");
        }

        public static void AssertNoChildren(this SceneHierarchyInformation info, GameObject obj)
        {
            Assert.AreEqual(0, info.hierarchy[obj.GetComponent<Labeling>().instanceId].childrenInstanceIds.Count, $"{obj.name} has unexpected children");
        }

        public static void AssertHasChildren(this SceneHierarchyInformation info, GameObject obj, params GameObject[] children)
        {
            Assert.NotZero(info.hierarchy[obj.GetComponent<Labeling>().instanceId].childrenInstanceIds.Count, $"{obj.name} has no children");
            var childInstanceIds = info.hierarchy[obj.GetComponent<Labeling>().instanceId].childrenInstanceIds;
            foreach (var child in children)
            {
                Assert.IsTrue(childInstanceIds.Contains(child.GetComponent<Labeling>().instanceId), $"child \"{child.name}\" not recorded in output hierarchy");
            }

            // make sure we are testing all children
            Assert.AreEqual(childInstanceIds.Count, children.Length, $"not all children are being tested for");
        }
    }

    [TestFixture]
    public class SceneHierarchyTests
    {
        PerceptionCamera m_PerceptionCamera;
        GameObject m_CamObject;
        Camera m_Camera;
        GameObject m_Root;

        [SetUp]
        public void SetupScene()
        {
            m_CamObject = new GameObject("Main Camera");
            m_Root = new GameObject("Root");
            m_Camera = m_CamObject.AddComponent<Camera>();
            m_Camera.orthographic = true;
            m_Camera.orthographicSize = 10;
            m_PerceptionCamera = m_CamObject.AddComponent<PerceptionCamera>();
            m_PerceptionCamera.EnableChannel<InstanceIdChannel>();
            m_PerceptionCamera.enabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            m_PerceptionCamera.enabled = false;
            Object.DestroyImmediate(m_CamObject.gameObject);
            Object.DestroyImmediate(m_Root.gameObject);
        }

        static GameObject AddGameObjectChild(
            string gameObjectName, GameObject parent, bool labelIt,
            int x = 0, int y = 0, int z = 10,
            Action<GameObject> prep = null
        )
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = gameObjectName;

            if (parent != null)
                go.transform.parent = parent.transform;

            go.transform.position = new Vector3(x, y, z);

            if (labelIt)
            {
                var goL = go.AddComponent<Labeling>();
                goL.labels = new List<string>() { gameObjectName };
                goL.RefreshLabeling();
            }

            prep?.Invoke(go);

            return go;
        }

        [UnityTest]
        public IEnumerator ValidateCorrectBasicHierarchyProduced()
        {
            var L1 = AddGameObjectChild("L1", m_Root, true); // has label, no parent
            var L1U1 = AddGameObjectChild("L1U1", L1, false, 2); // no label, l1 parent
            var L1L1 = AddGameObjectChild("L1L1", L1, true, 4); // has label, l1 parent

            /*  Expected Output Hierarchy
             *   - L1
             *     - L1L1
             */

            m_PerceptionCamera.RenderedObjectInfosCalculated += (_, _, hierarchy) =>
            {
                //// Hierarchy Items Verification
                // Verify things in hierarchy
                hierarchy.AssertInHierarchy(L1);
                hierarchy.AssertInHierarchy(L1L1);
                Assert.AreEqual(2, hierarchy.hierarchy.Count);

                // Verify things not in hierarchy
                hierarchy.AssertNotInHierarchy(L1U1);

                //// Hierarchy Parent Verification
                hierarchy.AssertNoParent(L1);
                hierarchy.AssertHasParent(L1L1, L1);

                //// Hierarchy Children Verification
                hierarchy.AssertHasChildren(L1, L1L1);
                hierarchy.AssertNoChildren(L1L1);

                Debug.Log("Hierarchy tests complete.");
            };

            m_PerceptionCamera.enabled = true;
            yield return null;
        }

        [UnityTest]
        public IEnumerator ValidateCorrectDeepHierarchyProduced()
        {
            // has label, no parent
            var L1 = AddGameObjectChild("L1", m_Root, true, -5);

            // no label, l1 parent
            var L1U1 = AddGameObjectChild("L1U1", L1, false, -4);

            // no label, l1u1 parent
            var L1U1U1 = AddGameObjectChild("L1U1", L1U1, false, -3);

            // no label, l1u1u1 parent
            var L1U1U1U1 = AddGameObjectChild("L1U1U1", L1U1U1, false, -2);

            // no label, l1u1u1u1 parent
            var L1U1U1U1U1 = AddGameObjectChild("L1U1U1U1", L1U1U1U1, false, -1);

            // no label, l1u1u1u1u1 parent
            var L1U1U1U1U1U1 = AddGameObjectChild("L1U1U1U1U1", L1U1U1U1U1, false, 0);

            // has label, l1u1u1u1u1u1 parent
            var L1U1U1U1U1U1L1 = AddGameObjectChild("L1U1U1U1U1U1L1", L1U1U1U1U1U1, true, 1);

            /*  Expected Output Hierarchy
             *   - L1
             *     - L1U1U1U1U1U1L1
             */

            m_PerceptionCamera.RenderedObjectInfosCalculated += (_, _, hierarchy) =>
            {
                //// Hierarchy Items Verification
                // Verify things in hierarchy
                hierarchy.AssertInHierarchy(L1);
                hierarchy.AssertInHierarchy(L1U1U1U1U1U1L1);
                Assert.AreEqual(2, hierarchy.hierarchy.Count);

                // Verify things not in hierarchy
                hierarchy.AssertNotInHierarchy(L1U1);
                hierarchy.AssertNotInHierarchy(L1U1U1);
                hierarchy.AssertNotInHierarchy(L1U1U1U1);
                hierarchy.AssertNotInHierarchy(L1U1U1U1U1);
                hierarchy.AssertNotInHierarchy(L1U1U1U1U1U1);

                //// Hierarchy Parent Verification
                hierarchy.AssertNoParent(L1);
                hierarchy.AssertHasParent(L1U1U1U1U1U1L1, L1);

                //// Hierarchy Children Verification
                hierarchy.AssertHasChildren(L1, L1U1U1U1U1U1L1);
                hierarchy.AssertNoChildren(L1U1U1U1U1U1L1);

                Debug.Log("Hierarchy tests complete.");
            };

            m_PerceptionCamera.enabled = true;
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisabledObjectsNotInHierarchy()
        {
            // has label, no parent
            var L1 = AddGameObjectChild("L1", m_Root, true);

            // has label, l1 parent
            var L1L1 = AddGameObjectChild("L1L1", L1, true, 5);

            // has label, l1 parent
            var L1L2_D = AddGameObjectChild("L1L2_D", L1, true, -5, prep: (x) => x.SetActive(false));
            var L1L3 = AddGameObjectChild("L1L3", L1, true, 0, 3);

            /*  Expected Output Hierarchy
             *   - L1
             *     - L1L1
             *     - L1L3
             */

            m_PerceptionCamera.RenderedObjectInfosCalculated += (_, _, hierarchy) =>
            {
                //// Hierarchy Items Verification
                // Verify things in hierarchy
                hierarchy.AssertInHierarchy(L1);
                hierarchy.AssertInHierarchy(L1L1);
                hierarchy.AssertInHierarchy(L1L3);
                Assert.AreEqual(3, hierarchy.hierarchy.Count);

                // Verify things not in hierarchy
                hierarchy.AssertNotInHierarchy(L1L2_D);

                //// Hierarchy Parent Verification
                hierarchy.AssertNoParent(L1);
                hierarchy.AssertHasParent(L1L1, L1);
                hierarchy.AssertHasParent(L1L3, L1);

                //// Hierarchy Children Verification
                hierarchy.AssertHasChildren(L1, L1L1, L1L3);
                hierarchy.AssertNoChildren(L1L1);
                hierarchy.AssertNoChildren(L1L3);

                Debug.Log("Hierarchy tests complete.");
            };

            m_PerceptionCamera.enabled = true;
            yield return null;
        }

        [UnityTest]
        public IEnumerator OffScreenObjectsNotInHierarchy()
        {
            m_PerceptionCamera.enabled = false;

            // has label, no parent
            var L1 = AddGameObjectChild("L1", m_Root, true, -3);

            // has label, l1 parent
            var L1L1 = AddGameObjectChild("L1L1", L1, true, -2);

            // has label, l1 parent
            var L1L2_OffScreen = AddGameObjectChild("L1L2_OffScreen", L1, true,
                -10000, -10000, -10000
            );
            var L1L3 = AddGameObjectChild("L1L3", L1, true, -1);

            /*  Expected Output Hierarchy
             *   - L1
             *     - L1L1
             *     - L1L3
             */

            m_PerceptionCamera.RenderedObjectInfosCalculated += (_, _, hierarchy) =>
            {
                //// Hierarchy Items Verification
                // Verify things in hierarchy
                hierarchy.AssertInHierarchy(L1);
                hierarchy.AssertInHierarchy(L1L1);
                hierarchy.AssertInHierarchy(L1L3);
                Assert.AreEqual(3, hierarchy.hierarchy.Count);

                // Verify things not in hierarchy
                hierarchy.AssertNotInHierarchy(L1L2_OffScreen);

                //// Hierarchy Parent Verification
                hierarchy.AssertNoParent(L1);
                hierarchy.AssertHasParent(L1L1, L1);
                hierarchy.AssertHasParent(L1L3, L1);

                //// Hierarchy Children Verification
                hierarchy.AssertHasChildren(L1, L1L1, L1L3);
                hierarchy.AssertNoChildren(L1L1);
                hierarchy.AssertNoChildren(L1L3);

                Debug.Log("Hierarchy tests complete.");
            };

            m_PerceptionCamera.enabled = true;
            yield return null;
        }
    }
}
