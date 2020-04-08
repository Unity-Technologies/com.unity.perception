using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;
#endif
using UnityEngine.Perception.Sensors;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    public class ImageReaderBehaviour : MonoBehaviour
    {
        public RenderTexture source;
        public Camera cameraSource;
        RenderTextureReader<uint> m_Reader;

        public event Action<int, NativeArray<uint>> SegmentationImageReceived;

        void Awake()
        {
            m_Reader = new RenderTextureReader<uint>(source, cameraSource, ImageReadCallback);
        }

        void ImageReadCallback(int frameCount, NativeArray<uint> data, RenderTexture renderTexture)
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

    //Graphics issues with OpenGL Linux Editor. https://jira.unity3d.com/browse/AISV-422
    [UnityPlatform(exclude = new[] {RuntimePlatform.LinuxEditor, RuntimePlatform.LinuxPlayer})]
    public class SegmentationPassTests : PassTestBase
    {
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator SegmentationPassTestsWithEnumeratorPasses()
        {
            int timesSegmentationImageReceived = 0;
            int? frameStart = null;
            Action<int, NativeArray<uint>> onSegmentationImageReceived = (frameCount, data) =>
            {
                if (frameStart == null || frameStart > frameCount)
                    return;

                timesSegmentationImageReceived++;
                CollectionAssert.AreEqual(Enumerable.Repeat(1, data.Length), data);
            };

            var cameraObject = SetupCamera(onSegmentationImageReceived);
            //
            // // Arbitrary wait for 5 frames for shaders to load. Workaround for issue with Shader.WarmupAllShaders()
            // for (int i=0 ; i<5 ; ++i)
            //     yield return new WaitForSeconds(1);

            frameStart = Time.frameCount;

            //Put a plane in front of the camera
            var planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            planeObject.transform.SetPositionAndRotation(new Vector3(0, 0, 10), Quaternion.Euler(90, 0, 0) );
            planeObject.transform.localScale = new Vector3(10, -1, 10);
            planeObject.AddComponent<Labeling>();
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
        [UnityTest]
        public IEnumerator SegmentationPassProducesCorrectValuesEachFrame()
        {
            int timesSegmentationImageReceived = 0;
            Dictionary<int, int> expectedLabelAtFrame = null;

            //TestHelper.LoadAndStartRenderDocCapture(out var gameView);

            Action<int, NativeArray<uint>> onSegmentationImageReceived = (frameCount, data) =>
            {
                if (expectedLabelAtFrame == null || !expectedLabelAtFrame.ContainsKey(frameCount))
                    return;

                timesSegmentationImageReceived++;

                Debug.Log($"Segmentation image received. FrameCount: {frameCount}");

                try
                {
                    CollectionAssert.AreEqual(Enumerable.Repeat(expectedLabelAtFrame[frameCount], data.Length), data);
                }
                catch (Exception e)
                {
                    //uncomment to get RenderDoc captures while this check is failing
                    //RenderDoc.EndCaptureRenderDoc(gameView);
                    throw;
                }
            };

            var cameraObject = SetupCamera(onSegmentationImageReceived);

            expectedLabelAtFrame = new Dictionary<int, int>
            {
                {Time.frameCount    , 1},
                {Time.frameCount + 1, 1},
                {Time.frameCount + 2, 1}
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

        GameObject SetupCamera(Action<int, NativeArray<uint>> onSegmentationImageReceived)
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
            var InstanceSegmentationPass = new InstanceSegmentationPass();
            InstanceSegmentationPass.targetCamera = camera;
            InstanceSegmentationPass.targetTexture = rt;
            customPassVolume.customPasses.Add(InstanceSegmentationPass);
            InstanceSegmentationPass.name = nameof(InstanceSegmentationPass);
            InstanceSegmentationPass.EnsureInit();

            var reader = cameraObject.AddComponent<ImageReaderBehaviour>();
            reader.source = rt;
            reader.cameraSource = camera;

            reader.SegmentationImageReceived += onSegmentationImageReceived;
#endif
#if URP_PRESENT
            var labelingConfiguration = ScriptableObject.CreateInstance<LabelingConfiguration>();
            var perceptionCamera = cameraObject.AddComponent<UnityEngine.Perception.PerceptionCamera>();
            perceptionCamera.LabelingConfiguration = labelingConfiguration;
            perceptionCamera.captureRgbImages = false;
            perceptionCamera.produceBoundingBoxAnnotations = false;
            perceptionCamera.produceObjectCountAnnotations = true;
            perceptionCamera.segmentationImageReceived += onSegmentationImageReceived;
#endif
            AddTestObjectForCleanup(cameraObject);
            cameraObject.SetActive(true);
            return cameraObject;
        }
    }
}
