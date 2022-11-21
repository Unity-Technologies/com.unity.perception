using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A utility for common RGB sensor and channel rendering operations.
    /// </summary>
    public static class RenderUtilities
    {
        static ShaderVariantCollection s_ShaderVariantCollection;


        static ShaderTagId[] s_shaderPassNames;
        /// <summary>
        /// An array of common shader pass names to enable.
        /// </summary>
        public static ShaderTagId[] shaderPassNames
        {
            get
            {
                if (s_shaderPassNames == null)
                {
                    s_shaderPassNames = new[]
                    {
                        new ShaderTagId("Forward"), // HDRP Lit shader
                        new ShaderTagId("ForwardOnly"), // HDRP Unlit shader
                        new ShaderTagId("SRPDefaultUnlit"), // Cross SRP Unlit shader
                        new ShaderTagId("UniversalForward") // URP Forward
                    };
                }

                return s_shaderPassNames;
            }
        }

        /// <summary>
        /// Loads and prewarms the given shader.
        /// </summary>
        /// <param name="shaderPath">The resources path of the shader to load.</param>
        /// <returns>The loaded and prewarmed shader.</returns>
        public static Shader LoadPrewarmedShader(string shaderPath)
        {
            var shader = Shader.Find(shaderPath);
            var variant = new ShaderVariantCollection.ShaderVariant(shader, PassType.ScriptableRenderPipelineDefaultUnlit);
            if (s_ShaderVariantCollection == null)
                s_ShaderVariantCollection = new();

            s_ShaderVariantCollection.Add(variant);
            s_ShaderVariantCollection.WarmUp();
            s_ShaderVariantCollection.Remove(variant);
            return shader;
        }

        /// <summary>
        /// Creates a new RendererListDesc from the given parameters.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="cullingResult"></param>
        /// <param name="overrideMaterialPassIndex"></param>
        /// <param name="overrideMaterial"></param>
        /// <param name="layerMask"></param>
        /// <returns></returns>
        public static RendererListDesc CreateRendererListDesc(
            Camera camera,
            CullingResults cullingResult,
            Material overrideMaterial,
            int overrideMaterialPassIndex,
            LayerMask layerMask)
        {
            var shaderPasses = new[]
            {
                new ShaderTagId("Forward"), // HD Lit shader
                new ShaderTagId("ForwardOnly"), // HD Unlit shader
                new ShaderTagId("SRPDefaultUnlit"), // Cross SRP Unlit shader
                new ShaderTagId("UniversalForward"), // URP Forward
                new ShaderTagId("LightweightForward") // LWRP Forward
            };

            var stateBlock = new RenderStateBlock(0)
            {
                depthState = new DepthState(true, CompareFunction.LessEqual),
            };

            var result = new RendererListDesc(shaderPasses, cullingResult, camera)
            {
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.CommonOpaque,
                excludeObjectMotionVectors = false,
                overrideMaterial = overrideMaterial,
                overrideMaterialPassIndex = overrideMaterialPassIndex,
                stateBlock = stateBlock,
                layerMask = layerMask
            };
            return result;
        }

        /// <summary>
        /// Vertically mirrors a back buffer RenderTexture if the current platform renders to its backbuffer upside down.
        /// </summary>
        /// <param name="cmd">The command buffer in which to execute the flip.</param>
        /// <param name="source">The source back buffer RenderTexture.</param>
        /// <param name="dest">The destination RenderTexture with the flipped back buffer output.</param>
        /// <param name="camera">The camera that rendered the back buffer.</param>
        public static void FlipBackBuffer(
            CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier dest, Camera camera)
        {
            Vector2 scaleForFlip;
            Vector2 offset;
            if (ShouldFlipColorY(camera, false))
            {
                scaleForFlip = new Vector2(1, -1);
                offset = Vector2.up;
            }
            else
            {
                scaleForFlip = new Vector2(1, 1);
                offset = Vector2.zero;
            }

            cmd.Blit(source, dest, scaleForFlip, offset);
        }

        /// <summary>
        /// Check if for the given rendering pipeline there is a need to flip Y during readback.
        /// </summary>
        /// <param name="camera">Camera from which the readback is being performed.</param>
        /// <param name="usePassedInRenderTargetId">When we are using a passed in RenderTexture id, then we don't need to flip.</param>
        /// <returns>A boolean indicating if the flip is required.</returns>
        public static bool ShouldFlipColorY(Camera camera, bool usePassedInRenderTargetId)
        {
            var shouldFlipY = false;

            shouldFlipY = !usePassedInRenderTargetId && camera.targetTexture == null && SystemInfo.graphicsUVStartsAtTop;
            return shouldFlipY;
        }
    }
}
