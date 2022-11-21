using System;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors
{
    /// <summary>
    /// Derive this class to define a new sensor that generates RGB data and it's associated
    /// channel information (e.g. depth, normals, segmentation, etc.).
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public abstract class CameraSensor
    {
        /// <summary>
        /// The <see cref="PerceptionCamera"/> component capturing the output of this sensor.
        /// </summary>
        protected PerceptionCamera perceptionCamera { get; private set; }

        /// <summary>
        /// The pixel width of the sensor output.
        /// </summary>
        public abstract int pixelWidth { get; }

        /// <summary>
        /// The pixel height of the sensor output.
        /// </summary>
        public abstract int pixelHeight { get; }

        /// <summary>
        /// The intrinsic properties of the RGB sensor such as the field of view and projection type.
        /// </summary>
        public abstract CameraSensorIntrinsics intrinsics { get; }

        /// <summary>
        /// Configures newly enabled channels and returns the output texture of the enabled channel.
        /// </summary>
        /// <param name="channel">The channel instance being enabled through this sensor.</param>
        /// <typeparam name="T">The type of channel to configure.</typeparam>
        /// <returns>The output texture of the enabled channel.</returns>
        protected abstract RenderTexture SetupChannel<T>(T channel) where T : CameraChannelBase, new();

        /// <summary>
        /// Setup is called when the PerceptionCamera capturing data from this sensor is initialized (on Awake).
        /// </summary>
        protected virtual void Setup() {}

        /// <summary>
        /// Cleanup is called when the PerceptionCamera capturing data from this sensor is destroyed.
        /// </summary>
        protected virtual void Cleanup() {}

        /// <summary>
        /// OnEnable is called when the PerceptionCamera capturing data from this sensor is enabled.
        /// </summary>
        protected virtual void OnEnable() {}

        /// <summary>
        /// OnDisable is called when the PerceptionCamera capturing data from this sensor is disabled.
        /// </summary>
        protected virtual void OnDisable() {}

        /// <summary>
        /// OnBeginFrameRendering is called just before rendering has begun for the current frame.
        /// </summary>
        /// <param name="ctx">The scriptable render context for the current frame.</param>
        protected virtual void OnBeginFrameRendering(ScriptableRenderContext ctx) {}

        /// <summary>
        /// OnEndFrameRendering is called just after rendering has ended for the current frame.
        /// </summary>
        /// <param name="ctx">The scriptable render context for the current frame.</param>
        protected virtual void OnEndFrameRendering(ScriptableRenderContext ctx) {}

        internal RenderTexture InternalSetupChannel<T>(T channel) where T : CameraChannelBase, new()
        {
            return SetupChannel(channel);
        }

        internal void Setup(PerceptionCamera camera)
        {
            perceptionCamera = camera;
            Setup();
        }

        internal void InternalCleanup() => Cleanup();

        internal void Enable() => OnEnable();

        internal void Disable() => OnDisable();

        internal void BeginFrameRendering(ScriptableRenderContext ctx) => OnBeginFrameRendering(ctx);

        internal void EndFrameRendering(ScriptableRenderContext ctx) => OnEndFrameRendering(ctx);
    }
}
