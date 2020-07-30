using System;
using Unity.Simulation;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Abstract class for defining custom annotation and metric generation to be run by <see cref="PerceptionCamera"/>.
    /// Instances of CameraLabeler on <see cref="PerceptionCamera.labelers"/> will be invoked each frame the camera is
    /// set to capture data (see <see cref="SensorHandle.ShouldCaptureThisFrame"/>).
    /// </summary>
    [Serializable]
    public abstract class CameraLabeler
    {
        /// <summary>
        /// Whether the CameraLabeler should be set up and called each frame.
        /// </summary>
        public bool enabled = true;

        internal bool isInitialized { get; private set; }

        /// <summary>
        /// Labelers should set this in their setup to define if they support realtime
        /// visualization of their data.
        /// </summary>
        protected abstract bool supportsVisualization
        {
            get;
        }

        /// <summary>
        /// Retrieve a handle to the visualization canvas <see cref="VisualizationCanvas"/>. This is the specific canvas that all visualization
        /// labelers should be added to. The canvas has helper functions to create many common visualization components.
        /// </summary>
        public VisualizationCanvas visualizationCanvas { get; private set; }

        /// <summary>
        /// The control panel that is attached to the visualization canvas. The common location to add interactive controls.
        /// </summary>
        public ControlPanel controlPanel => visualizationCanvas != null ? visualizationCanvas.controlPanel : null;

        /// <summary>
        /// The heads up display (HUD) panel. Generally used to add stats to the display.
        /// </summary>
        public HUDPanel hudPanel => visualizationCanvas != null ? visualizationCanvas.hudPanel : null;

        /// <summary>
        /// The <see cref="PerceptionCamera"/> that contains this labeler.
        /// </summary>
        protected PerceptionCamera perceptionCamera { get; private set; }

        /// <summary>
        /// The SensorHandle for the <see cref="PerceptionCamera"/> that contains this labeler. Use this to report
        /// annotations and metrics.
        /// </summary>
        protected SensorHandle sensorHandle { get; private set; }

        /// <summary>
        /// Called just before the first call to <see cref="OnUpdate"/> or <see cref="OnBeginRendering"/>. Implement this
        /// to initialize state.
        /// </summary>
        protected virtual void Setup() { }
        /// <summary>
        /// Called immediately after <see cref="setup"/>. Implement this to initialize labeler's visualization
        /// capability if one exists <see cref="supportVisualization"/>.
        /// <param name="panel">The target control panel for the labeler's visualization component</param>
        /// </summary>
        protected virtual void PopulateVisualizationPanel(ControlPanel panel) { }
        /// <summary>
        /// Called when the labeler's visualization capability is turned on or off.
        /// <param name="enabled">The current enabled state of the visualizer</param>
        /// </summary>
        protected virtual void OnVisualizerEnabledChanged(bool enabled) {}
        /// <summary>
        /// Called during the Update each frame the the labeler is enabled and <see cref="SensorHandle.ShouldCaptureThisFrame"/> is true.
        /// </summary>
        protected virtual void OnUpdate() {}
        /// <summary>
        /// Called just before the camera renders each frame the the labeler is enabled and <see cref="SensorHandle.ShouldCaptureThisFrame"/> is true.
        /// </summary>
        protected virtual void OnBeginRendering() {}

        /// <summary>
        /// Called when the Labeler is about to be destroyed or removed from the PerceptionCamera. Use this to clean up to state.
        /// </summary>
        protected virtual void Cleanup() {}

        internal void InternalSetup() => Setup();
        internal void InternalPopulateVisualizationPanel(GameObject panel) => PopulateVisualizationPanel(controlPanel);

        internal bool InternalVisualizationEnabled
        {
            get => visualizationEnabled;
            set => visualizationEnabled = value;
        }
        internal void InternalOnUpdate() => OnUpdate();
        internal void InternalOnBeginRendering() => OnBeginRendering();
        internal void InternalCleanup() => Cleanup();

        private bool m_ShowVisualizations = false;

        /// <summary>
        /// Turns on/off the labeler's realtime visualization capability. If a labeler does not support realtime
        /// visualization (<see cref="supportsVisualization"/>) or visualization is not enabled on the PerceptionCamera
        /// this will not function.
        /// </summary>
        protected bool visualizationEnabled
        {
            get
            {
                return supportsVisualization && m_ShowVisualizations;
            }
            set
            {
                if (!supportsVisualization) return;
                if (visualizationCanvas == null) return;

                if (value != m_ShowVisualizations)
                {
                    m_ShowVisualizations = value;

                    OnVisualizerEnabledChanged(m_ShowVisualizations);
                }
            }
        }

        internal void Init(PerceptionCamera newPerceptionCamera, VisualizationCanvas visualizationCanvas)
        {
            try
            {
                this.perceptionCamera = newPerceptionCamera;
                sensorHandle = newPerceptionCamera.SensorHandle;
                this.visualizationCanvas = visualizationCanvas;
                Setup();
                isInitialized = true;

                if (supportsVisualization && visualizationCanvas != null)
                {
                    m_ShowVisualizations = true;
                    InitVisualizationUI();
                }
            }
            catch (Exception)
            {
                this.enabled = false;
                throw;
            }
        }

        private void InitVisualizationUI()
        {
            PopulateVisualizationPanel(controlPanel);
        }
    }
}
