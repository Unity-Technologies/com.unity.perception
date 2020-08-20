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
using UnityEngine.Serialization;
using UnityEngine.UI;
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

        int m_LastFrameCaptured = -1;
        Ego m_EgoMarker;

#pragma warning disable 414
        //only used to confirm that GroundTruthRendererFeature is present in URP
        bool m_GroundTruthRendererFeatureRun;
#pragma warning restore 414

        static PerceptionCamera s_VisualizedPerceptionCamera;

        /// <summary>
        /// Turns on/off the realtime visualization capability.
        /// </summary>
        [SerializeField]
        public bool showVisualizations = true;

        bool m_ShowingVisualizations;

        /// <summary>
        /// The <see cref="SensorHandle"/> associated with this camera. Use this to report additional annotations and metrics at runtime.
        /// </summary>
        public SensorHandle SensorHandle { get; private set; }

        static ProfilerMarker s_WriteFrame = new ProfilerMarker("Write Frame (PerceptionCamera)");
        static ProfilerMarker s_EncodeAndSave = new ProfilerMarker("Encode and save (PerceptionCamera)");


#if URP_PRESENT
        internal List<ScriptableRenderPass> passes = new List<ScriptableRenderPass>();
        public void AddScriptableRenderPass(ScriptableRenderPass pass)
        {
            passes.Add(pass);
        }
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

            AsyncRequest.maxJobSystemParallelism = 0; // Jobs are not chained to one another in any way, maximizing parallelism
            AsyncRequest.maxAsyncRequestFrameAge = 4; // Ensure that readbacks happen before Allocator.TempJob allocations get stale

            SetupInstanceSegmentation();
            var cam = GetComponent<Camera>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SetupVisualizationCamera(cam);
#endif

            DatasetCapture.SimulationEnding += OnSimulationEnding;
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += CheckForRendererFeature;
        }

        internal HUDPanel hudPanel = null;

        void SetupVisualizationCamera(Camera cam)
        {
            var visualizationAllowed = s_VisualizedPerceptionCamera == null;

            if (!visualizationAllowed && showVisualizations)
            {
                Debug.LogWarning($"Currently only one PerceptionCamera may be visualized at a time. Disabling visualization on {gameObject.name}.");
                showVisualizations = false;
                return;
            }
            if (!showVisualizations)
                return;

            m_ShowingVisualizations = true;
            s_VisualizedPerceptionCamera = this;

            hudPanel = gameObject.AddComponent<HUDPanel>();
        }

        void CheckForRendererFeature(ScriptableRenderContext context, Camera camera)
        {
            if (camera == GetComponent<Camera>())
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

            var cam = GetComponent<Camera>(); // TODO I don't like this... Get the camera handle, we should have it
            cam.enabled = SensorHandle.ShouldCaptureThisFrame;

            bool anyVisualizing = false;
            foreach (var labeler in m_Labelers)
            {
                if (!labeler.enabled)
                    continue;

                if (!labeler.isInitialized)
                {
                    labeler.Init(this);
                }

                labeler.InternalOnUpdate();
                anyVisualizing |= labeler.InternalVisualizationEnabled;
            }

            // Currently there is an issue in the perception camera that causes the UI layer not to be visualized
            // if we are utilizing async readback and we have to flip our captured image. We have created a jira
            // issue for this (aisv-779) and have notified the engine team about this.
            anyVisualizing = true;

            if (m_ShowingVisualizations)
                CaptureOptions.useAsyncReadbackIfSupported = !anyVisualizing;
        }

        private Vector2 scrollPosition;

        private const float panelWidth = 200;
        private const float panelHeight = 250;

        private void SetUpGUIStyles()
        {
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.font = Resources.Load<Font>("Inter-Light");
            GUI.skin.label.padding = new RectOffset(0, 0, 1, 1);
            GUI.skin.label.margin = new RectOffset(0, 0, 1, 1);
            GUI.skin.box.padding = new RectOffset(5, 5, 5, 5);
            GUI.skin.toggle.margin = new RectOffset(0, 0, 0, 0);
            GUI.skin.horizontalSlider.margin = new RectOffset(0, 0, 0, 0);
            m_GUIStylesInitialized = true;
        }

        private bool m_GUIStylesInitialized = false;

        private void DisplayNoLabelersMessage()
        {
            var x = Screen.width - panelWidth - 10;
            var height = Math.Min(Screen.height * 0.5f - 20, 90);

            GUILayout.BeginArea(new Rect(x, 10, panelWidth, height), GUI.skin.box);

            GUILayout.Label("Visualization: No labelers are currently active. Enable at least one labeler from the inspector window of your perception camera to see visualizations.");

            // If a labeler has never been initialized then it was off from the
            // start, it should not be called to draw on the UI
            foreach (var labeler in m_Labelers.Where(labeler => labeler.isInitialized))
            {
                labeler.VisualizeUI();
                GUILayout.Space(4);
            }

            GUILayout.EndArea();
        }

        private void OnGUI()
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

            var x = Screen.width - panelWidth - 10;
            var height = Math.Min(Screen.height * 0.5f - 20, panelHeight);

            GUILayout.BeginArea(new Rect(x, 10, panelWidth, height), GUI.skin.box);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            // If a labeler has never been initialized then it was off from the
            // start, it should not be called to draw on the UI
            foreach (var labeler in m_Labelers.Where(labeler => labeler.isInitialized))
            {
                labeler.VisualizeUI();
                GUILayout.Space(4);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
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

            var captureFilename = $"{Manager.Instance.GetDirectoryFor(RgbDirectory)}/{s_RgbFilePrefix}{Time.frameCount}.png";
            var dxRootPath = $"{RgbDirectory}/{s_RgbFilePrefix}{Time.frameCount}.png";
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

                    byte[] encodedData;
                    using (s_EncodeAndSave.Auto())
                    {
                        encodedData = ImageConversion.EncodeArrayToPNG(dataColorBuffer, GraphicsFormat.R8G8B8A8_UNorm, (uint)width, (uint)height);
                    }

                    return !FileProducer.Write(captureFilename, encodedData) ? AsyncRequest.Result.Error : AsyncRequest.Result.Completed;
                }
            };

            CaptureCamera.Capture(cam, colorFunctor, flipY: flipY);

            Profiler.EndSample();
        }

        // ReSharper disable once ParameterHidesMember
        bool ShouldFlipY(Camera camera)
        {

#if HDRP_PRESENT
            var hdAdditionalCameraData = GetComponent<HDAdditionalCameraData>();

            //Based on logic in HDRenderPipeline.PrepareFinalBlitParameters
            return hdAdditionalCameraData.flipYMode == HDAdditionalCameraData.FlipYMode.ForceFlipY || (camera.targetTexture == null && camera.cameraType == CameraType.Game);
#elif URP_PRESENT
            return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal) &&
                (camera.targetTexture == null && camera.cameraType == CameraType.Game);
#else
            return false;
#endif
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
            //there are cases when OnBeginCameraRendering is called multiple times in the same frame. Ignore the subsequent calls.
            if (m_LastFrameCaptured == Time.frameCount)
                return;

            m_LastFrameCaptured = Time.frameCount;
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
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
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

        void CleanupVisualization()
        {
            if (s_VisualizedPerceptionCamera == this)
            {
                s_VisualizedPerceptionCamera = null;
            }
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

        internal void OnGroundTruthRendererFeatureRun()
        {
            //only used to confirm that GroundTruthRendererFeature is present in URP
            m_GroundTruthRendererFeatureRun = true;
        }
    }
}
