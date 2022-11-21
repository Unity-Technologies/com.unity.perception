using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors
{
    /// <summary>
    /// The graphics inputs available to executing <see cref="CameraChannel{T}">CameraSensorChannels</see>.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public struct CameraChannelInputs
    {
        /// <summary>
        /// The <see cref="ScriptableRenderContext"/> of the current frame.
        /// </summary>
        public ScriptableRenderContext ctx;

        /// <summary>
        /// The <see cref="CommandBuffer"/> to fill with graphics commands
        /// to generate a  <see cref="CameraChannel{T}"/>'s output.
        /// </summary>
        public CommandBuffer cmd;

        /// <summary>
        /// The camera rendering the <see cref="CameraChannel{T}"/>.
        /// </summary>
        public Camera camera;

        /// <summary>
        /// The culling results calculated from the rendering camera's perspective.
        /// </summary>
        public CullingResults cullingResults;

        /// <summary>
        /// The camera's color buffer render target.
        /// </summary>
        public RenderTargetIdentifier cameraColorBuffer;
    }
}
