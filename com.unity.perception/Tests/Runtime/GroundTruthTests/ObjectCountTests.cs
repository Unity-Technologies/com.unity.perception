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
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if HDRP_PRESENT
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
#endif

namespace GroundTruthTests
{
    //Graphics issues with OpenGL Linux Editor. https://jira.unity3d.com/browse/AISV-422
    [UnityPlatform(exclude = new[] {RuntimePlatform.LinuxEditor, RuntimePlatform.LinuxPlayer})]
    [TestFixture]
    class ObjectCountTests : GroundTruthTestBase
    {
        [UnityTest]
        public IEnumerator ProducesCorrectValuesWithChangingObjects()
        {
            var label = "label";
            var labelingConfiguration = ScriptableObject.CreateInstance<LabelingConfiguration>();

            labelingConfiguration.LabelEntries = new List<LabelEntry>
            {
                new LabelEntry
                {
                    id = 1,
                    label = label,
                    value = 500
                }
            };

            var receivedResults = new List<(uint[] counts, LabelEntry[] labels, int frameCount)>();

            var cameraObject = SetupCamera(labelingConfiguration, (counts, labels, frameCount) =>
            {
                receivedResults.Add((counts.ToArray(), labels.ToArray(), frameCount));
            });
            AddTestObjectForCleanup(cameraObject);

            //TestHelper.LoadAndStartRenderDocCapture(out EditorWindow gameView);
            var startFrameCount = Time.frameCount;
            var expectedFramesAndCounts = new Dictionary<int, int>()
            {
                {Time.frameCount    , 0},
                {startFrameCount + 1, 1},
                {startFrameCount + 2, 1},
                {startFrameCount + 3, 2},
                {startFrameCount + 4, 1},
                {startFrameCount + 5, 1},
            };

            yield return null;
            //Put a plane in front of the camera
            var planeObject = TestHelper.CreateLabeledPlane(.1f, label);
            yield return null;
            Object.DestroyImmediate(planeObject);
            planeObject = TestHelper.CreateLabeledPlane(.1f, label);
            yield return null;
            var planeObject2 = TestHelper.CreateLabeledPlane(.1f, label);
            planeObject2.transform.Translate(.5f, 0, 0);

            yield return null;
            Object.DestroyImmediate(planeObject);
            yield return null;
            yield return null;

            Object.DestroyImmediate(planeObject2);
#if HDRP_PRESENT
            //TODO: Remove this when DestroyImmediate properly calls Cleanup on the pass
            var labelHistogramPass = (ObjectCountPass)cameraObject.GetComponent<CustomPassVolume>().customPasses.First(p => p is ObjectCountPass);
            labelHistogramPass.WaitForAllRequests();
#endif
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);

            //RenderDoc.EndCaptureRenderDoc(gameView);

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

        static GameObject SetupCamera(LabelingConfiguration labelingConfiguration,
            Action<NativeSlice<uint>, IReadOnlyList<LabelEntry>, int> onClassCountsReceived)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;

#if HDRP_PRESENT
            cameraObject.AddComponent<HDAdditionalCameraData>();
            var customPassVolume = cameraObject.AddComponent<CustomPassVolume>();
            customPassVolume.isGlobal = true;
            var rt = new RenderTexture(128, 128, 1, GraphicsFormat.R8G8B8A8_UNorm);
            rt.Create();
            var instanceSegmentationPass = new InstanceSegmentationPass()
            {
                targetCamera = camera,
                targetTexture = rt
            };
            instanceSegmentationPass.name = nameof(instanceSegmentationPass);
            instanceSegmentationPass.EnsureInit();
            customPassVolume.customPasses.Add(instanceSegmentationPass);
            var objectCountPass = new ObjectCountPass(camera);
            objectCountPass.SegmentationTexture = rt;
            objectCountPass.LabelingConfiguration = labelingConfiguration;
            objectCountPass.name = nameof(objectCountPass);
            customPassVolume.customPasses.Add(objectCountPass);

            objectCountPass.ClassCountsReceived += onClassCountsReceived;
#endif
#if URP_PRESENT
            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.LabelingConfiguration = labelingConfiguration;
            perceptionCamera.captureRgbImages = false;
            perceptionCamera.produceBoundingBoxAnnotations = false;
            perceptionCamera.produceObjectCountAnnotations = true;
            perceptionCamera.classCountsReceived += onClassCountsReceived;
#endif
            cameraObject.SetActive(true);
            return cameraObject;
        }
    }
}
