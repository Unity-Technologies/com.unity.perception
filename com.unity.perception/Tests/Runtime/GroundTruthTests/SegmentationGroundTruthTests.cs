using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Rendering;

#if HDRP_PRESENT
    using UnityEngine.Rendering.HighDefinition;
#elif URP_PRESENT
    using UnityEngine.Rendering.Universal;
#endif

using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    public class ImageReaderBehaviour : MonoBehaviour
    {
        public RenderTexture source;
        public Camera cameraSource;
        RenderTextureReader<Color32> m_Reader;

        public event Action<int, NativeArray<Color32>> SegmentationImageReceived;

        void Awake()
        {
            m_Reader = new RenderTextureReader<Color32>(source, cameraSource, ImageReadCallback);
        }

        void ImageReadCallback(int frameCount, NativeArray<Color32> data, RenderTexture renderTexture)
        {
            if (SegmentationImageReceived != null)
                SegmentationImageReceived(frameCount, data);
        }

        void OnDestroy()
        {
            m_Reader.Dispose();
            m_Reader = null;
        }
    }

    public enum RendererType
    {
        MeshRenderer,
        SkinnedMeshRenderer,
        Terrain
    }

    //Graphics issues with OpenGL Linux Editor. https://jira.unity3d.com/browse/AISV-422
    [UnityPlatform(exclude = new[] {RuntimePlatform.LinuxEditor, RuntimePlatform.LinuxPlayer})]
    public class SegmentationPassTests : GroundTruthTestBase
    {
        static readonly Color32 k_SemanticPixelValue = new Color32(10, 20, 30, Byte.MaxValue);

        public enum SegmentationKind
        {
            Instance,
            Semantic
        }
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator SegmentationPassTestsWithEnumeratorPasses(
            [Values(RendererType.MeshRenderer, RendererType.SkinnedMeshRenderer, RendererType.Terrain)] RendererType rendererType,
            [Values(SegmentationKind.Instance, SegmentationKind.Semantic)] SegmentationKind segmentationKind)
        {
            int timesSegmentationImageReceived = 0;
            int? frameStart = null;
            GameObject cameraObject = null;

            object expectedPixelValue;
            void OnSegmentationImageReceived<T>(int frameCount, NativeArray<T> data, RenderTexture tex) where T : struct
            {
                if (frameStart == null || frameStart > frameCount) return;

                timesSegmentationImageReceived++;
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, data.Length), data);
            }

            switch (segmentationKind)
            {
                case SegmentationKind.Instance:
                    //expectedPixelValue = new Color32(0, 74, 255, 255);
                    expectedPixelValue = new Color32(255,0,0, 255);
                    cameraObject = SetupCameraInstanceSegmentation(OnSegmentationImageReceived);
                    break;
                case SegmentationKind.Semantic:
                    expectedPixelValue = k_SemanticPixelValue;
                    cameraObject = SetupCameraSemanticSegmentation(a => OnSegmentationImageReceived(a.frameCount, a.data, a.sourceTexture), false);
                    break;
            }

            //Put a plane in front of the camera
            GameObject planeObject;
            if (rendererType == RendererType.Terrain)
            {
                var terrainData = new TerrainData();
                AddTestObjectForCleanup(terrainData);
                //look down because terrains cannot be rotated
                cameraObject.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                planeObject = Terrain.CreateTerrainGameObject(terrainData);
                planeObject.transform.SetPositionAndRotation(new Vector3(-10, -10, -10), Quaternion.identity);
            }
            else
            {
                planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                if (rendererType == RendererType.SkinnedMeshRenderer)
                {
                    var oldObject = planeObject;
                    planeObject = new GameObject();

                    var meshFilter = oldObject.GetComponent<MeshFilter>();
                    var meshRenderer = oldObject.GetComponent<MeshRenderer>();
                    var skinnedMeshRenderer = planeObject.AddComponent<SkinnedMeshRenderer>();
                    skinnedMeshRenderer.sharedMesh = meshFilter.sharedMesh;
                    skinnedMeshRenderer.material = meshRenderer.material;

                    Object.DestroyImmediate(oldObject);
                }
                planeObject.transform.SetPositionAndRotation(new Vector3(0, 0, 10), Quaternion.Euler(90, 0, 0));
                planeObject.transform.localScale = new Vector3(10, -1, 10);
            }
            var labeling = planeObject.AddComponent<Labeling>();
            labeling.labels.Add("label");

            frameStart = Time.frameCount;

            AddTestObjectForCleanup(planeObject);

            yield return null;
            yield return null;
            yield return null;
            yield return null;
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            DestroyTestObject(planeObject);

            Assert.AreEqual(4, timesSegmentationImageReceived);
        }

        // Lens Distortion is only applicable in URP or HDRP pipelines
        // As such, this test will always fail if URP or HDRP are not present (and also not really compile either)
