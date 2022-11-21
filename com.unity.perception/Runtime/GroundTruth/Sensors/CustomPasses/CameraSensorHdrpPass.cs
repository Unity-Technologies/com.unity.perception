#if HDRP_PRESENT
using System;
using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.Sensors;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A custom pass used for rendering CameraSensorChannels.
    /// </summary>
    class CameraSensorHdrpPass : CustomPass
    {
        Camera m_TargetCamera;
        List<(CameraChannelBase, RenderTexture)> m_ChannelTargets = new();

        /// <summary>
        /// Initialize a new CameraSensorHdrpPass.
        /// </summary>
        /// <param name="targetCamera">The camera used to render this pass's channels.</param>
        public CameraSensorHdrpPass(Camera targetCamera)
        {
            name = "CameraSensorHdrpPass";
            m_TargetCamera = targetCamera;

            // Add this CameraSensorHdrpPass to a CustomPassVolume attached to the target camera.
            var gameObject = targetCamera.gameObject;
            if (!gameObject.TryGetComponent<CustomPassVolume>(out var customPassVolume) ||
                customPassVolume.injectionPoint != CustomPassInjectionPoint.AfterPostProcess)
                customPassVolume = gameObject.AddComponent<CustomPassVolume>();
            customPassVolume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
            customPassVolume.isGlobal = false;
            customPassVolume.targetCamera = targetCamera;
            customPassVolume.customPasses.Add(this);
            customPassVolume.hideFlags |= HideFlags.HideInInspector;
        }

        /// <inheritdoc/>
        protected override void Execute(CustomPassContext customPassContext)
        {
            var camera = customPassContext.hdCamera.camera;
            if (camera != m_TargetCamera)
                return;

            var cmd = customPassContext.cmd;
            foreach (var channelTarget in m_ChannelTargets)
            {
                var channel = channelTarget.Item1;
                var renderTarget = channelTarget.Item2;
                using (new ProfilingScope(cmd, new ProfilingSampler($"{channel.GetType().Name}")))
                {
                    channel.Execute(new CameraChannelInputs
                    {
                        ctx = customPassContext.renderContext,
                        cmd = cmd,
                        camera = m_TargetCamera,
                        cameraColorBuffer = customPassContext.cameraColorBuffer,
                        cullingResults = customPassContext.cullingResults
                    }, renderTarget);
                }
            }
        }

        /// <summary>
        /// Adds a channel and it's render target to the list of channels to render within this pass.
        /// </summary>
        /// <param name="channel">The channel to render within this pass.</param>
        /// <param name="outputTexture">The output texture to render this channel to.</param>
        /// <exception cref="InvalidOperationException">
        /// A channel of the particular type cannot be rendered by this pass more than once.
        /// </exception>
        public void AddChannel(CameraChannelBase channel, RenderTexture outputTexture)
        {
            foreach (var channelTarget in m_ChannelTargets)
                if (channelTarget.Item1.GetType() == channel.GetType())
                    throw new InvalidOperationException("A channel of this type has already been added to this pass.");
            m_ChannelTargets.Add((channel, outputTexture));
        }
    }
}
#endif
