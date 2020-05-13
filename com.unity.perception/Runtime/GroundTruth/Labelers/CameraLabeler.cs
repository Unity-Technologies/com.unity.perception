using System;

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
        internal void InternalOnUpdate() => OnUpdate();
        internal void InternalOnBeginRendering() => OnBeginRendering();
        internal void InternalCleanup() => Cleanup();

        internal void Init(PerceptionCamera newPerceptionCamera)
        {
            try
            {
                this.perceptionCamera = newPerceptionCamera;
                sensorHandle = newPerceptionCamera.SensorHandle;
                Setup();
                isInitialized = true;
            }
            catch (Exception)
            {
                this.enabled = false;
                throw;
            }
        }
    }
}
