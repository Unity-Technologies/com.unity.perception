using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.Sensors;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Rendering;
#pragma warning disable 649

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Captures ground truth from the associated Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PerceptionCamera : MonoBehaviour
    {
        const float k_PanelWidth = 200;
        const float k_PanelHeight = 250;

        internal static PerceptionCamera visualizedPerceptionCamera;
        internal static HashSet<PerceptionCamera> enabledPerceptionCameras = new();

        internal HUDPanel hudPanel;
        internal OverlayPanel overlayPanel;

        [SerializeReference]
        CameraSensor m_CameraSensor = new UnityCameraSensor();
        [SerializeReference]
        List<CameraLabeler> m_Labelers = new();
        [SerializeField]
        LayerMask m_LayerMask = -1;

        bool m_BegunCapturingData;
        bool m_SimulationEnded;
        bool m_ShowingVisualizations;
        bool m_GUIStylesInitialized;
        int m_PrevBeginFrameCaptured = -1;
        int m_PrevEndFrameCaptured = -1;
        SensorHandle m_SensorHandle;
        Vector2 m_ScrollPosition;
        RgbSensorDefinition m_SensorDefinition;
        Dictionary<int, RgbSensor> m_RgbSensorCaptures = new();
        Dictionary<int, AsyncFuture<Sensor>> m_Futures = new();
        Dictionary<string, object> m_PersistentSensorData = new();
        List<CameraChannelBase> m_Channels = new();

        internal static Dictionary<int, SceneHierarchyInformation> savedHierarchies = new();

        /// <summary>
        /// Number of frames to wait before starting the scenario
        /// </summary>
#if UNITY_EDITOR
        public static int waitFrames = 2;
#else
        public static int waitFrames = 1;
#endif
        /// <summary>
        /// The number of capture-able frames that have been generated
        /// </summary>
        public static int captureFrameCount => Time.frameCount - waitFrames;

        /// <summary>
        /// Invoked when RenderedObjectInfos are calculated. The first parameter is the Time.frameCount at which the
        /// objects were rendered. This may be called many frames after the objects were rendered.
        /// </summary>
        public event Action<int, NativeArray<RenderedObjectInfo>, SceneHierarchyInformation> RenderedObjectInfosCalculated;

        /// <summary>
        /// The string ID of this camera sensor
        /// </summary>
        public string id = "camera";

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
        const ImageEncodingFormat k_RgbImageEncodingFormat = ImageEncodingFormat.Png;

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
        public bool showVisualizations = true;

        /// <summary>
        /// The minimum level of transparency that will be rendered per pixel in the segmentation image.
        /// </summary>
        [Tooltip("The minimum level of transparency that will be rendered per pixel in segmentation images.")]
        [Range(0f, 1f)]
        public float alphaThreshold = .1f;

        /// <summary>
        /// Whether to override the camera's LayerMask on its labelers.
        /// </summary>
        public bool overrideLayerMask;

        /// <summary>
        /// The LayerMask used for filtering objects before capturing labeler data.
        /// The <see cref="PerceptionCamera.overrideLayerMask"/> field must be enabled for this LayerMask to be used.
        /// The attached <see cref="Camera"/>'s <see cref="Camera.cullingMask"/> is used otherwise.
        /// </summary>
        public LayerMask layerMask
        {
            get
            {
                if (attachedCamera == null)
                {
                    attachedCamera = GetComponent<Camera>();
                }

                return overrideLayerMask ? m_LayerMask : attachedCamera.cullingMask;
            }
            set => m_LayerMask = value;
        }

        /// <summary>
        /// Whether to use accumulation before capture (e.g. Path Tracing)
        /// </summary>
        public bool useAccumulation;

        /// <summary>
        /// The <see cref="CameraSensor"/> used to generate the PerceptionCamera's output.
        /// </summary>
        public CameraSensor cameraSensor
        {
            get => m_CameraSensor;
            set
            {
                if (m_BegunCapturingData)
                    throw new InvalidOperationException(
                        "The camera sensor cannot be switched to a different sensor " +
                        "after the PerceptionCamera has begun capturing data.");
                m_CameraSensor = value;
            }
        }

        /// <summary>
        /// The <see cref="CameraLabeler"/> instances which will be run for this PerceptionCamera.
        /// </summary>
        public IReadOnlyList<CameraLabeler> labelers => m_Labelers;

        /// <summary>
        /// The currently enabled <see cref="CameraChannel{T}"/>s for this PerceptionCamera.
        /// </summary>
        public IReadOnlyList<CameraChannelBase> channels => m_Channels;

        /// <summary>
        /// Requests a capture from this PerceptionCamera on the next rendered frame.
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

        /// <summary>
        /// Enables a channel of the given type on the sensor.
        /// </summary>
        /// <typeparam name="T">The type of channel to enable.</typeparam>
        /// <returns>Newly created Channel</returns>
        public T EnableChannel<T>() where T : CameraChannelBase, new()
        {
            if (m_BegunCapturingData)
                throw new InvalidOperationException(
                    "Channels must be enabled before the sensor begins capturing data. " +
                    "Sensors begin capturing data when rendering starts during the frame the sensor is initialized.");
            if (!TryGetChannel<T>(out var channel))
            {
                channel = new T();
                m_Channels.Add(channel);
                channel.Initialize(this);
                channel.SetOutputTexture(cameraSensor.InternalSetupChannel(channel));
                return channel;
            }

            return channel;
        }

        /// <summary>
        /// Returns whether a channel of the given type has been enabled on the sensor.
        /// </summary>
        /// <typeparam name="T">The type of channel to check the enabled status of.</typeparam>
        /// <returns>The enabled status of the given channel.</returns>
        public bool IsChannelEnabled<T>() where T : CameraChannelBase
        {
            return m_Channels.Any(enabledChannel => enabledChannel.GetType() == typeof(T));
        }

        /// <summary>
        /// Returns the enabled channel of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of channel to get.</typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// Channel's must be enabled before they can be accessed.
        /// </exception>
        public T GetChannel<T>() where T : CameraChannelBase
        {
            if (!TryGetChannel<T>(out var channel))
                throw new InvalidOperationException(
                    "The requested channel must be enabled first before it can be accessed.");
            return channel;
        }

        /// <summary>
        /// Returns whether a channel of the given type is enabled, and additionally outputs the found channel.
        /// </summary>
        /// <param name="channel">The found channel of the given type.</param>
        /// <typeparam name="T">The type of channel to get.</typeparam>
        /// <returns>True if a channel of the given type is enabled, false if otherwise.</returns>
        public bool TryGetChannel<T>(out T channel) where T : CameraChannelBase
        {
            foreach (var enabledChannel in m_Channels)
            {
                if (enabledChannel.GetType() == typeof(T))
                {
                    channel = (T) enabledChannel;
                    return true;
                }
            }

            channel = null;
            return false;
        }

        internal void ClearNullLabelers()
        {
            m_Labelers?.RemoveAll(x => x == null);
        }

        internal void CheckFixedLengthScenarioHasEnoughFPI()
        {
            if (captureTriggerMode == CaptureTriggerMode.Scheduled)
            {
                var scenario = ScenarioBase.activeScenario as FixedLengthScenario;
                if (scenario)
                {
                    if (firstCaptureFrame >= scenario.framesPerIteration)
                    {
                        Debug.LogError($"The number of frames per Iteration of the {nameof(FixedLengthScenario)} on the object \"{scenario.name}\" is not large " +
                            $"enough to have any frames captured by the {nameof(PerceptionCamera)} on the object \"{name}\". " +
                            $"The camera's start frame is currently set to {firstCaptureFrame}. The number of frames per Iteration for the Scenario should be larger than this number ({firstCaptureFrame + 1} or more) for captures to happen.");
                    }
                }
            }
        }

        void Awake()
        {
            CheckHdrp();
            CheckFixedLengthScenarioHasEnoughFPI();
            attachedCamera = GetComponent<Camera>();
            cameraSensor.Setup(this);
            Application.runInBackground = true;
        }

        static void CheckHdrp()
        {
#if !HDRP_PRESENT
            Debug.LogError("Perception requires an HDRP project. Application will quit.");
            if (Application.isEditor)
                UnityEditor.EditorApplication.isPlaying = false;
            Application.Quit(-1);
#endif
        }

        void Start()
        {
            var channel = EnableChannel<RGBChannel>();
            if (captureRgbImages)
                channel.outputTextureReadback += OnRGBTextureOutputTextureReadback;

            SetupVisualizationCamera();
            DatasetCapture.SimulationEnding += OnSimulationEnding;
        }

        void OnEnable()
        {
            enabledPerceptionCameras.Add(this);
            cameraSensor.Enable();
            PerceptionUpdater.beginFrameRendering += OnBeginFrameRendering;
            PerceptionUpdater.endFrameRendering += OnEndFrameRendering;
        }

        void OnDisable()
        {
            enabledPerceptionCameras.Remove(this);
            cameraSensor.Disable();
            PerceptionUpdater.beginFrameRendering -= OnBeginFrameRendering;
            PerceptionUpdater.endFrameRendering -= OnEndFrameRendering;
        }

        void OnValidate()
        {
            if (m_Labelers == null)
            {
                m_Labelers = new List<CameraLabeler>();
            }
            if (m_CameraSensor == null)
            {
                m_CameraSensor = new UnityCameraSensor();
            }
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

        void OnDestroy()
        {
            DatasetCapture.SimulationEnding -= OnSimulationEnding;

            OnSimulationEnding();
            CleanupVisualization();

            if (SensorHandle.IsValid)
                SensorHandle.Dispose();

            SensorHandle = default;

            cameraSensor.InternalCleanup();

            foreach (var channel in m_Channels)
            {
                if (channel.outputTexture != null)
                    channel.outputTexture.Release();
                if (channel is IPostProcessChannel postProcessChannel)
                    if (postProcessChannel.preprocessTexture != null)
                        postProcessChannel.preprocessTexture.Release();
            }
        }

        void EnsureSensorRegistered()
        {
            if (m_SensorHandle.IsNil)
            {
                m_SensorDefinition = new RgbSensorDefinition(id, captureTriggerMode, description, firstCaptureFrame,
                    framesBetweenCaptures, manualSensorAffectSimulationTiming, "camera", simulationDeltaTime, useAccumulation);
                SensorHandle = DatasetCapture.RegisterSensor(m_SensorDefinition);
            }
        }

        void SetupVisualizationCamera()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            showVisualizations = false;
#else
            var visualizationAllowed = visualizedPerceptionCamera == null;

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
            visualizedPerceptionCamera = this;

            hudPanel = gameObject.AddComponent<HUDPanel>();
            overlayPanel = gameObject.AddComponent<OverlayPanel>();
            overlayPanel.perceptionCamera = this;
#endif
        }

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
            AsyncGPUReadback.WaitAllRequests();
            ImageEncoder.WaitForAllEncodingJobsToComplete();

            m_SimulationEnded = true;
            foreach (var labeler in m_Labelers)
            {
                if (labeler.isInitialized)
                    labeler.InternalCleanup();
            }
        }

        static void CaptureHierarchy()
        {
            // multiple perception cameras should not calculate the scene hierarchy multiple times
            // but keep track of how many subscribed so we know when to dispose off the hierarchy information
            if (!savedHierarchies.ContainsKey(Time.frameCount))
                savedHierarchies[Time.frameCount] = EnvironmentUtilities.ParseHierarchyFromAllScenes();

            savedHierarchies[Time.frameCount].subscribers += 1;
        }

        void OnBeginFrameRendering(ScriptableRenderContext ctx)
        {
            // Sensors and channels are allowed to continue rendering even when the editor is paused to enable
            // compatibility with the frame debugger editor window.
            cameraSensor.BeginFrameRendering(ctx);

            if (!ShouldCaptureThisFrame())
                return;

            // When play mode is paused, the current frame can continue to be repeatedly rendered in the editor.
            // This check ensures that the current frame is only captured once for each unique frame.
            if (m_PrevBeginFrameCaptured == Time.frameCount)
                return;
            m_PrevBeginFrameCaptured = Time.frameCount;

            m_BegunCapturingData = true;
            CaptureHierarchy();
            RegisterNewCapture();
            CallOnLabelers(l => l.InternalOnBeginRendering(ctx));
        }

        void OnEndFrameRendering(ScriptableRenderContext ctx)
        {
            // Sensors and channels are allowed to continue rendering even when the editor is paused to enable
            // compatibility with the frame debugger editor window.
            cameraSensor.EndFrameRendering(ctx);

            if (!ShouldCaptureThisFrame())
                return;

            // When play mode is paused, the current frame can continue to be repeatedly rendered in the editor.
            // This check ensures that the current frame is only captured once for each unique frame.
            if (m_PrevEndFrameCaptured == Time.frameCount)
                return;
            m_PrevEndFrameCaptured = Time.frameCount;

            // Signal the labelers to perform their end of frame operations.
            CallOnLabelers(l => l.InternalOnEndRendering(ctx));

            // Invoke channel readback events.
            var cmd = CommandBufferPool.Get($"PerceptionCamera {id} Readbacks");
            foreach (var channel in m_Channels)
                channel.InvokeReadbackEvent(cmd);

            // Invoke RenderedObjectInfosCalculated subscribers.
            if (RenderedObjectInfosCalculated != null)
            {
                if (!IsChannelEnabled<InstanceIdChannel>())
                    throw new InvalidOperationException(
                        $"The {nameof(InstanceIdChannel)} must be enabled " +
                        "before subscribing to the RenderedObjectInfosCalculated event.");
                var texture = GetChannel<InstanceIdChannel>().outputTexture;
                RenderedObjectInfoComputer.CalculateRenderedObjectInfos(cmd, texture, RenderedObjectInfosCalculated);
            }

            // Execute the enqueued end-of-frame operations.
            ctx.ExecuteCommandBuffer(cmd);
            ctx.Submit();
        }

        void RegisterNewCapture()
        {
            var trans = transform;
            var sensorIntrinsics = cameraSensor.intrinsics;
            var capture = new RgbSensor(m_SensorDefinition)
            {
                position = trans.position,
                rotation = trans.rotation,
                projection = sensorIntrinsics.projection,
                dimension = new Vector2(cameraSensor.pixelWidth, cameraSensor.pixelHeight),
                matrix = sensorIntrinsics.matrix,
                imageEncodingFormat = k_RgbImageEncodingFormat
            };

            if (!captureRgbImages)
                SensorHandle.ReportSensor(capture);
            else
            {
                var frame = Time.frameCount;
                m_RgbSensorCaptures[frame] = capture;
                m_Futures[frame] = SensorHandle.ReportSensorAsync();
            }
        }

        void OnRGBTextureOutputTextureReadback(int frame, NativeArray<Color32> data)
        {
            var capture = m_RgbSensorCaptures[frame];
            m_RgbSensorCaptures.Remove(frame);

            var future = m_Futures[frame];
            m_Futures.Remove(frame);

            var outputTexture = GetChannel<RGBChannel>().outputTexture;
            ImageEncoder.EncodeImage(data, outputTexture.width, outputTexture.height,
                outputTexture.graphicsFormat, k_RgbImageEncodingFormat, encodedImageData =>
                {
                    capture.buffer = encodedImageData.ToArray();
                    future.Report(capture);
                }
            );
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

        bool ShouldCaptureThisFrame()
        {
            return !m_SimulationEnded && SensorHandle.ShouldCaptureThisFrame;
        }

        void CleanupVisualization()
        {
            if (visualizedPerceptionCamera == this)
            {
                visualizedPerceptionCamera = null;
            }
        }
    }
}
