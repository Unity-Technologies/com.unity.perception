using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    [TestFixture]
    public class BoundingBox2dTests
    {
        struct BoundingBoxTestCallback
        {
            public BoundingBox center;
            public BoundingBox top;
            public BoundingBox right;
            public BoundingBox bottom;
            public BoundingBox left;
        }

        GameObject m_CameraAndScenario;
        PerceptionCamera m_PerceptionCamera;

        GameObject m_Center; // at the center
        int centerInstanceId => (int)m_Center.GetComponent<Labeling>().instanceId;
        const string k_CenterLabel = "Center";

        GameObject m_Top; // above human
        int topInstanceId => (int)m_Top.GetComponent<Labeling>().instanceId;
        const string k_TopLabel = "Top";

        GameObject m_Right; // to the right of human
        int rightInstanceId => (int)m_Right.GetComponent<Labeling>().instanceId;
        const string k_RightLabel = "Right";

        GameObject m_Left; // to the right of human
        int leftInstanceId => (int)m_Left.GetComponent<Labeling>().instanceId;
        const string k_LeftLabel = "Left";

        GameObject m_Bottom; // below human
        int bottomInstanceId => (int)m_Bottom.GetComponent<Labeling>().instanceId;
        const string k_BottomLabel = "Bottom";
        const float k_Delta = 2.0f;

        IdLabelConfig m_LastConfig;

        /// <summary>
        /// Creates a scene with the above five objects and an orthographic perception camera.
        /// </summary>
        [UnitySetUp]
        public IEnumerator Setup()
        {
            DatasetCapture.OverrideEndpoint(new NoOutputEndpoint());

            m_Center = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_Center.name = nameof(m_Center);
            var humanPos = m_Center.transform.position;
            m_Center.AddComponent<Labeling>().labels = new List<string>() { k_CenterLabel };

            // each object is a 1x1 cube placed either above, below, to the left, or the right
            // of the center object with a corresponding label "Top", "Left", "Right", "Bottom"
            m_Top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_Top.transform.position = humanPos + (Vector3.up);
            m_Top.name = nameof(m_Top);
            m_Top.transform.parent = m_Center.transform;
            m_Top.AddComponent<Labeling>().labels = new List<string>() { k_TopLabel };

            m_Right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_Right.transform.position = humanPos + (Vector3.right);
            m_Right.name = nameof(m_Right);
            m_Right.transform.parent = m_Center.transform;
            m_Right.AddComponent<Labeling>().labels = new List<string>() { k_RightLabel };

            m_Left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_Left.transform.position = humanPos + (Vector3.left);
            m_Left.name = nameof(m_Left);
            m_Left.transform.parent = m_Center.transform;
            m_Left.AddComponent<Labeling>().labels = new List<string>() { k_LeftLabel };

            m_Bottom = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_Bottom.transform.position = humanPos + (Vector3.down);
            m_Bottom.name = nameof(m_Bottom);
            m_Bottom.transform.parent = m_Center.transform;
            m_Bottom.AddComponent<Labeling>().labels = new List<string>() { k_BottomLabel };

            m_CameraAndScenario = new GameObject("Cam & Scenario");
            var camera = m_CameraAndScenario.AddComponent<Camera>();
            // we use a orthographic camera so we can test bounding box sizes without
            // distortion of perspective
            camera.orthographic = true;
            camera.orthographicSize = 5;

            m_PerceptionCamera = m_CameraAndScenario.AddComponent<PerceptionCamera>();
            m_PerceptionCamera.transform.position = new Vector3(0, 0, -4);
            m_PerceptionCamera.firstCaptureFrame = 2;
            m_PerceptionCamera.framesBetweenCaptures = 1;
            m_PerceptionCamera.enabled = false;

            yield return null;
        }

        /// <summary>
        /// Cleans up all assets set up in <see cref="Setup" />
        /// </summary>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.DestroyImmediate(m_CameraAndScenario);

            Object.DestroyImmediate(m_Center);
            Object.DestroyImmediate(m_Top);
            Object.DestroyImmediate(m_Right);
            Object.DestroyImmediate(m_Left);
            Object.DestroyImmediate(m_Bottom);

            Object.DestroyImmediate(m_LastConfig);

            DatasetCapture.ResetSimulation();
            yield return null;
        }

        /// <summary>
        /// Checks whether the X and Y components of two vectors are equal within a certain delta
        /// </summary>
        static void AssertVector2Equal(Vector2 expected, Vector2 actual, float delta, string testDescription)
        {
            Assert.AreEqual(expected.x, actual.x, delta,
                $"X components are not within given delta. [{testDescription}]");
            Assert.AreEqual(expected.y, actual.y, delta,
                $"Y components are not within given delta. [{testDescription}]");
        }

        /// <summary>
        /// Executes a test for bounding boxes. Sets up the IdLabelConfig based on the specified list of
        /// label name, hierarchy relation pairs, and invokes the given assertion callback when it receives complete
        /// data from <see cref="BoundingBox2DLabeler.boundingBoxesCalculated" />.
        /// </summary>
        /// <remarks>
        /// Ensures we have received a callback by yielding frames until we do.
        /// </remarks>
        IEnumerator ExecuteBoundingBox2dTest(
            IEnumerable<(string label, HierarchyRelation relation)> labelConfig,
            Action<BoundingBoxTestCallback> assertionCallback
        )
        {
            if (m_LastConfig != null)
                Object.DestroyImmediate(m_LastConfig);

            m_LastConfig = ScriptableObject.CreateInstance<IdLabelConfig>();
            m_LastConfig.name = nameof(BoundingBox2dTests) + "LabelConfig";
            var labelId = 1;
            m_LastConfig.Init(labelConfig.Select(item => new IdLabelEntry()
            {
                label = item.label,
                hierarchyRelation = item.relation,
                id = ++labelId
            }));

            var boundingBoxLabeler = new BoundingBox2DLabeler(m_LastConfig);
            m_PerceptionCamera.AddLabeler(boundingBoxLabeler);
#if UNITY_EDITOR
            m_PerceptionCamera.showVisualizations = true;
#endif
            m_PerceptionCamera.enabled = true;

            var callbackReceived = true;
            if (assertionCallback != null)
            {
                callbackReceived = false;
                boundingBoxLabeler.boundingBoxesCalculated += (boxes) =>
                {
                    if (callbackReceived)
                        return;

                    callbackReceived = true;
                    var callbackValue = new BoundingBoxTestCallback()
                    {
                        center = boxes.data.First(box => box.instanceId == centerInstanceId),
                        top = boxes.data.First(box => box.instanceId == topInstanceId),
                        right = boxes.data.First(box => box.instanceId == rightInstanceId),
                        bottom = boxes.data.First(box => box.instanceId == bottomInstanceId),
                        left = boxes.data.First(box => box.instanceId == leftInstanceId),
                    };
                    assertionCallback?.Invoke(callbackValue);
                    Debug.Log("Test complete.");
                };
            }

            for (int i = 0; i < 100 && !callbackReceived; i++)
                yield return null;
        }

        /// <summary>
        /// A list of test-cases for <see cref="SelectedSides_MergedIntoCenter_BoundingBoxCorrect" />
        /// Each test case is a tuple with:
        ///   1. a string name for identification during test failure
        ///   2. a list of (string, hierarchyRelation) tuples which will essentially be the IdLabelConfig
        ///   3. a functor that computes the size of the center bounding box
        /// </summary>
        static List<(string, List<(string, HierarchyRelation)>, Func<(Vector2 left, Vector2 top, Vector2 right, Vector2 bottom), Vector2>)> s_SideTests = new()
        {
            (
                "No merging. Center remains at 1x size",
                new()
                {
                    (k_CenterLabel, HierarchyRelation.Independent),
                    (k_TopLabel, HierarchyRelation.Independent),
                    (k_BottomLabel, HierarchyRelation.Independent),
                    (k_RightLabel, HierarchyRelation.Independent),
                    (k_LeftLabel, HierarchyRelation.Independent),
                },
                (corners) => corners.left
            ),
            (
                "Bottom object merged in with center, center will be 2x in height.",
                new() {
                    (k_CenterLabel, HierarchyRelation.Independent),
                    (k_TopLabel, HierarchyRelation.Independent),
                    (k_BottomLabel, HierarchyRelation.AddToParent),
                    (k_RightLabel, HierarchyRelation.Independent),
                    (k_LeftLabel, HierarchyRelation.Independent)
                },
                (corners) => new Vector2(corners.left.x, 2 * corners.left.y)
            ),
            (
                "Top object merged in with center, center will be 2x in height.",
                new() {
                    (k_CenterLabel, HierarchyRelation.Independent),
                    (k_TopLabel, HierarchyRelation.AddToParent),
                    (k_BottomLabel, HierarchyRelation.Independent),
                    (k_RightLabel, HierarchyRelation.Independent),
                    (k_LeftLabel, HierarchyRelation.Independent)
                },
                (corners) => new Vector2(corners.left.x, 2 * corners.left.y)
            ),
            (
                "Left object merged in with center, center will be 2x in width.",
                new() {
                    (k_CenterLabel, HierarchyRelation.Independent),
                    (k_TopLabel, HierarchyRelation.Independent),
                    (k_BottomLabel, HierarchyRelation.Independent),
                    (k_RightLabel, HierarchyRelation.Independent),
                    (k_LeftLabel, HierarchyRelation.AddToParent)
                },
                (corners) => new Vector2(2 * corners.right.x, corners.right.y)
            ),
            (
                "Right object merged in with center, center will be 2x in width.",
                new() {
                    (k_CenterLabel, HierarchyRelation.Independent),
                    (k_TopLabel, HierarchyRelation.Independent),
                    (k_BottomLabel, HierarchyRelation.Independent),
                    (k_RightLabel, HierarchyRelation.AddToParent),
                    (k_LeftLabel, HierarchyRelation.Independent)
                },
                (corners) => new Vector2(2 * corners.left.x, corners.left.y)
            ),
            (
                "Top, Right, Bottom, Left objects merged, center will be 3x in width and height.",
                new()
                {
                    (k_CenterLabel, HierarchyRelation.Independent),
                    (k_TopLabel, HierarchyRelation.AddToParent),
                    (k_BottomLabel, HierarchyRelation.AddToParent),
                    (k_RightLabel, HierarchyRelation.AddToParent),
                    (k_LeftLabel, HierarchyRelation.AddToParent),
                },
                (corners) => 3 * corners.left
            ),
            (
                "Left, Right objects merged, center will be 3x in width.",
                new()
                {
                    (k_CenterLabel, HierarchyRelation.Independent),
                    (k_TopLabel, HierarchyRelation.Independent),
                    (k_BottomLabel, HierarchyRelation.Independent),
                    (k_RightLabel, HierarchyRelation.AddToParent),
                    (k_LeftLabel, HierarchyRelation.AddToParent),
                },
                (corners) => new Vector2(3 * corners.top.x, corners.top.y)
            ),
            (
                "Top, Bottom objects merged, center will be 3x in height.",
                new()
                {
                    (k_CenterLabel, HierarchyRelation.Independent),
                    (k_TopLabel, HierarchyRelation.AddToParent),
                    (k_BottomLabel, HierarchyRelation.AddToParent),
                    (k_RightLabel, HierarchyRelation.Independent),
                    (k_LeftLabel, HierarchyRelation.Independent),
                },
                (corners) => new Vector2(corners.left.x, 3 * corners.left.y)
            ),
            (
                "Top, Left objects merged, center will be 2x in width and 2x in height.",
                new()
                {
                    (k_CenterLabel, HierarchyRelation.Independent),
                    (k_TopLabel, HierarchyRelation.AddToParent),
                    (k_BottomLabel, HierarchyRelation.Independent),
                    (k_RightLabel, HierarchyRelation.Independent),
                    (k_LeftLabel, HierarchyRelation.AddToParent),
                },
                (corners) => 2 * corners.bottom
            ),
            (
                "Bottom, Right objects merged, center will be 2x in width and 2x in height.",
                new()
                {
                    (k_CenterLabel, HierarchyRelation.Independent),
                    (k_TopLabel, HierarchyRelation.Independent),
                    (k_BottomLabel, HierarchyRelation.AddToParent),
                    (k_RightLabel, HierarchyRelation.AddToParent),
                    (k_LeftLabel, HierarchyRelation.Independent),
                },
                (corners) => 2 * corners.left
            )
        };

        [UnityTest]
        public IEnumerator SelectedSides_MergedIntoCenter_BoundingBoxCorrect(
            [ValueSource(nameof(s_SideTests))]
                (string description, List<(string, HierarchyRelation)> labelConfig, Func<(Vector2 left, Vector2 top, Vector2 right, Vector2 bottom), Vector2> centerSizeFunctor) sideTest
        )
        {
            yield return ExecuteBoundingBox2dTest(
                sideTest.labelConfig,
                boxes =>
                {
                    // in all tests, the four corners will not enlarge and will be the same size
                    AssertVector2Equal(boxes.top.dimension, boxes.right.dimension, k_Delta, sideTest.description);
                    AssertVector2Equal(boxes.right.dimension, boxes.bottom.dimension, k_Delta, sideTest.description);
                    AssertVector2Equal(boxes.bottom.dimension, boxes.left.dimension, k_Delta, sideTest.description);

                    // the size of the center bounding box is a function of which combination of
                    // corner objects we want to merge in
                    AssertVector2Equal(
                        sideTest.centerSizeFunctor(
                            (boxes.left.dimension, boxes.top.dimension, boxes.right.dimension, boxes.bottom.dimension)
                            ),
                        boxes.center.dimension, k_Delta,
                        sideTest.description
                    );
                });
        }
    }
}
