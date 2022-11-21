using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Perception.GroundTruth.Utilities;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors
{
    /// <summary>
    /// An RGB sensor that sources its RGB output directly from the Unity Camera component
    /// attached to same GameObject as the PerceptionCamera.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class UnityCameraSensor : CameraSensor
    {
        Dictionary<CameraChannelBase, RenderTexture> m_SuperSamplingTextures = new();
        Camera m_SuperSamplingCamera;

#if HDRP_PRESENT
        CameraSensorHdrpPass m_CameraSensorPass;
#endif

        /// <summary>
        /// Averages multiple samples per pixel to enable a high quality anti-aliasing effect.
        /// Note that this field will not influence the final output resolution of this sensor.
        /// </summary>
        [Tooltip("Averages multiple samples per pixel to enable a high quality anti-aliasing effect. " +
            "Note that this field will not influence the final output resolution of this sensor.")]
        public SuperSamplingFactor superSamplingFactor = SuperSamplingFactor.None;

        /// <inheritdoc/>
        public override int pixelWidth => perceptionCamera.attachedCamera.pixelWidth;

        /// <inheritdoc/>
        public override int pixelHeight => perceptionCamera.attachedCamera.pixelHeight;

        /// <summary>
        /// The pixel width of the super resolution sensor.
        /// </summary>
        public int superResWidth => perceptionCamera.attachedCamera.pixelWidth * (int)superSamplingFactor;

        /// <summary>
        /// The pixel height of the super resolution sensor.
        /// </summary>
        public int superResHeight => perceptionCamera.attachedCamera.pixelHeight * (int)superSamplingFactor;

        /// <inheritdoc/>
        public override CameraSensorIntrinsics intrinsics => new CameraSensorIntrinsics
        {
            projection = perceptionCamera.attachedCamera.orthographic ? "Isometric" : "Perspective",
            matrix = Matrix4X4ToMatrix3X3(perceptionCamera.attachedCamera.projectionMatrix)
        };

        /// <inheritdoc/>
        protected override void Setup()
        {
            var activeCamera = perceptionCamera.attachedCamera;
            if (superSamplingFactor != SuperSamplingFactor.None)
            {
                m_SuperSamplingCamera = CameraUtilities.DuplicateCamera(perceptionCamera.attachedCamera);
                m_SuperSamplingCamera.name = $"{perceptionCamera.name} - Super Resolution";
                var superResOutputTexture = new RenderTexture(
                    superResWidth, superResHeight, 32, GraphicsFormat.R8G8B8A8_UNorm)
                {
                    enableRandomWrite = true,
                    filterMode = FilterMode.Bilinear
                };
                m_SuperSamplingCamera.targetTexture = superResOutputTexture;

                CameraUtilities.ConvertToPassThroughCamera(perceptionCamera.attachedCamera);
                activeCamera = m_SuperSamplingCamera;
            }

#if HDRP_PRESENT
            m_CameraSensorPass = new CameraSensorHdrpPass(activeCamera);
#endif
        }

        /// <inheritdoc/>
        protected override void Cleanup()
        {
            foreach (var pair in m_SuperSamplingTextures)
                pair.Value.Release();
        }

        /// <inheritdoc/>
        protected override RenderTexture SetupChannel<T>(T channel)
        {
            // The texture that will be rendered to in the custom pass.
            RenderTexture channelTexture;

            // The final output texture of the channel.
            // This texture could be different from the preprocessedTexture because some channels require a post
            // processing step on their preprocessedTexture before writing to their postProcessedTexture.
            RenderTexture postProcessedTexture;

            if (superSamplingFactor == SuperSamplingFactor.None)
            {
                // Create the channel's output texture.
                if (channel is RGBChannel)
                {
                    var targetTexture = perceptionCamera.attachedCamera.targetTexture;
                    postProcessedTexture = targetTexture == null
                        ? channel.CreateOutputTexture(pixelWidth, pixelHeight) : targetTexture;
                }
                else
                {
                    postProcessedTexture = channel.CreateOutputTexture(pixelWidth, pixelHeight);
                }

                // Check if the channel has a post processing step.
                if (channel is IPostProcessChannel postProcessChannel)
                {
                    channelTexture = postProcessChannel.CreatePreprocessTexture(pixelWidth, pixelHeight);
                    postProcessChannel.preprocessTexture = channelTexture;
                }
                else
                {
                    channelTexture = postProcessedTexture;
                }
            }
            else
            {
                var superSamplingTexture = channel is IPostProcessChannel postProcessChannel
                    ? postProcessChannel.CreatePreprocessTexture(superResWidth, superResHeight)
                    : channel.CreateOutputTexture(superResWidth, superResHeight);
                superSamplingTexture.name += "-SuperSampling";
                m_SuperSamplingTextures.Add(channel, superSamplingTexture);
                channelTexture = superSamplingTexture;

                if (channel is IPostProcessChannel postChannel)
                    postChannel.preprocessTexture = postChannel.CreatePreprocessTexture(pixelWidth, pixelHeight);

                if (channel is RGBChannel)
                {
                    m_SuperSamplingCamera.targetTexture = superSamplingTexture;
                    var targetTexture = perceptionCamera.attachedCamera.targetTexture;
                    postProcessedTexture = targetTexture == null
                        ? channel.CreateOutputTexture(pixelWidth, pixelHeight) : targetTexture;
                }
                else
                {
                    postProcessedTexture = channel.CreateOutputTexture(pixelWidth, pixelHeight);
                }
            }

#if HDRP_PRESENT
            // Register the channel to be rendered through the CameraSensorPass.
            m_CameraSensorPass.AddChannel(channel, channelTexture);
#endif
            return postProcessedTexture;
        }

        /// <inheritdoc/>
        protected override void OnEndFrameRendering(ScriptableRenderContext ctx)
        {
            var cmd = CommandBufferPool.Get($"Sensor End Frame Rendering - {perceptionCamera.name}");

#if HDRP_PRESENT
            // Apply SRP lens distortion to all channel outputs.
            foreach (var channel in perceptionCamera.channels)
            {
                var outputTexture = channel is IPostProcessChannel postProcessChannel
                    ? postProcessChannel.preprocessTexture : channel.outputTexture;
                if (!(channel is RGBChannel))
                    LensDistortionUtility.ApplyLensDistortion(
                        cmd, outputTexture, perceptionCamera.attachedCamera);
            }
#endif

            if (superSamplingFactor != SuperSamplingFactor.None)
            {
                var rgbChannelTexture = perceptionCamera.GetChannel<RGBChannel>().outputTexture;
                cmd.SetRenderTarget(rgbChannelTexture);
                cmd.ClearRenderTarget(true, true, Color.black);

                // Down-sample the RGB texture by averaging super resolution pixels in a compute shader.
                SuperSamplingUtility.Downscale(
                    cmd, m_SuperSamplingCamera.targetTexture, rgbChannelTexture,
                    pixelWidth, pixelHeight, superSamplingFactor);

                // Downscale the other channel textures using a traditional blit.
                foreach (var channel in perceptionCamera.channels)
                {
                    if (channel is RGBChannel)
                        continue;

                    var superResTexture = m_SuperSamplingTextures[channel];
                    var outputTexture = channel is IPostProcessChannel postProcessChannel
                        ? postProcessChannel.preprocessTexture : channel.outputTexture;
                    cmd.Blit(superResTexture, outputTexture);
                }
            }

            // Execute post processing step for post-process-channels.
            foreach (var channel in perceptionCamera.channels)
            {
                if (channel is IPostProcessChannel postProcessChannel)
                {
                    var sampleName = $"Post Process {channel.GetType().Name}";
                    cmd.BeginSample(sampleName);
                    postProcessChannel.PostProcessChannelOutput(
                        ctx, cmd, postProcessChannel.preprocessTexture, channel.outputTexture);
                    cmd.EndSample(sampleName);
                }
            }

            // Execute the CommandBuffer
            ctx.ExecuteCommandBuffer(cmd);
            ctx.Submit();
            CommandBufferPool.Release(cmd);
        }

        static float3x3 Matrix4X4ToMatrix3X3(Matrix4x4 inMatrix)
        {
            return new float3x3(
                inMatrix[0, 0], inMatrix[0, 1], inMatrix[0, 2],
                inMatrix[1, 0], inMatrix[1, 1], inMatrix[1, 2],
                inMatrix[2, 0], inMatrix[2, 1], inMatrix[2, 2]);
        }
    }
}
