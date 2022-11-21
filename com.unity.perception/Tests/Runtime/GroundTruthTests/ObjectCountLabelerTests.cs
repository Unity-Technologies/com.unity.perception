using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
#if UNITY_EDITOR
#endif
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    //Graphics issues with OpenGL Linux Editor. https://jira.unity3d.com/browse/AISV-422
    [UnityPlatform(exclude = new[] {RuntimePlatform.LinuxEditor, RuntimePlatform.LinuxPlayer})]
    [TestFixture]
    class ObjectCountLabelerTests : GroundTruthTestBase
    {
        [Test]
        public void NullLabelingConfiguration_ProducesInvalidOperationException()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentNullException>(() => new ObjectCountLabeler(null));
        }

        [UnityTest]
        public IEnumerator ProducesCorrectValuesWithChangingObjects()
        {
            TearDown();

            var label = "label";
            var labelingConfiguration = ScriptableObject.CreateInstance<IdLabelConfig>();

            labelingConfiguration.Init(new List<IdLabelEntry>
            {
                new IdLabelEntry
                {
                    id = 1,
                    label = label
                }
            });

            var receivedResults = new List<(uint[] counts, IdLabelEntry[] labels, int frameCount)>();
            var cameraObject = SetupCamera(labelingConfiguration, (frameCount, counts, labels) =>
            {
                receivedResults.Add((counts.ToArray(), labels.ToArray(), frameCount));
            });
            AddTestObjectForCleanup(cameraObject);

            var startFrameCount = Time.frameCount;
            var expectedFramesAndCounts = new Dictionary<int, int>()
            {
                {startFrameCount    , 0},
                {startFrameCount + 1, 1},
                {startFrameCount + 2, 1},
                {startFrameCount + 3, 2},
                {startFrameCount + 4, 1},
                {startFrameCount + 5, 1},
            };

            // Frame: 0 | Nothing on camera
            yield return null;
            //Put a plane in front of the camera
            var planeObject = TestHelper.CreateLabeledPlane(.1f, label);
            // Frame: 1 | 1 Plane on camera
            yield return null;
            Object.DestroyImmediate(planeObject);
            planeObject = TestHelper.CreateLabeledPlane(.1f, label);
            // Frame: 2 | 1 Plane on camera
            yield return null;
            var planeObject2 = TestHelper.CreateLabeledPlane(.1f, label);
            planeObject2.transform.Translate(.5f, 0, 0.1f);
            // Frame: 3 | 2 Planes on camera
            yield return null;
            Object.DestroyImmediate(planeObject);
            // Frame: 4 | 1 Plane on camera
            yield return null;
            // Frame: 5 | 1 Plane on camera
            yield return null;
            Object.DestroyImmediate(planeObject2);
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);

            yield return null;
            yield return null;

            foreach (var result in receivedResults)
            {
                Assert.AreEqual(1, result.counts.Length);
                Assert.AreEqual(1, result.labels.Length);
                Assert.Contains(result.frameCount, expectedFramesAndCounts.Keys, "Received event with unexpected frameCount.");

                var expectedCount = expectedFramesAndCounts[result.frameCount];

                var errorString = $"Wrong count in frame {result.frameCount - startFrameCount}. {string.Join(", ", receivedResults.Select(r => $"count: {r.counts[0]}"))}";
                Assert.AreEqual(expectedCount, result.counts[0], errorString);

                expectedFramesAndCounts.Remove(result.frameCount);
            }

            CollectionAssert.IsEmpty(expectedFramesAndCounts);
        }

        static GameObject SetupCamera(IdLabelConfig idLabelConfig,
            Action<int, NativeSlice<uint>, IReadOnlyList<IdLabelEntry>> onClassCountsReceived)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            var classCountLabeler = new ObjectCountLabeler(idLabelConfig);
            if (onClassCountsReceived != null)
                classCountLabeler.ObjectCountsComputed += onClassCountsReceived;

            perceptionCamera.AddLabeler(classCountLabeler);
            cameraObject.SetActive(true);

            return cameraObject;
        }
    }
}
