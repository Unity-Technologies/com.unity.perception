using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Helper functions for rendering.
    /// </summary>
    internal class RenderingUtil
    {
        /// <summary>
        /// Check if for the given rendering pipeline there is a need to flip Y during readback.
        /// </summary>
        /// <param name="camera">Camera from which the readback is being performed.</param>
        /// <param name="usePassedInRenderTargetId">When we are using a passed in rtid, then we don't need to flip.</param>
        /// <returns>A boolean indicating if the flip is required.</returns>
        public static bool ShouldFlipColorY(Camera camera, bool usePassedInRenderTargetId)
        {
            bool shouldFlipY = false;

#if URP_ENABLED
            // Issue SIMPE-356: URP color channel is inverted with FXAA disabled, and PostProcessing enabled.
            // Issue SIMPE-400: URP disabled PP, and FXAA, but with MSAA..
            var additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            var ppaa = additionalCameraData.antialiasing != AntialiasingMode.FastApproximateAntialiasing && additionalCameraData.renderPostProcessing != false;
            shouldFlipY = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal ? ppaa || SystemInfo.graphicsUVStartsAtTop : ppaa && SystemInfo.graphicsUVStartsAtTop;
#else
            shouldFlipY = !usePassedInRenderTargetId && camera.targetTexture == null && SystemInfo.graphicsUVStartsAtTop;
#endif
            return shouldFlipY;
        }

        internal static void LogGraphicsAndFlipY(Camera camera)
        {
            var rt    = camera.targetTexture == null ? "null" : camera.targetTexture.ToString();
            var uv    = SystemInfo.graphicsUVStartsAtTop.ToString();
            var pipe  = RenderPipelineManager.currentPipeline?.GetType()?.ToString();
            var gfx   = SystemInfo.graphicsDeviceType.ToString();

            Debug.Log($"ShouldFlipY: {ShouldFlipColorY(camera, false)} <= " +
                      $"camera({camera}) rt({rt}) uv({uv}) pipe({pipe}) gfx({gfx})");
        }
    }
}
