using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Rendering;

#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif

using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    public class ImageReaderBehaviour : MonoBehaviour
    {
        public RenderTexture source;
        public Camera cameraSource;

        public event Action<int, NativeArray<Color32>> SegmentationImageReceived;

        void Awake()
        {
            RenderPipelineManager.endCameraRendering += (context, _) =>
            {
                var cmd = CommandBufferPool.Get("Test Texture Readback");
                RenderTextureReader.Capture<Color32>(cmd, source, ImageReadCallback);
                context.ExecuteCommandBuffer(cmd);
            };
        }

        void ImageReadCallback(int frameCount, NativeArray<Color32> data, RenderTexture renderTexture)
        {
            SegmentationImageReceived?.Invoke(frameCount, data);
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
        static readonly Color32 k_InstanceSegmentationPixelValue = new Color32(255, 0, 0, 255);
        static readonly Color32 k_SkyValue = new Color32(0, 0, 0, 255);

        public enum SegmentationKind
        {
            Instance,
            Semantic
        }

        enum ColorExpectation
        {
            Foreground,
            Background
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator SegmentationPassTestsWithEnumeratorPasses(
            [Values(RendererType.MeshRenderer, RendererType.SkinnedMeshRenderer, RendererType.Terrain)] RendererType rendererType,
            [Values(SegmentationKind.Instance, SegmentationKind.Semantic)] SegmentationKind segmentationKind)
        {
            var frameStart = Time.frameCount;
            var cameraObject = SetupCameraAndExpectColor(segmentationKind, frameStart, ColorExpectation.Foreground);

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

            AddTestObjectForCleanup(planeObject);

            yield return null;
            yield return null;
            yield return null;
            yield return null;
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            DestroyTestObject(planeObject);

            Assert.AreEqual(4, m_TimesSegmentationImageReceived);
        }

#if HDRP_PRESENT
        [UnityTest]
        public IEnumerator Segmentation_OnPartiallyTransparentObjects_Thresholds(
            [Values(SegmentationKind.Instance, SegmentationKind.Semantic)] SegmentationKind segmentationKind,
            [Values(.7f, .3f)] float textureAlpha, [Values(.5f)] float alphaThreshold)
        {
            var frameStart = Time.frameCount;

            var colorExpectation = alphaThreshold > textureAlpha
                ? ColorExpectation.Background
                : ColorExpectation.Foreground;
            var camera = SetupCameraAndExpectColor(segmentationKind, frameStart, colorExpectation);
            camera.GetComponent<PerceptionCamera>().alphaThreshold = alphaThreshold;

            // Put a plane in front of the camera
            var planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            AddTestObjectForCleanup(planeObject);

            planeObject.transform.SetPositionAndRotation(new Vector3(0, 0, 10), Quaternion.Euler(90, 0, 0));
            planeObject.transform.localScale = new Vector3(10, -1, 10);

            var texture = TestHelper.CreateBlankTexture(
                100, 100, GraphicsFormat.R8G8B8A8_UNorm, new Color(1f, 1f, 1f, textureAlpha));

            const string shaderName = "HDRP/Unlit";

            var material = new Material(Shader.Find(shaderName))
            {
                mainTexture = texture
            };

            planeObject.GetComponent<MeshRenderer>().material = material;

            var labeling = planeObject.AddComponent<Labeling>();
            labeling.labels.Add("label");

            yield return null;
            DatasetCapture.ResetSimulation();

            Assert.AreEqual(1, m_TimesSegmentationImageReceived);
        }

#endif

        public enum ObjectInFilteredLayer
        {
            InLayer,
            NotInLayer
        }

        public enum FilterTarget
        {
            Camera,
            PerceptionCamera
        }
        [UnityTest]
        public IEnumerator Segmentation_OnObjectOutsideLayer_Thresholds(
            [Values(SegmentationKind.Instance, SegmentationKind.Semantic)] SegmentationKind segmentationKind,
            [Values(ObjectInFilteredLayer.InLayer, ObjectInFilteredLayer.NotInLayer)] ObjectInFilteredLayer objectInFilteredLayer,
            [Values(FilterTarget.Camera, FilterTarget.PerceptionCamera)] FilterTarget filterTarget)
        {
            var frameStart = Time.frameCount;

            var colorExpectation = objectInFilteredLayer == ObjectInFilteredLayer.InLayer
                ? ColorExpectation.Foreground
                : ColorExpectation.Background;
            var camera = SetupCameraAndExpectColor(segmentationKind, frameStart, colorExpectation);
            if (filterTarget == FilterTarget.PerceptionCamera)
            {
                camera.GetComponent<PerceptionCamera>().overrideLayerMask = true;
                camera.GetComponent<PerceptionCamera>().layerMask = 1 << 1;
            }
            else
                camera.GetComponent<Camera>().cullingMask = 1 << 1;

            //Put a plane covering another plane, where the plane in the back is labeled but the one in the front is not
            var planeObject = TestHelper.CreateLabeledPlane();
            AddTestObjectForCleanup(planeObject);
            if (objectInFilteredLayer == ObjectInFilteredLayer.InLayer)
                planeObject.layer = 1;
            else
                planeObject.layer = 2;

            //while (true)
            yield return null;

            DatasetCapture.ResetSimulation();

            Assert.AreEqual(1, m_TimesSegmentationImageReceived);
        }

        private int m_TimesSegmentationImageReceived;
        private GameObject SetupCameraAndExpectColor(SegmentationKind segmentationKind, int? frameStart, ColorExpectation colorExpectation)
        {
            object expectedPixelValue;
            m_TimesSegmentationImageReceived = 0;

            void OnSegmentationImageReceived<T>(int frameCount, NativeArray<T> data, RenderTexture tex) where T : struct
            {
                if (frameStart == null || frameStart > frameCount) return;

                m_TimesSegmentationImageReceived++;
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, data.Length), data.ToArray());
            }

            GameObject cameraObject;

            switch (segmentationKind)
            {
                case SegmentationKind.Instance:
                    expectedPixelValue = k_InstanceSegmentationPixelValue;
                    cameraObject = SetupCameraInstanceSegmentation(OnSegmentationImageReceived);
                    break;
                case SegmentationKind.Semantic:
                    expectedPixelValue = k_SemanticPixelValue;
                    cameraObject =
                        SetupCameraSemanticSegmentation(OnSegmentationImageReceived, false);
                    break;
                default:
                    return null;
            }

            if (colorExpectation == ColorExpectation.Background)
                expectedPixelValue = new Color32(0, 0, 0, 255);

            AddTestObjectForCleanup(cameraObject);
            return cameraObject;
        }

#if HDRP_PRESENT
        [UnityTest]
        [UnityPlatform(exclude = new[] {RuntimePlatform.OSXPlayer})]
        public IEnumerator SemanticSegmentationPass_WithLensDistortion()
        {
            // Add a perception camera to the scene.
            var cameraObject = SetupCamera(out var perceptionCamera, false);
            cameraObject.SetActive(true);

            // Put a quad in front of the camera.
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "quad";
            quad.transform.position = new Vector3(0, 0, 1);
            var labeling = quad.AddComponent<Labeling>();
            labeling.labels.Add("label");
            AddTestObjectForCleanup(quad);

            // Setup a callback to capture the 2D bounding box of the quad for each rendered frame.
            var frameIndex = 0;
            var capturedBoundingBoxes = new Rect[2];
            perceptionCamera.EnableChannel<InstanceIdChannel>();
            perceptionCamera.RenderedObjectInfosCalculated += (_, boundingBoxes, _) =>
            {
                Assert.Greater(boundingBoxes.Length, 0, "No bounding boxes were generated.");
                capturedBoundingBoxes[frameIndex] = boundingBoxes[0].boundingBox;
                frameIndex++;
            };

            // Add the lens distortion effect to a post processing volume.
            var volumeObject = new GameObject("Volume");
            AddTestObjectForCleanup(volumeObject);
            var volume = volumeObject.gameObject.AddComponent<Volume>();
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AddTestObjectForCleanup(profile);
            var lensDistortion = profile.Add<LensDistortion>();
            lensDistortion.intensity.overrideState = true;
            volume.profile = profile;

            // Capture the quad's bounding box without lens distortion.
            lensDistortion.intensity.value = 0.0f;
            yield return null;

            // Capture the quad's bounding box with lens distortion.
            lensDistortion.intensity.value = 0.715f;
            yield return null;

            // Destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);

            // Confirm that the size of the quad's bounding box increases when the lens distortion effect is applied.
            var boundingBoxWithoutLensDistortion = capturedBoundingBoxes[0];
            var boundingBoxWithLensDistortion = capturedBoundingBoxes[1];
            Assert.AreNotEqual(boundingBoxWithoutLensDistortion, boundingBoxWithLensDistortion);
            Assert.Greater(boundingBoxWithLensDistortion.width, boundingBoxWithoutLensDistortion.width);
        }

#endif

        [UnityTest]
        public IEnumerator SemanticSegmentationPass_WithLabeledButNotMatchingObject_ProducesBlack()
        {
            int timesSegmentationImageReceived = 0;
            var expectedPixelValue = new Color32(0, 0, 0, 255);
            void OnSegmentationImageReceived(NativeArray<Color32> data)
            {
                timesSegmentationImageReceived++;
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, data.Length), data.ToArray());
            }

            var cameraObject = SetupCameraSemanticSegmentation(
                (_, data, _) => OnSegmentationImageReceived(data), false, k_SkyValue);

            AddTestObjectForCleanup(TestHelper.CreateLabeledPlane(label: "non-matching"));
            yield return null;
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            Assert.AreEqual(1, timesSegmentationImageReceived);
        }

        [UnityTest]
        public IEnumerator SemanticSegmentationPass_WithMatchingButDisabledLabel_ProducesBlack()
        {
            int timesSegmentationImageReceived = 0;
            var expectedPixelValue = new Color32(0, 0, 0, 255);
            void OnSegmentationImageReceived(NativeArray<Color32> data)
            {
                timesSegmentationImageReceived++;
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, data.Length), data.ToArray());
            }

            var cameraObject = SetupCameraSemanticSegmentation(
                (_, data, _) => OnSegmentationImageReceived(data), false, k_SkyValue);

            var gameObject = TestHelper.CreateLabeledPlane();
            gameObject.GetComponent<Labeling>().enabled = false;
            AddTestObjectForCleanup(gameObject);
            yield return null;
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            Assert.AreEqual(1, timesSegmentationImageReceived);
        }

        [UnityTest]
        public IEnumerator SemanticSegmentationPass_WithMatchingButDisabledLabel_ProducesNoInstances()
        {
            var collector = new TestCollectorEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            DatasetCapture.ResetSimulation();

            var cameraObject = SetupCameraSemanticSegmentation(null, false, k_SkyValue);

            var gameObject = TestHelper.CreateLabeledPlane();
            gameObject.GetComponent<Labeling>().enabled = false;
            AddTestObjectForCleanup(gameObject);
            yield return null;
            DatasetCapture.ResetSimulation();

            Assert.AreEqual(1, collector.currentRun.frames.Count);
            var annotations = collector.currentRun.frames[0].sensors[0].annotations;
            Assert.AreEqual(1, annotations.Count);
            Assert.AreEqual(0, ((SemanticSegmentationAnnotation)annotations[0]).instances.ToList().Count);
        }

        [UnityTest]
        public IEnumerator InstanceSegmentationPass_WithMatchingButDisabledLabel_ProducesBlack()
        {
            int timesSegmentationImageReceived = 0;
            var expectedPixelValue = new Color32(0, 0, 0, 255);
            void OnSegmentationImageReceived(NativeArray<Color32> data)
            {
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, data.Length), data);
                timesSegmentationImageReceived++;
            }

            var cameraObject = SetupCameraInstanceSegmentation((_, data, _) => OnSegmentationImageReceived(data));

            var gameObject = TestHelper.CreateLabeledPlane();
            gameObject.GetComponent<Labeling>().enabled = false;
            AddTestObjectForCleanup(gameObject);
            yield return null;
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            Assert.AreEqual(1, timesSegmentationImageReceived);
        }

        [UnityTest]
        public IEnumerator SemanticSegmentationPass_WithEmptyFrame_ProducesSky([Values(false, true)] bool showVisualizations)
        {
            var collector = new TestCollectorEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            DatasetCapture.ResetSimulation();

            var timesSegmentationImageReceived = 0;
            var expectedPixelValue = k_SkyValue;
            void OnSegmentationImageReceived(NativeArray<Color32> data)
            {
                timesSegmentationImageReceived++;
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, data.Length), data.ToArray());
            }

            SetupCameraSemanticSegmentation(
                (_, data, _) => OnSegmentationImageReceived(data), showVisualizations, expectedPixelValue);

            yield return null;

            DatasetCapture.ResetSimulation();
            Assert.AreEqual(1, timesSegmentationImageReceived);
            var annotations = collector.currentRun.frames[0].sensors[0].annotations;
            Assert.AreEqual(1, annotations.Count);
            var entries = ((SemanticSegmentationAnnotation)annotations[0]).instances;

            // sky is not a rendering object any more
            CollectionAssert.AreEqual(Array.Empty<SemanticSegmentationDefinitionEntry>(), entries);
        }

        [UnityTest]
        public IEnumerator SemanticSegmentationPass_WithNoObjects_ProducesSky()
        {
            int timesSegmentationImageReceived = 0;
            var expectedPixelValue = k_SkyValue;
            void OnSegmentationImageReceived(NativeArray<Color32> data)
            {
                timesSegmentationImageReceived++;
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedPixelValue, data.Length), data.ToArray());
            }

            var cameraObject = SetupCameraSemanticSegmentation(
                (_, data, _) => OnSegmentationImageReceived(data), false, expectedPixelValue);

            yield return null;
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            Assert.AreEqual(1, timesSegmentationImageReceived);
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

            var cameraObject = SetupCameraSemanticSegmentation(
                (_, data, _) => OnSegmentationImageReceived(data), false);

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

            var cameraObject = SetupCameraSemanticSegmentation(
                (_, data, _) => OnSegmentationImageReceived(data), false);

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
        public IEnumerator InstanceSegmentationPass_WithSeparateDisabledPerceptionCamera_ProducesCorrectValues()
        {
            int timesSegmentationImageReceived = 0;
            void OnSegmentationImageReceived(NativeArray<Color32> data)
            {
                CollectionAssert.AreEqual(Enumerable.Repeat(k_InstanceSegmentationPixelValue, data.Length), data);
                timesSegmentationImageReceived++;
            }

            var cameraObject = SetupCameraInstanceSegmentation((_, data, _) => OnSegmentationImageReceived(data));
            var cameraObject2 = SetupCameraInstanceSegmentation(null);
            cameraObject2.SetActive(false);

            var plane = TestHelper.CreateLabeledPlane();
            AddTestObjectForCleanup(plane);
            yield return null;
            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            DestroyTestObject(cameraObject2);
            Assert.AreEqual(1, timesSegmentationImageReceived);
        }

        [UnityTest]
        public IEnumerator SegmentationPass_WithMultiplePerceptionCameras_ProducesCorrectValues(
            [Values(SegmentationKind.Instance, SegmentationKind.Semantic)] SegmentationKind segmentationKind)
        {
            int timesSegmentationImageReceived = 0;

            var color1 = segmentationKind == SegmentationKind.Instance ?
                k_InstanceSegmentationPixelValue :
                k_SemanticPixelValue;
            var color2 = segmentationKind == SegmentationKind.Instance ?
                new Color32(0, 74, Byte.MaxValue, Byte.MaxValue) :
                new Color32(0, 0, 0, Byte.MaxValue);
            void OnCam1SegmentationImageReceived(NativeArray<Color32> data)
            {
                CollectionAssert.AreEqual(Enumerable.Repeat(color1, data.Length), data);
                timesSegmentationImageReceived++;
            }

            void OnCam2SegmentationImageReceived(NativeArray<Color32> data)
            {
                Assert.AreEqual(color1, data[data.Length / 4]);
                Assert.AreEqual(color2, data[data.Length * 3 / 4]);
                timesSegmentationImageReceived++;
            }

            GameObject cameraObject;
            GameObject cameraObject2;
            if (segmentationKind == SegmentationKind.Instance)
            {
                cameraObject = SetupCameraInstanceSegmentation((_, data, _) => OnCam1SegmentationImageReceived(data));
                cameraObject2 = SetupCameraInstanceSegmentation((_, data, _) => OnCam2SegmentationImageReceived(data));
            }
            else
            {
                cameraObject = SetupCameraSemanticSegmentation((_, data, _) => OnCam1SegmentationImageReceived(data), false);
                cameraObject2 = SetupCameraSemanticSegmentation((_, data, _) => OnCam2SegmentationImageReceived(data), false);
            }
            //position camera to point straight at the top edge of plane1, such that plane1 takes up the bottom half of
            //the image and plane2 takes up the top half
            cameraObject2.transform.localPosition = Vector3.up * 10f;

            var plane1 = TestHelper.CreateLabeledPlane(2f);
            var plane2 = TestHelper.CreateLabeledPlane(2f, "label2");
            plane2.transform.localPosition = plane2.transform.localPosition + Vector3.up * 20f;
            AddTestObjectForCleanup(plane1);
            AddTestObjectForCleanup(plane2);
            yield return null;

            //destroy the object to force all pending segmented image readbacks to finish and events to be fired.
            DestroyTestObject(cameraObject);
            DestroyTestObject(cameraObject2);
            Assert.AreEqual(2, timesSegmentationImageReceived);
        }

        [UnityTest]
        public IEnumerator SegmentationPassProducesCorrectValuesEachFrame(
            [Values(SegmentationKind.Instance, SegmentationKind.Semantic)] SegmentationKind segmentationKind)
        {
            var timesSegmentationImageReceived = 0;
            var expectedPixelValue = segmentationKind == SegmentationKind.Instance
                ? k_InstanceSegmentationPixelValue : k_SemanticPixelValue;
            var expectedBackgroundPixelColorAtFrame = new Dictionary<int, Color32>
            {
                {Time.frameCount    , expectedPixelValue},
                {Time.frameCount + 1, expectedPixelValue},
                {Time.frameCount + 2, expectedPixelValue}
            };

            void OnSegmentationImageReceived(int frameCount, NativeArray<Color32> data, RenderTexture tex)
            {
                if (!expectedBackgroundPixelColorAtFrame.ContainsKey(frameCount))
                    return;

                timesSegmentationImageReceived++;
                var expectedColor = expectedBackgroundPixelColorAtFrame[frameCount];
                CollectionAssert.AreEqual(Enumerable.Repeat(expectedColor, data.Length), data.ToArray());
            }

            var cameraObject = segmentationKind == SegmentationKind.Instance ?
                SetupCameraInstanceSegmentation(OnSegmentationImageReceived) :
                SetupCameraSemanticSegmentation(OnSegmentationImageReceived, false);

            // Put a plane in front of the camera to force the background of the
            // segmentation images to be a color other than black.
            var planeObject = TestHelper.CreateLabeledPlane();

            // Wait 3 frames
            for (var i = 0; i < 3; i++)
                yield return null;

            // Destroy the camera to force all pending segmentation image readbacks and subsequent callbacks to finish
            DestroyTestObject(cameraObject);
            Object.DestroyImmediate(planeObject);

            Assert.AreEqual(3, timesSegmentationImageReceived);
        }

        GameObject SetupCameraInstanceSegmentation(
            Action<int, NativeArray<Color32>, RenderTexture> onSegmentationImageReceived)
        {
            var cameraObject = SetupCamera(out var perceptionCamera, false);

            var labelConfig = ScriptableObject.CreateInstance<IdLabelConfig>();
            labelConfig.Init(new List<IdLabelEntry> { new IdLabelEntry { label = "label", id = 1 } });

            var semanticSegmentationLabeler = new InstanceSegmentationLabeler(labelConfig);
            semanticSegmentationLabeler.imageReadback += onSegmentationImageReceived;
            perceptionCamera.AddLabeler(semanticSegmentationLabeler);

            cameraObject.SetActive(true);
            return cameraObject;
        }

        GameObject SetupCameraSemanticSegmentation(
            Action<int, NativeArray<Color32>, RenderTexture> onSegmentationImageReceived,
            bool showVisualizations, Color? backgroundColor = null)
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
            if (backgroundColor != null)
            {
                labelConfig.skyColor = backgroundColor.Value;
            }
            var semanticSegmentationLabeler = new SemanticSegmentationLabeler(labelConfig);
            semanticSegmentationLabeler.imageReadback += onSegmentationImageReceived;
            perceptionCamera.AddLabeler(semanticSegmentationLabeler);
            cameraObject.SetActive(true);
            return cameraObject;
        }

        GameObject SetupCamera(out PerceptionCamera perceptionCamera, bool showVisualizations)
        {
            var cameraObject = new GameObject("Camera");
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            camera.targetTexture = texture;
            camera.orthographic = true;
            camera.orthographicSize = 1;
            perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            perceptionCamera.showVisualizations = showVisualizations;

            AddTestObjectForCleanup(cameraObject);
            AddTestObjectForCleanup(texture);
            return cameraObject;
        }
    }
}
