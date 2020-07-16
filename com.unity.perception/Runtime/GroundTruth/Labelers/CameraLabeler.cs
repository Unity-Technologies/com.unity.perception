using System;
using Unity.Simulation;

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
        protected bool supportsVisualization = false;
        /// <summary>
        /// Flag holding if the realtime visualizer for the labeler in enabled. This
        /// value is ignored if <see cref="supportsVisualization"/> is false.
        /// </summary>
        private bool visualizationEnabled = false;
        /// <summary>
        /// Static counter shared by all lableler classes. If any visualizers are
        /// active then perception camera cannot operate in asynchronous mode. 
        /// </summary>
        private static int activeVisualizers = 0;

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
        /// </summary>
        protected virtual void SetupVisualizationPanel(GameObject panel) { }
        /// <summary>
        /// Called when the labeler's visualization capability is turned on or off.
        /// </summary>
        protected virtual void OnVisualizerEnabled(bool enabled) {}
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
        internal void InternalSetupVisualizationPanel(GameObject panel) => SetupVisualizationPanel(panel);
        internal void InternalVisualizerEnabled(bool enabled) => OnVisualizerEnabled(enabled);
        internal void InternalOnUpdate() => OnUpdate();
        internal void InternalOnBeginRendering() => OnBeginRendering();
        internal void InternalCleanup() => Cleanup();

        /// <summary>
        /// Turns on/off the labeler's realtime visualization capability. If a labeler
        /// does not support realtime visualization (<see cref="supportsVisualization"/>)
        /// this will not function.
        /// </summary>
        protected void EnableVisualization(bool enabled)
        {
            if (!supportsVisualization) return;

            if (enabled != visualizationEnabled)
            {
                visualizationEnabled = enabled;
                
                if (enabled)
                    activeVisualizers++;
                else
                    activeVisualizers--;

                if (activeVisualizers > 0)
                    CaptureOptions.useAsyncReadbackIfSupported = false;
                else
                    CaptureOptions.useAsyncReadbackIfSupported = true;

                OnVisualizerEnabled(enabled);
            }
        }

        /// <summary>
        /// Is the visualization capability of the labeler currently active.
        /// </summary>
        protected bool IsVisualizationEnabled()
        {
            return supportsVisualization && visualizationEnabled;
        }

        internal void Init(PerceptionCamera newPerceptionCamera)
        {
            try
            {
                this.perceptionCamera = newPerceptionCamera;
                sensorHandle = newPerceptionCamera.SensorHandle;
                Setup();
                isInitialized = true;

                if (supportsVisualization)
                {
                    InitPanel();
                }
            }
            catch (Exception)
            {
                this.enabled = false;
                throw;
            }
        }

        internal void InitPanel()
        {
            var canvas = GameObject.Find("Canvas");
            
            if (canvas == null)
            {
               canvas = GameObject.Instantiate(Resources.Load<GameObject>("Canvas"));
               canvas.name = "Canvas";
            }
            
            var panel = canvas.transform.Find("VisualizationPanel");

            if (panel == null)
            {
                panel = GameObject.Instantiate(Resources.Load<GameObject>("VisualizationPanel")).transform;
                (panel as RectTransform).SetParent(canvas.transform);
            }

            SetupVisualizationPanel(panel.gameObject);
        }
    }
}
