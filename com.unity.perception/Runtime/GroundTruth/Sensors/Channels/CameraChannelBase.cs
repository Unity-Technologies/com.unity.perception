using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors.Channels
{
    /// <summary>
    /// The base class for <see cref="CameraChannel{T}">CameraChannels</see>.
    /// A camera channel generates supplementary per-pixel ground truth data
    /// for the pixel data captured by a <see cref="CameraSensor"/>.
    /// </summary>
    /// <note> Derive from <see cref="CameraChannel{T}"/> to implement a new camera channel.</note>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public abstract class CameraChannelBase
    {
        PerceptionCamera m_PerceptionCamera;
        RenderTexture m_OutputTexture;

        /// <summary>
        /// The <see cref="PerceptionCamera"/> that enabled the channel.
        /// </summary>
        protected PerceptionCamera perceptionCamera => m_PerceptionCamera;

        /// <summary>
        /// The <see cref="RenderTexture"/> output of the channel.
        /// </summary>
        public RenderTexture outputTexture => m_OutputTexture;

        /// <summary>
        /// The color to use when clearing the output texture of the channel.
        /// </summary>
        public abstract Color clearColor { get; }

        /// <summary>
        /// Initializes and returns a new RenderTexture that will contain the output of the channel.
        /// </summary>
        /// <param name="width">The requested width of the output texture in pixels.</param>
        /// <param name="height">The requested height of the output texture in pixels.</param>
        /// <returns>The newly created channel output texture.</returns>
        /// <note>
        /// This method may be called multiple times for a single channel if a <see cref="CameraSensor"/> must create
        /// multiple output textures for an array of cameras.
        /// </note>
        public abstract RenderTexture CreateOutputTexture(int width, int height);

        /// <summary>
        /// Perform the graphics operations necessary to render the output of the channel.
        /// </summary>
        /// <param name="inputs">
        /// A variety of input parameters that can be used to provide context to a channel to facilitate rendering.
        /// </param>
        /// <param name="renderTarget">The render target to write the channel output to.</param>
        public abstract void Execute(CameraChannelInputs inputs, RenderTexture renderTarget);

        /// <summary>
        /// Initializes a channel with a reference to the <see cref="perceptionCamera"/> that created the channel.
        /// </summary>
        /// <note>
        /// This method should only be called from the <see cref="perceptionCamera"/>.
        /// </note>
        /// <param name="camera">The <see cref="PerceptionCamera"/> that created this channel.</param>
        internal void Initialize(PerceptionCamera camera) => m_PerceptionCamera = camera;

        /// <summary>
        /// Set's the output renderTexture property of the channel to the given renderTexture.
        /// </summary>
        /// <note>
        /// This method should only be called from the <see cref="perceptionCamera"/>.
        /// </note>
        /// <param name="texture"></param>
        internal void SetOutputTexture(RenderTexture texture) => m_OutputTexture = texture;

        /// <summary>
        /// Invokes the readback event the channel if its readback event has any subscribers.
        /// </summary>
        /// <note>
        /// This method should only be called from the <see cref="perceptionCamera"/>.
        /// </note>
        /// <param name="cmd">The <see cref="CommandBuffer"/> to enqueue the readback operation into.</param>
        internal abstract void InvokeReadbackEvent(CommandBuffer cmd);
    }
}