#if HDRP_PRESENT || URP_PRESENT
        [UnityTest]
        public IEnumerator SemanticSegmentationPass_WithLensDistortion()
        {
            GameObject cameraObject = null;
            PerceptionCamera perceptionCamera;
            var fLensDistortionEnabled = false;
            var fDone = false;
            var frames = 0;
#if false
            var dataBBox = new Color32[]
            {
                Color.blue, Color.blue,
                Color.blue, Color.blue
            };
#endif

            var boundingBoxWithoutLensDistortion = new Rect();
            var boundingBoxWithLensDistortion = new Rect();

            void OnSegmentationImageReceived(int frameCount, NativeArray<Color32> data, RenderTexture tex)
            {
                frames++;

                if (frames < 10)
                    return;

                // Calculate the bounding box
                if (fLensDistortionEnabled == false)
                {
                    fLensDistortionEnabled = true;

                    var renderedObjectInfoGenerator = new RenderedObjectInfoGenerator();
                    renderedObjectInfoGenerator.Compute(data, tex.width, BoundingBoxOrigin.TopLeft, out var boundingBoxes, Allocator.Temp);

                    boundingBoxWithoutLensDistortion = boundingBoxes[0].boundingBox;

                    // Add lens distortion
                    perceptionCamera.OverrideLensDistortionIntensity(0.715f);

                    frames = 0;
                }
                else
                {
                    var renderedObjectInfoGenerator = new RenderedObjectInfoGenerator();

                    renderedObjectInfoGenerator.Compute(data, tex.width, BoundingBoxOrigin.TopLeft, out var boundingBoxes, Allocator.Temp);

                    boundingBoxWithLensDistortion = boundingBoxes[0].boundingBox;

                    Assert.AreNotEqual(boundingBoxWithoutLensDistortion, boundingBoxWithLensDistortion);
                    Assert.Greater(boundingBoxWithLensDistortion.width, boundingBoxWithoutLensDistortion.width);

                    fDone = true;
                }
            }

            cameraObject = SetupCamera(out perceptionCamera, false);
            perceptionCamera.InstanceSegmentationImageReadback += OnSegmentationImageReceived;
            cameraObject.SetActive(true);

            // Put a plane in front of the camera
            var planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);

            planeObject.transform.SetPositionAndRotation(new Vector3(0, 0, 10), Quaternion.Euler(90, 0, 0));
            planeObject.transform.localScale = new Vector3(0.1f, -1, 0.1f);
            var labeling = planeObject.AddComponent<Labeling>();
            labeling.labels.Add("label");

            AddTestObjectForCleanup(planeObject);

            perceptionCamera.OverrideLensDistortionIntensity(0.5f);

            while (fDone != true)
            {
                yield return null;
            }

            // Destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            DestroyTestObject(planeObject);
        }
