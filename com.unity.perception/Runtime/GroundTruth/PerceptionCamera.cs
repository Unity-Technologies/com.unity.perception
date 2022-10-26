using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.Settings;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif
#if URP_PRESENT
using UnityEngine.Rendering.Universal;
#endif
#pragma warning disable 649

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Captures ground truth from the associated Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public partial class PerceptionCamera : MonoBehaviour
    {
        const float k_PanelWidth = 200;
        const float k_PanelHeight = 250;

        static ProfilerMarker s_WriteFrame = new ProfilerMarker("Write Frame (PerceptionCamera)");

        static PerceptionCamera s_VisualizedPerceptionCamera;

        internal HUDPanel hudPanel;
        internal OverlayPanel overlayPanel;

        [SerializeReference]
        List<CameraLabeler> m_Labelers = new List<CameraLabeler>();
        Dictionary<string, object> m_PersistentSensorData = new Dictionary<string, object>();

        bool m_SimulationEnded;
        bool m_ShowingVisualizations;
        bool m_GUIStylesInitialized;
        int m_LastFrameCaptured = -1;
        int m_LastFrameEndRendering = -1;
        SensorHandle m_SensorHandle;
        Vector2 m_ScrollPosition;
        RgbSensor m_RgbSensorCapture;
        RgbSensorDefinition m_SensorDefinition;
        AsyncFuture<Sensor> m_Future;

#if URP_PRESENT
        // only used to confirm that GroundTruthRendererFeature is present in URP
        bool m_IsGroundTruthRendererFeaturePresent;
        internal List<ScriptableRenderPass> passes = new List<ScriptableRenderPass>();
#endif

        /// <summary>
        /// The number of capture-able frames that have been generated
        /// </summary>
#if UNITY_EDITOR
        public static int captureFrameCount => Time.frameCount - 2;
#else
        public static int captureFrameCount => Time.frameCount - 1;
#endif

        /// <summary>
        /// A toggle for choosing whether to use async or synchronous readback APIs to transfer captured RGB image
        /// data from the GPU back to the CPU. The more performant option of async readbacks is enabled by default.
        /// </summary>
        public static bool useAsyncReadbackIfSupported = true;

        /// <summary>
        /// An event that executes for each captured RGB image that is read from GPU memory back to CPU memory.
        /// </summary>
        public Action<int, NativeArray<Color32>> RgbCaptureReadback;

        /// <summary>
        /// The string ID of this camera sensor
        /// </summary>
        public string ID = "camera";

        /// <summary>
        /// A human-readable description of the camera.
        /// </summary>
        public string description;

        /// <summary>
        /// Whether camera output should be captured to disk.
        /// </summary>
        public bool captureRgbImages = true;

        /// <summary>
        /// The image encoding format used to encode captured RGB images.
        /// </summary>
        ImageEncodingFormat k_RgbImageEncodingFormat = ImageEncodingFormat.Png;

        /// <summary>
        /// The image graphics format used to encode captured images.
        /// </summary>
        public GraphicsFormat cameraGraphicsFormat = GraphicsFormat.R8G8B8A8_SRGB;

        /// <summary>
        /// Caches access to the camera attached to the perception camera.
        /// </summary>
        public Camera attachedCamera { get; private set; }

        /// <summary>
        /// Frame number at which this camera starts capturing.
        /// </summary>
        public int firstCaptureFrame;

        /// <summary>
        /// The method of triggering captures for this camera.
        /// </summary>
        public CaptureTriggerMode captureTriggerMode = CaptureTriggerMode.Scheduled;

        /// <summary>
        /// Have this unscheduled (manual capture) camera affect simulation timings (similar to a scheduled camera) by
        /// requesting a specific frame delta time.
        /// </summary>
        public bool manualSensorAffectSimulationTiming;

        /// <summary>
        /// The simulation frame time (seconds) for this camera. E.g. 0.0166 translates to 60 frames per second.
        /// This will be used as Unity's <see cref="Time.captureDeltaTime"/>, causing a fixed number of frames to be
        /// generated for each second of elapsed simulation time regardless of the capabilities of the underlying hardware.
        /// </summary>
        public float simulationDeltaTime = 0.0166f;

        /// <summary>
        /// The number of frames to simulate and render between the camera's scheduled captures.
        /// Setting this to 0 makes the camera capture every frame.
        /// </summary>
        public int framesBetweenCaptures;

        /// <summary>
        /// Turns on/off the realtime visualization capability.
        /// </summary>
        [SerializeField]
        public bool showVisualizations = true;

        /// <summary>
        /// The <see cref="CameraLabeler"/> instances which will be run for this PerceptionCamera.
        /// </summary>
        public IReadOnlyList<CameraLabeler> labelers => m_Labelers;

        /// <summary>
        /// Requests a capture from this camera on the next rendered frame.
        /// Can only be used when using <see cref="CaptureTriggerMode.Manual"/> capture mode.
        /// </summary>
        public void RequestCapture()
        {
            if (captureTriggerMode.Equals(CaptureTriggerMode.Manual))
            {
                SensorHandle.RequestCapture();
            }
            else
            {
                Debug.LogError($"{nameof(RequestCapture)} can only be used if the camera is in " +
                    $"{nameof(CaptureTriggerMode.Manual)} capture mode.");
            }
        }

        /// <summary>
        /// The <see cref="SensorHandle"/> associated with this camera.
        /// Use this to report additional annotations and metrics at runtime.
        /// </summary>
        public SensorHandle SensorHandle
        {
            get
            {
                EnsureSensorRegistered();
                return m_SensorHandle;
            }
            private set => m_SensorHandle = value;
        }

        /// <summary>
        /// Add a data object which will be added to the dataset with each capture.
        /// Overrides existing sensor data associated with the given key.
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

        void Start()
        {
            Application.runInBackground = true;
            k_RgbImageEncodingFormat = PerceptionSettings.instance.RgbImageEncodingFormat;

            SetupInstanceSegmentation();
            attachedCamera = GetComponent<Camera>();

            SetupVisualizationCamera();

            DatasetCapture.SimulationEnding += OnSimulationEnding;
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += CheckForRendererFeature;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        // LateUpdate is called once per frame. It is called after coroutines, ensuring it is called properly after
        // creation when running tests, since the test runner uses coroutines to run test code.
        void LateUpdate()
        {
            EnsureSensorRegistered();
            if (!SensorHandle.IsValid)
                return;

            foreach (var labeler in m_Labelers)
            {
                if (!labeler.enabled)
                    continue;

                if (!labeler.isInitialized)
                    labeler.Init(this);

                labeler.InternalOnUpdate();
            }

            // Currently there is an issue in the perception camera that causes the UI layer not to be visualized
            // if we are utilizing async readback and we have to flip our captured image.
            // We have created a jira issue for this (aisv-779) and have notified the engine team about this.
            if (m_ShowingVisualizations)
                useAsyncReadbackIfSupported = false;
        }

        void OnGUI()
        {
            if (!m_ShowingVisualizations) return;

            if (!m_GUIStylesInitialized) SetUpGUIStyles();

            GUI.depth = 5;

            var anyLabelerEnabled = false;

            // If a labeler has never been initialized then it was off from the
            // start, it should not be called to draw on the UI
            foreach (var labeler in m_Labelers.Where(labeler => labeler.isInitialized))
            {
                labeler.Visualize();
                anyLabelerEnabled = true;
            }

            if (!anyLabelerEnabled)
            {
                DisplayNoLabelersMessage();
                return;
            }

            GUI.depth = 0;

            hudPanel.OnDrawGUI();

            var x = Screen.width - k_PanelWidth - 10;
            var height = Math.Min(Screen.height * 0.5f - 20, k_PanelHeight);

            GUILayout.BeginArea(new Rect(x, 10, k_PanelWidth, height), GUI.skin.box);

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);

            // If a labeler has never been initialized then it was off from the
            // start, it should not be called to draw on the UI
            foreach (var labeler in m_Labelers.Where(labeler => labeler.isInitialized))
            {
                labeler.VisualizeUI();
            }

            // This needs to happen here so that the overlay panel controls
            // are placed in the controls panel
            overlayPanel.OnDrawGUI(x, 10, k_PanelWidth, height);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void OnValidate()
        {
            if (m_Labelers == null)
                m_Labelers = new List<CameraLabeler>();
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            RenderPipelineManager.endCameraRendering -= CheckForRendererFeature;
        }

        void OnDestroy()
        {
            DatasetCapture.SimulationEnding -= OnSimulationEnding;

            OnSimulationEnding();
            CleanupVisualization();

            if (SensorHandle.IsValid)
                SensorHandle.Dispose();

            SensorHandle = default;
        }

        void EnsureSensorRegistered()
        {
            if (m_SensorHandle.IsNil)
            {
                m_SensorDefinition = new RgbSensorDefinition(ID, captureTriggerMode, description, firstCaptureFrame,
                    framesBetweenCaptures, manualSensorAffectSimulationTiming, "camera", simulationDeltaTime);
                SensorHandle = DatasetCapture.RegisterSensor(m_SensorDefinition);
            }
        }

        void SetupVisualizationCamera()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            showVisualizations = false;
#else
            var visualizationAllowed = s_VisualizedPerceptionCamera == null;

            if (!visualizationAllowed && showVisualizations)
            {
                Debug.LogWarning("Currently only one PerceptionCamera may be visualized at a time. " +
                    $"Disabling visualization on {gameObject.name}.");
                showVisualizations = false;
                return;
            }
            if (!showVisualizations)
                return;

            m_ShowingVisualizations = true;
            s_VisualizedPerceptionCamera = this;

            hudPanel = gameObject.AddComponent<HUDPanel>();
            overlayPanel = gameObject.AddComponent<OverlayPanel>();
            overlayPanel.perceptionCamera = this;
#endif
        }

        void CheckForRendererFeature(ScriptableRenderContext context, Camera cam)
        {
            if (cam == attachedCamera)
            {
#if URP_PRESENT
                if (!m_IsGroundTruthRendererFeaturePresent)
                {
                    Debug.LogError("GroundTruthRendererFeature must be present on the ScriptableRenderer associated " +
                        "with the camera. The ScriptableRenderer can be accessed through Edit -> Project Settings... " +
                        "-> Graphics -> Scriptable Render Pipeline Settings -> Renderer List.");
                    enabled = false;
                }
#endif
                RenderPipelineManager.endCameraRendering -= CheckForRendererFeature;
            }
        }

#if URP_PRESENT
        public void AddScriptableRenderPass(ScriptableRenderPass pass)
        {
            passes.Add(pass);
        }
#endif

        void SetUpGUIStyles()
        {
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.font = Resources.Load<Font>("Inter-Light");
            GUI.skin.label.padding = new RectOffset(0, 0, 1, 1);
            GUI.skin.label.margin = new RectOffset(0, 0, 1, 1);
            GUI.skin.label.wordWrap = true;
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
            GUI.skin.box.padding = new RectOffset(5, 5, 5, 5);
            GUI.skin.toggle.margin = new RectOffset(0, 0, 0, 0);
            GUI.skin.horizontalSlider.margin = new RectOffset(0, 0, 0, 0);
            m_GUIStylesInitialized = true;
        }

        void DisplayNoLabelersMessage()
        {
            var x = Screen.width - k_PanelWidth - 10;
            var height = Math.Min(Screen.height * 0.5f - 20, 90);

            GUILayout.BeginArea(new Rect(x, 10, k_PanelWidth, height), GUI.skin.box);

            GUILayout.Label("Visualization: No labelers are currently active. Enable at least one labeler from the " +
                "inspector window of your perception camera to see visualizations.");

            // If a labeler has never been initialized then it was off from the
            // start, it should not be called to draw on the UI
            foreach (var labeler in m_Labelers.Where(labeler => labeler.isInitialized))
            {
                labeler.VisualizeUI();
                GUILayout.Space(4);
            }

            GUILayout.EndArea();
        }

        void OnSimulationEnding()
        {
            RenderTextureReader.WaitForAllImages();
            ImageEncoder.WaitForAllEncodingJobsToComplete();

            m_SimulationEnded = true;
            CleanUpInstanceSegmentation();
            foreach (var labeler in m_Labelers)
            {
                if (labeler.isInitialized)
                    labeler.InternalCleanup();
            }
        }

        void OnBeginCameraRendering(ScriptableRenderContext scriptableRenderContext, Camera cam)
        {
            ImageEncoder.ExecutePendingCallbacks();

            if (!ShouldCaptureThisFrame(cam, m_LastFrameCaptured))
                return;
            m_LastFrameCaptured = Time.frameCount;
            BeginSensorCapture();
            CallOnLabelers(l => l.InternalOnBeginRendering(scriptableRenderContext));
        }

        void OnEndCameraRendering(ScriptableRenderContext scriptableRenderContext, Camera cam)
        {
            if (!ShouldCaptureThisFrame(cam, m_LastFrameEndRendering))
                return;

            // Submit the graphics commands already queued up in this camera's ScriptableRenderContext to ensure that
            // the custom passes added by the labelers in this camera have written to their respective RenderTextures.
            scriptableRenderContext.Submit();

            m_LastFrameEndRendering = Time.frameCount;
            CaptureRGB(scriptableRenderContext);
            CallOnLabelers(l => l.InternalOnEndRendering(scriptableRenderContext));
            CaptureInstanceSegmentation(scriptableRenderContext);
        }

        void CallOnLabelers(Action<CameraLabeler> action)
        {
            foreach (var labeler in m_Labelers)
            {
                if (!labeler.enabled)
                    continue;

                if (!labeler.isInitialized)
                    labeler.Init(this);

                action(labeler);
            }
        }

        bool ShouldCaptureThisFrame(Camera cam, int lastFrameCalledThisCallback)
        {
            if (m_SimulationEnded)
                return false;
            if (cam != attachedCamera)
                return false;
            if (!SensorHandle.ShouldCaptureThisFrame)
                return false;
            // There are cases when OnBegin/EndCameraRendering is called multiple times in the same frame.
            // Ignore the subsequent calls.
            return lastFrameCalledThisCallback != Time.frameCount;
        }

        void BeginSensorCapture()
        {
            var trans = transform;

            var dimension = new Vector2(attachedCamera.pixelWidth, attachedCamera.pixelHeight);
            var projection = attachedCamera.orthographic
                ? RgbSensor.ImageProjection.Isometric : RgbSensor.ImageProjection.Perspective;
            m_RgbSensorCapture = new RgbSensor(
                m_SensorDefinition,
                trans.position, trans.rotation, k_RgbImageEncodingFormat, projection, dimension)
            {
                matrix = ToProjectionMatrix3x3(attachedCamera.projectionMatrix)
            };

            if (!captureRgbImages)
                SensorHandle.ReportSensor(m_RgbSensorCapture);
            else
                m_Future = SensorHandle.ReportSensorAsync();
        }

        void CaptureRGB(ScriptableRenderContext ctx)
        {
            if (!captureRgbImages)
                return;

            Profiler.BeginSample("CaptureDataFromLastFrame");
            var cmd = CommandBufferPool.Get($"{ID}_PerceptionRGBCapture");
            var tempRT1 = RenderTexture.GetTemporary(
                attachedCamera.pixelWidth, attachedCamera.pixelHeight, 0, cameraGraphicsFormat);
            var tempRT2 = RenderTexture.GetTemporary(
                attachedCamera.pixelWidth, attachedCamera.pixelHeight, 0, cameraGraphicsFormat);

            // Blit the back buffer to a temporary RenderTexture to obtain the RGB output image
            cmd.Blit(null, tempRT1);

            Vector2 scaleForFlip;
            Vector2 offset;
            if (RenderingUtil.ShouldFlipColorY(attachedCamera, false))
            {
                scaleForFlip = new Vector2(1, -1);
                offset = Vector2.up;
            }
            else
            {
                scaleForFlip = new Vector2(1, 1);
                offset = Vector2.zero;
            }

            cmd.Blit(tempRT1, tempRT2, scaleForFlip, offset);

            // Execute the CommandBuffer
            ctx.ExecuteCommandBuffer(cmd);
            ctx.Submit();

            // Release the CommandBuffer
            CommandBufferPool.Release(cmd);
            tempRT1.Release();

            // Capture these parameters in the method body for the ImageEncoder callback
            var capture = m_RgbSensorCapture;
            var future = m_Future;

            RenderTextureReader.Capture<Color32>(ctx, tempRT2, (frame, pixelData, rt) =>
            {
                RgbCaptureReadback?.Invoke(frame, pixelData);

                ImageEncoder.EncodeImage(pixelData, rt.width, rt.height,
                    rt.graphicsFormat, k_RgbImageEncodingFormat, encodedImageData =>
                {
                    using (s_WriteFrame.Auto())
                    {
                        capture.buffer = encodedImageData.ToArray();
                        future.Report(capture);
                    }
                });

                rt.Release();
            });
            Profiler.EndSample();
        }

        void CleanupVisualization()
        {
            if (s_VisualizedPerceptionCamera == this)
            {
                s_VisualizedPerceptionCamera = null;
            }
        }

#if URP_PRESENT
        internal void MarkGroundTruthRendererFeatureAsPresent()
        {
            // only used to confirm that GroundTruthRendererFeature is present in URP
            m_IsGroundTruthRendererFeaturePresent = true;
        }
#endif

        /// <summary>
        /// Convert the Unity 4x4 projection matrix to a 3x3 matrix
        /// </summary>
        // ReSharper disable once InconsistentNaming
        static float3x3 ToProjectionMatrix3x3(Matrix4x4 inMatrix)
        {
            return new float3x3(
                inMatrix[0, 0], inMatrix[0, 1], inMatrix[0, 2],
                inMatrix[1, 0], inMatrix[1, 1], inMatrix[1, 2],
                inMatrix[2, 0], inMatrix[2, 1], inMatrix[2, 2]);
        }
    }
}
