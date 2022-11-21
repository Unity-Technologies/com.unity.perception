using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors.Channels
{
    /// <summary>
    /// A <see cref="CameraChannel{T}"/> that outputs the captured RGB color value for each pixel.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class RGBChannel : CameraChannel<Color32>
    {
        /// <inheritdoc/>
        public override Color clearColor => Color.black;

        /// <summary>
        /// Creates render texture with required sizes
        /// </summary>
        /// <param name="width">Width of render texture</param>
        /// <param name="height">Height of render texture</param>
        /// <returns>RenderTexture</returns>
        public override RenderTexture CreateOutputTexture(int width, int height)
        {
            var texture = new RenderTexture(width, height, 32, GraphicsFormat.R8G8B8A8_SRGB) { name = "RGB Channel" };
            texture.Create();
            return texture;
        }

        /// <summary>
        /// Sets camera output to the specified renderTarget
        /// </summary>
        /// <param name="inputs">Input camera</param>
        /// <param name="renderTarget">Target texture to render</param>
        public override void Execute(CameraChannelInputs inputs, RenderTexture renderTarget)
        {
            // If the camera is already rendering directly into the given renderTarget,
            // there's no additional work to do here.
            if (inputs.cameraColorBuffer == (RenderTargetIdentifier)renderTarget)
                return;
#if HDRP_PRESENT
            var scale = RTHandles.rtHandleProperties.rtHandleScale;
            inputs.cmd.Blit(inputs.cameraColorBuffer, renderTarget, new Vector2(scale.x, scale.y), Vector2.zero, 0, 0);
#endif
        }
    }
}