#endif // ! HDRP_PRESENT || URP_PRESENT

        [UnityTest]
        public IEnumerator SemanticSegmentationPass_WithLabeledButNotMatchingObject_ProducesBlack()
        {
            int timesSegmentationImageReceived = 0;
            var expectedPixelValue = new Color32(0, 0, 0, 255);
            void OnSegmentationImageReceived(NativeArray<Color32> data)
            {
                timesSegmentationImageReceived++;
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, data.Length), data);
            }

            var cameraObject = SetupCameraSemanticSegmentation(a => OnSegmentationImageReceived(a.data), false);

            AddTestObjectForCleanup(TestHelper.CreateLabeledPlane(label: "non-matching"));
            yield return null;
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            Assert.AreEqual(1, timesSegmentationImageReceived);
        }

        [UnityTest]
        public IEnumerator SemanticSegmentationPass_WithEmptyFrame_ProducesBlack([Values(false, true)] bool showVisualizations)
        {
            int timesSegmentationImageReceived = 0;
            var expectedPixelValue = new Color32(0, 0, 0, 255);
            void OnSegmentationImageReceived(NativeArray<Color32> data)
            {
                timesSegmentationImageReceived++;
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, data.Length), data);
            }

            var cameraObject = SetupCameraSemanticSegmentation(a => OnSegmentationImageReceived(a.data), showVisualizations);

            //TestHelper.LoadAndStartRenderDocCapture(out var gameView);
            yield return null;
            var segLabeler = (SemanticSegmentationLabeler)cameraObject.GetComponent<PerceptionCamera>().labelers[0];
            var request = AsyncGPUReadback.Request(segLabeler.targetTexture, callback: r =>
            {
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, segLabeler.targetTexture.width * segLabeler.targetTexture.height), r.GetData<Color32>());
            });
            AsyncGPUReadback.WaitAllRequests();

            //RenderDoc.EndCaptureRenderDoc(gameView);

            //request.WaitForCompletion();
            Assert.IsTrue(request.done);
            Assert.IsFalse(request.hasError);

            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            Assert.AreEqual(1, timesSegmentationImageReceived);
        }

        [UnityTest]
        public IEnumerator SemanticSegmentationPass_WithTextureOverride_RendersToOverride([Values(true, false)] bool showVisualizations)
        {
            var expectedPixelValue = new Color32(0, 0, 255, 255);
            var targetTextureOverride = new RenderTexture(2, 2, 1, RenderTextureFormat.R8);

            var cameraObject = SetupCamera(out var perceptionCamera, showVisualizations);
            var labelConfig = ScriptableObject.CreateInstance<SemanticSegmentationLabelConfig>();
            labelConfig.Init(new List<SemanticSegmentationLabelEntry>()
            {
                new SemanticSegmentationLabelEntry()
                {
                    label = "label",
                    color = expectedPixelValue
                }
            });
            var semanticSegmentationLabeler = new SemanticSegmentationLabeler(labelConfig, targetTextureOverride);
            perceptionCamera.AddLabeler(semanticSegmentationLabeler);
            cameraObject.SetActive(true);
            AddTestObjectForCleanup(cameraObject);
            AddTestObjectForCleanup(TestHelper.CreateLabeledPlane());

            yield return null;
            TestHelper.ReadRenderTextureRawData<Color32>(targetTextureOverride, data =>
            {
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, targetTextureOverride.width * targetTextureOverride.height), data);
            });
        }


        [UnityTest]
        public IEnumerator SemanticSegmentationPass_WithMultiMaterial_ProducesCorrectValues([Values(true, false)] bool showVisualizations)
        {
            int timesSegmentationImageReceived = 0;
            var expectedPixelValue = k_SemanticPixelValue;
            void OnSegmentationImageReceived(NativeArray<Color32> data)
            {
                timesSegmentationImageReceived++;
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, data.Length), data);
            }

            var cameraObject = SetupCameraSemanticSegmentation(a => OnSegmentationImageReceived(a.data), false);

            var plane = TestHelper.CreateLabeledPlane();
            var meshRenderer = plane.GetComponent<MeshRenderer>();
            var baseMaterial = meshRenderer.material;
            meshRenderer.materials = new[] { baseMaterial, baseMaterial };
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetFloat("float", 1f);
            for (int i = 0; i < 2; i++)
            {
                meshRenderer.SetPropertyBlock(mpb, i);
            }
            AddTestObjectForCleanup(plane);
            yield return null;
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            Assert.AreEqual(1, timesSegmentationImageReceived);
        }


        [UnityTest]
        public IEnumerator SemanticSegmentationPass_WithChangingLabeling_ProducesCorrectValues([Values(true, false)] bool showVisualizations)
        {
            int timesSegmentationImageReceived = 0;
            var expectedPixelValue = k_SemanticPixelValue;
            void OnSegmentationImageReceived(NativeArray<Color32> data)
            {
                if (timesSegmentationImageReceived == 1)
                {
                    CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, data.Length), data);
                }
                timesSegmentationImageReceived++;
            }

            var cameraObject = SetupCameraSemanticSegmentation(a => OnSegmentationImageReceived(a.data), false);

            var plane = TestHelper.CreateLabeledPlane(label: "non-matching");
            AddTestObjectForCleanup(plane);
            yield return null;
            var labeling = plane.GetComponent<Labeling>();
            labeling.labels = new List<string> { "label" };
            labeling.RefreshLabeling();
            yield return null;
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            Assert.AreEqual(2, timesSegmentationImageReceived);
        }

        [UnityTest]
        public IEnumerator SegmentationPassProducesCorrectValuesEachFrame(
            [Values(SegmentationKind.Instance, SegmentationKind.Semantic)] SegmentationKind segmentationKind)
        {
            int timesSegmentationImageReceived = 0;
            Dictionary<int, object> expectedLabelAtFrame = null;

            //TestHelper.LoadAndStartRenderDocCapture(out var gameView);

            void OnSegmentationImageReceived<T>(int frameCount, NativeArray<T> data, RenderTexture tex) where T : struct
            {
                if (expectedLabelAtFrame == null || !expectedLabelAtFrame.ContainsKey(frameCount)) return;

                timesSegmentationImageReceived++;

                Debug.Log($"Segmentation image received. FrameCount: {frameCount}");

                try
                {
                    CollectionAssert.AreEqual(Enumerable.Repeat(expectedLabelAtFrame[frameCount], data.Length), data);
                }

                // ReSharper disable once RedundantCatchClause
                catch (Exception)
                {
                    //uncomment to get RenderDoc captures while this check is failing
                    //UnityEditorInternal.RenderDoc.EndCaptureRenderDoc(gameView);
                    throw;
                }
            }

            var cameraObject = segmentationKind == SegmentationKind.Instance ?
                SetupCameraInstanceSegmentation(OnSegmentationImageReceived<Color32>) :
                SetupCameraSemanticSegmentation((a) => OnSegmentationImageReceived<Color32>(a.frameCount, a.data, a.sourceTexture), false);

            //object expectedPixelValue = segmentationKind == SegmentationKind.Instance ? (object) new Color32(0, 74, 255, 255) : k_SemanticPixelValue;
            object expectedPixelValue = segmentationKind == SegmentationKind.Instance ? (object) new Color32(255, 0, 0, 255) : k_SemanticPixelValue;

            expectedLabelAtFrame = new Dictionary<int, object>
            {
                {Time.frameCount    , expectedPixelValue},
                {Time.frameCount + 1, expectedPixelValue},
                {Time.frameCount + 2, expectedPixelValue}
            };
            GameObject planeObject;

            //Put a plane in front of the camera
            planeObject = TestHelper.CreateLabeledPlane();
            yield return null;

            //UnityEditorInternal.RenderDoc.EndCaptureRenderDoc(gameView);
            Object.DestroyImmediate(planeObject);
            planeObject = TestHelper.CreateLabeledPlane();

            //TestHelper.LoadAndStartRenderDocCapture(out gameView);
            yield return null;

            //UnityEditorInternal.RenderDoc.EndCaptureRenderDoc(gameView);
            Object.DestroyImmediate(planeObject);
            planeObject = TestHelper.CreateLabeledPlane();
            yield return null;
            Object.DestroyImmediate(planeObject);
            yield return null;

            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);

            Assert.AreEqual(3, timesSegmentationImageReceived);
        }

        GameObject SetupCameraInstanceSegmentation(Action<int, NativeArray<Color32>, RenderTexture> onSegmentationImageReceived)
        {
            var cameraObject = SetupCamera(out var perceptionCamera, false);
            perceptionCamera.InstanceSegmentationImageReadback += onSegmentationImageReceived;
            cameraObject.SetActive(true);
            return cameraObject;
        }

        GameObject SetupCameraSemanticSegmentation(Action<SemanticSegmentationLabeler.ImageReadbackEventArgs> onSegmentationImageReceived, bool showVisualizations)
        {
            var cameraObject = SetupCamera(out var perceptionCamera, showVisualizations);
            var labelConfig = ScriptableObject.CreateInstance<SemanticSegmentationLabelConfig>();
            labelConfig.Init(new List<SemanticSegmentationLabelEntry>()
            {
                new SemanticSegmentationLabelEntry()
                {
                    label = "label",
                    color = k_SemanticPixelValue
                }
            });
            var semanticSegmentationLabeler = new SemanticSegmentationLabeler(labelConfig);
            semanticSegmentationLabeler.imageReadback += onSegmentationImageReceived;
            perceptionCamera.AddLabeler(semanticSegmentationLabeler);
            cameraObject.SetActive(true);
            return cameraObject;
        }

        GameObject SetupCamera(out PerceptionCamera perceptionCamera, bool showVisualizations)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;
            perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            perceptionCamera.showVisualizations = showVisualizations;

            AddTestObjectForCleanup(cameraObject);
            return cameraObject;
        }
    }
}
