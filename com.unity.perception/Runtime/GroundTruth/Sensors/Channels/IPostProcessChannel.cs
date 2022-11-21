using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors.Channels
{
    /// <summary>
    /// When implemented on a <see cref="CameraChannel{T}"/>, this interface enables a channel to post process its
    /// output texture after all <see cref="CameraSensor"/>s and channels have been rendered. Any graphics commands
    /// authored within an implemented PostProcessChannelOutput method will be run at the end of any given frame,
    /// well after the channel's Execute() method has been called.
    /// </summary>
    /// <examples>
    /// Channel post processing can be used to convert channel output textures to different formats (e.g. convert a
    /// float texture to a uint texture) or modify the coordinate system of a channel output (e.g. convert world-space
    /// normals to sensor-space normals).
    /// </examples>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public interface IPostProcessChannel
    {
        /// <summary>
        /// The combine channel output texture before post processing.
        /// </summary>
        public RenderTexture preprocessTexture { get; set; }

        /// <summary>
        /// Creates a channel texture meant for eventual post-processing.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <returns></returns>
        public RenderTexture CreatePreprocessTexture(int width, int height);

        /// <summary>
        /// Perform post processing steps on a channel's preprocessTexture.
        /// </summary>
        /// <param name="ctx">The current frame's <see cref="ScriptableRenderContext"/></param>
        /// <param name="cmd">The commandBuffer to enqueue post processing commands into.</param>
        /// <param name="input">The channel's preprocess texture.</param>
        /// <param name="output">The channel's outputTexture.</param>
        public void PostProcessChannelOutput(
            ScriptableRenderContext ctx, CommandBuffer cmd, RenderTexture input, RenderTexture output);
    }
}
