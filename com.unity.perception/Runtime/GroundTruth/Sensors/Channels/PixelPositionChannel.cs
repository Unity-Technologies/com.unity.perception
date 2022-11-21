using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors.Channels
{
    /// <summary>
    /// A <see cref="CameraChannel{T}"/> that outputs the X, Y, and Z position of the surface captured by each pixel
    /// of a <see cref="CameraSensor">CameraSensor's</see> output texture, relative to the
    /// <see cref="CameraSensor">CameraSensor's</see> position and orientation.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class PixelPositionChannel : CameraChannel<float4>
    {
        static Material s_PixelPositionMaterial = new(RenderUtilities.LoadPrewarmedShader("Perception/PixelPosition"));

        /// <inheritdoc/>
        public override Color clearColor => Color.clear;

        /// <inheritdoc/>
        public override RenderTexture CreateOutputTexture(int width, int height)
        {
            var texture = new RenderTexture(
                width, height, 32, GraphicsFormat.R32G32B32A32_SFloat)
            {
                name = "Pixel Position Channel",
                enableRandomWrite = true,
                filterMode = FilterMode.Bilinear
            };
            texture.Create();
            return texture;
        }

        /// <inheritdoc/>
        public override void Execute(CameraChannelInputs inputs, RenderTexture renderTarget)
        {
            var rendererListDesc = RenderUtilities.CreateRendererListDesc(
                inputs.camera, inputs.cullingResults, s_PixelPositionMaterial, 0, perceptionCamera.layerMask);
            var list = inputs.ctx.CreateRendererList(rendererListDesc);

            inputs.cmd.SetRenderTarget(renderTarget);
            inputs.cmd.ClearRenderTarget(true, true, clearColor);
            inputs.cmd.DrawRendererList(list);
        }
    }
}
