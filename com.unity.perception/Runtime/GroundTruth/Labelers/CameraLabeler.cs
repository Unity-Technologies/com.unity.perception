using System;
using Unity.Simulation;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

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
        /// A human-readable description of the labeler
        /// </summary>
        public abstract string description { get; protected set; }

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
        /// The heads up display (HUD) panel. Generally used to add stats to the display.
        /// </summary>
        public HUDPanel hudPanel => perceptionCamera != null ? perceptionCamera.hudPanel : null;

        /// <summary>
        /// The overlay panel. Used to control which full screen image visual is displayed.
        /// </summary>
        public OverlayPanel overlayPanel => perceptionCamera != null ? perceptionCamera.overlayPanel : null;

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
        /// Called when the labeler's visualization capability is turned on or off.
        /// </summary>
        /// <param name="visualizerEnabled">The current enabled state of the visualizer</param>
        protected virtual void OnVisualizerEnabledChanged(bool visualizerEnabled) {}
        /// <summary>
        /// Called during the Update each frame the the labeler is enabled and <see cref="SensorHandle.ShouldCaptureThisFrame"/> is true.
        /// </summary>
        protected virtual void OnUpdate() {}
        /// <summary>
        /// Called just before the camera renders each frame the the labeler is enabled and <see cref="SensorHandle.ShouldCaptureThisFrame"/> is true.
        /// </summary>
        protected virtual void OnBeginRendering() {}
        /// <summary>
        /// Labeling pass to display labeler's visualization components, if applicable. Important note, all labeler's visualizations need
        /// to use Unity's Immediate Mode GUI (IMGUI) <see cref="https://docs.unity3d.com/Manual/GUIScriptingGuide.html"/> system.
        /// This called is triggered from <see cref="perceptionCamera.OnGUI"/> call. This call happens immediately before <see cref="OnVisualizeAdditionalUI"/>
        /// so that the visualization components are drawn below the UI elements.
        /// </summary>
        protected virtual void OnVisualize() {}
        /// <summary>
        /// In this pass, a labeler can add custom GUI controls to the scene. Important note, all labeler's additional
        /// GUIs need to use Unity's Immediate Mode GUI (IMGUI) <see cref="https://docs.unity3d.com/Manual/GUIScriptingGuide.html"/> system.
        /// This called is triggered from <see cref="perceptionCamera.OnGUI"/> call. This call happens immediately after the <see cref="OnVisualize"/>
        /// so that the visualization components are drawn below the UI elements.
        /// </summary>
        protected virtual void OnVisualizeAdditionalUI() {}

        /// <summary>
        /// Called when the Labeler is about to be destroyed or removed from the PerceptionCamera. Use this to clean up to state.
        /// </summary>
        protected virtual void Cleanup() {}

        internal void InternalSetup() => Setup();

        internal bool InternalVisualizationEnabled
        {
            get => visualizationEnabled;
            set => visualizationEnabled = value;
        }
        internal void InternalOnUpdate() => OnUpdate();
        internal void InternalOnBeginRendering() => OnBeginRendering();
        internal void InternalCleanup() => Cleanup();
        internal void InternalVisualize() => OnVisualize();

        private bool m_ShowVisualizations = false;

        /// <summary>
        /// Turns on/off the labeler's realtime visualization capability. If a labeler does not support realtime
        /// visualization (<see cref="supportsVisualization"/>) or visualization is not enabled on the PerceptionCamera
        /// this will not function.
        /// </summary>
        internal bool visualizationEnabled
        {
            get
            {
                return supportsVisualization && m_ShowVisualizations;
            }
            set
            {
                if (!supportsVisualization) return;

                if (value != m_ShowVisualizations)
                {
                    m_ShowVisualizations = value;

                    OnVisualizerEnabledChanged(m_ShowVisualizations);
                }
            }
        }

        internal void VisualizeUI()
        {
            if (supportsVisualization && !(this is IOverlayPanelProvider))
            {
                GUILayout.Label(GetType().Name);
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.Label("Enabled");
                GUILayout.FlexibleSpace();
                visualizationEnabled = GUILayout.Toggle(visualizationEnabled, "");
                GUILayout.EndHorizontal();
                if (visualizationEnabled) OnVisualizeAdditionalUI();
            }
        }

        internal void Visualize()
        {
            if (visualizationEnabled) OnVisualize();
        }

        internal void Init(PerceptionCamera newPerceptionCamera)
        {
            try
            {
                this.perceptionCamera = newPerceptionCamera;
                sensorHandle = newPerceptionCamera.SensorHandle;
                Setup();
                isInitialized = true;

                m_ShowVisualizations = supportsVisualization && perceptionCamera.showVisualizations;
            }
            catch (Exception)
            {
                this.enabled = false;
                throw;
            }
        }
    }
}
