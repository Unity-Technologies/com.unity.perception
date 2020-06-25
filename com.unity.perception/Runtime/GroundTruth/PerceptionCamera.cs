using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif
#if URP_PRESENT
using UnityEngine.Rendering.Universal;
#endif

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Captures ground truth from the associated Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public partial class PerceptionCamera : MonoBehaviour
    {
        //TODO: Remove the Guid path when we have proper dataset merging in USim/Thea
        internal static string RgbDirectory { get; } = $"RGB{Guid.NewGuid()}";
        static string s_RgbFilePrefix = "rgb_";

        /// <summary>
        /// A human-readable description of the camera.
        /// </summary>
        public string description;
        /// <summary>
        /// The period in seconds that the Camera should render
        /// </summary>
        public float period = .0166f;
        /// <summary>
        /// The start time in seconds of the first frame in the simulation.
        /// </summary>
        public float startTime;
        /// <summary>
        /// Whether camera output should be captured to disk
        /// </summary>
        public bool captureRgbImages = true;
        /// <summary>
        /// Event invoked after the camera finishes rendering during a frame.
        /// </summary>

        [SerializeReference]
        List<CameraLabeler> m_Labelers = new List<CameraLabeler>();
        Dictionary<string, object> m_PersistentSensorData = new Dictionary<string, object>();

#if URP_PRESENT
        internal List<ScriptableRenderPass> passes = new List<ScriptableRenderPass>();
        public void AddScriptableRenderPass(ScriptableRenderPass pass)
        {
            passes.Add(pass);
        }
#endif

        bool m_CapturedLastFrame;

        Ego m_EgoMarker;

        /// <summary>
        /// The <see cref="SensorHandle"/> associated with this camera. Use this to report additional annotations and metrics at runtime.
        /// </summary>
        public SensorHandle SensorHandle { get; private set; }

        static ProfilerMarker s_WriteFrame = new ProfilerMarker("Write Frame (PerceptionCamera)");
        static ProfilerMarker s_FlipY = new ProfilerMarker("Flip Y (PerceptionCamera)");
        static ProfilerMarker s_EncodeAndSave = new ProfilerMarker("Encode and save (PerceptionCamera)");

#if URP_PRESENT
        bool m_GroundTruthRendererFeatureRun;
#endif

        /// <summary>
        /// Add a data object which will be added to the dataset with each capture. Overrides existing sensor data associated with the given key.
        /// </summary>
        /// <param name="key">The key to associate with the data.</param>
        /// <param name="data">An object containing the data. Will be serialized into json.</param>
        public void SetPersistentSensorData(string key, object data)
        {
            m_PersistentSensorData[key] = data;
        }

        /// <summary>
        /// Removes a persistent sensor data object.
        /// </summary>
        /// <param name="key">The key of the object to remove.</param>
        /// <returns>True if a data object was removed. False if it was not set.</returns>
        public bool RemovePersistentSensorData(string key)
        {
            return m_PersistentSensorData.Remove(key);
        }

        // Start is called before the first frame update
        void Awake()
        {
            m_EgoMarker = this.GetComponentInParent<Ego>();
            var ego = m_EgoMarker == null ? DatasetCapture.RegisterEgo("") : m_EgoMarker.EgoHandle;
            SensorHandle = DatasetCapture.RegisterSensor(ego, "camera", description, period, startTime);

            SetupInstanceSegmentation();

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += CheckForRendererFeature;
            DatasetCapture.SimulationEnding += OnSimulationEnding;
        }


        void CheckForRendererFeature(ScriptableRenderContext arg1, Camera arg2)
        {
            if (arg2 == GetComponent<Camera>())
            {
#if URP_PRESENT
                if (!m_GroundTruthRendererFeatureRun)
                {
                    Debug.LogError("GroundTruthRendererFeature must be present on the ScriptableRenderer associated with the camera. The ScriptableRenderer can be accessed through Edit -> Project Settings... -> Graphics -> Scriptable Render Pipeline Settings -> Renderer List.");
                    enabled = false;
                }
#endif
                RenderPipelineManager.endCameraRendering -= CheckForRendererFeature;
            }
        }
        // Update is called once per frame
        void Update()
        {
            if (!SensorHandle.IsValid)
                return;

            var cam = GetComponent<Camera>();
            cam.enabled = SensorHandle.ShouldCaptureThisFrame;

            foreach (var labeler in m_Labelers)
            {
                if (!labeler.enabled)
                    continue;

                if (!labeler.isInitialized)
                    labeler.Init(this);

                labeler.InternalOnUpdate();
            }
        }

        void OnValidate()
        {
            if (m_Labelers == null)
                m_Labelers = new List<CameraLabeler>();
        }

        void CaptureRgbData(Camera cam)
        {
            Profiler.BeginSample("CaptureDataFromLastFrame");
            if (!captureRgbImages)
                return;

            var captureFilename = Path.Combine(Manager.Instance.GetDirectoryFor(RgbDirectory), $"{s_RgbFilePrefix}{Time.frameCount}.png");
            var dxRootPath = Path.Combine(RgbDirectory, $"{s_RgbFilePrefix}{Time.frameCount}.png");
            SensorHandle.ReportCapture(dxRootPath, SensorSpatialData.FromGameObjects(m_EgoMarker == null ? null : m_EgoMarker.gameObject, gameObject), m_PersistentSensorData.Select(kvp => (kvp.Key, kvp.Value)).ToArray());

            Func<AsyncRequest<CaptureCamera.CaptureState>, AsyncRequest.Result> colorFunctor;
            var width = cam.pixelWidth;
            var height = cam.pixelHeight;
            var flipY = ShouldFlipY(cam);

            colorFunctor = r =>
            {
                using (s_WriteFrame.Auto())
                {
                    var dataColorBuffer = (byte[])r.data.colorBuffer;
                    if (flipY)
                        FlipImageY(dataColorBuffer, height);

                    byte[] encodedData;
                    using (s_EncodeAndSave.Auto())
                    {
                        encodedData = ImageConversion.EncodeArrayToPNG(dataColorBuffer, GraphicsFormat.R8G8B8A8_UNorm, (uint)width, (uint)height);
                    }

                    return !FileProducer.Write(captureFilename, encodedData) ? AsyncRequest.Result.Error : AsyncRequest.Result.Completed;
                }
            };

            CaptureCamera.Capture(cam, colorFunctor);

            Profiler.EndSample();
        }

        // ReSharper disable once ParameterHidesMember
        bool ShouldFlipY(Camera camera)
        {
#if HDRP_PRESENT
            var hdAdditionalCameraData = GetComponent<HDAdditionalCameraData>();

            //Based on logic in HDRenderPipeline.PrepareFinalBlitParameters
            return camera.targetTexture != null || hdAdditionalCameraData.flipYMode == HDAdditionalCameraData.FlipYMode.ForceFlipY || camera.cameraType == CameraType.Game;
#elif URP_PRESENT
            return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal) &&
                (camera.targetTexture != null || camera.cameraType == CameraType.Game);
#else
            return false;
#endif
        }

        static unsafe void FlipImageY(byte[] dataColorBuffer, int height)
        {
            using (s_FlipY.Auto())
            {
                var stride = dataColorBuffer.Length / height;
                var buffer = new NativeArray<byte>(stride, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                fixed(byte* colorBufferPtr = &dataColorBuffer[0])
                {
                    var unsafePtr = (byte*)buffer.GetUnsafePtr();
                    for (var row = 0; row < height / 2; row++)
                    {
                        var nearRowStartPtr = colorBufferPtr + stride * row;
                        var oppositeRowStartPtr = colorBufferPtr + stride * (height - row - 1);
                        UnsafeUtility.MemCpy(unsafePtr, oppositeRowStartPtr, stride);
                        UnsafeUtility.MemCpy(oppositeRowStartPtr, nearRowStartPtr, stride);
                        UnsafeUtility.MemCpy(nearRowStartPtr, unsafePtr, stride);
                    }
                }
                buffer.Dispose();
            }
        }

        void OnSimulationEnding()
        {
            CleanUpInstanceSegmentation();
            foreach (var labeler in m_Labelers)
            {
                if (labeler.isInitialized)
                    labeler.InternalCleanup();
            }
        }

        void OnBeginCameraRendering(ScriptableRenderContext _, Camera cam)
        {
            if (cam != GetComponent<Camera>())
                return;
            if (!SensorHandle.ShouldCaptureThisFrame)
                return;
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPaused)
                return;
#endif
            CaptureRgbData(cam);

            foreach (var labeler in m_Labelers)
            {
                if (!labeler.enabled)
                    continue;

                if (!labeler.isInitialized)
                    labeler.Init(this);

                labeler.InternalOnBeginRendering();
            }
        }

        void OnDisable()
        {
            DatasetCapture.SimulationEnding -= OnSimulationEnding;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

            OnSimulationEnding();

#if URP_PRESENT
            instanceSegmentationUrpPass?.Cleanup();
            semanticSegmentationUrpPass?.Cleanup();
#endif

            if (SensorHandle.IsValid)
                SensorHandle.Dispose();

            SensorHandle = default;
        }

        /// <summary>
        /// The <see cref="CameraLabeler"/> instances which will be run for this PerceptionCamera.
        /// </summary>
        public IReadOnlyList<CameraLabeler> labelers => m_Labelers;
        /// <summary>
        /// Add the given <see cref="CameraLabeler"/> to the PerceptionCamera. It will be set up and executed by this
        /// PerceptionCamera each frame it captures data.
        /// </summary>
        /// <param name="cameraLabeler">The labeler to add to this PerceptionCamera</param>
        public void AddLabeler(CameraLabeler cameraLabeler) => m_Labelers.Add(cameraLabeler);

        /// <summary>
        /// Removes the given <see cref="CameraLabeler"/> from the list of labelers under this PerceptionCamera, if it
        /// is in the list. The labeler is cleaned up in the process. Labelers removed from a PerceptionCamera should
        /// not be used again.
        /// </summary>
        /// <param name="cameraLabeler"></param>
        /// <returns></returns>
        public bool RemoveLabeler(CameraLabeler cameraLabeler)
        {
            if (m_Labelers.Remove(cameraLabeler))
            {
                if (cameraLabeler.isInitialized)
                    cameraLabeler.InternalCleanup();

                return true;
            }
            return false;
        }

#if URP_PRESENT
        internal void OnGroundTruthRendererFeatureRun()
        {
            m_GroundTruthRendererFeatureRun = true;
        }
#endif
    }
}
