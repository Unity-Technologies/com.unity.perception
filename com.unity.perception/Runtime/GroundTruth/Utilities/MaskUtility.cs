using System;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A helper utility for masking values in float textures using the pixels in color textures.
    /// Clear pixel values in the color mask will zero out their respective value in the float texture.
    /// </summary>
    static class MaskUtility
    {
        static readonly int k_PropInputTexture = Shader.PropertyToID("inputTexture");
        static readonly int k_PropMaskTexture = Shader.PropertyToID("maskTexture");
        static readonly int k_PropOutputTexture = Shader.PropertyToID("outputTexture");

        static ComputeShader s_Shader;
        static int3 s_ThreadGroupSize;

        static MaskUtility()
        {
            s_Shader = ComputeUtilities.LoadShader("Mask");
            s_ThreadGroupSize = ComputeUtilities.GetKernelThreadGroupSizes(s_Shader, 0);
        }

        /// <summary>
        /// Applies a mask to a float texture, causing all color pixels in the mask texture
        /// (specifically any pixel not the color (0, 0, 0, 0)) to clear their respective
        /// float pixels in the float texture.
        /// </summary>
        /// <param name="cmd">The CommandBuffer for which to enqueue this operation.</param>
        /// <param name="input">The input float texture to apply the mask texture to.</param>
        /// <param name="mask">The mask texture to apply to the float texture.</param>
        /// <param name="output">The float texture result of the mask operation.</param>
        public static void MaskFloatTexture(
            CommandBuffer cmd, RenderTexture input, RenderTexture mask, RenderTexture output)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Mask Float Texture")))
            {
                cmd.SetComputeTextureParam(s_Shader, 0, k_PropInputTexture, input);
                cmd.SetComputeTextureParam(s_Shader, 0, k_PropMaskTexture, mask);
                cmd.SetComputeTextureParam(s_Shader, 0, k_PropOutputTexture, output);

                var threadGroupsX = ComputeUtilities.ThreadGroupsCount(input.width, s_ThreadGroupSize.x);
                var threadGroupsY = ComputeUtilities.ThreadGroupsCount(input.height, s_ThreadGroupSize.y);
                cmd.DispatchCompute(s_Shader, 0, threadGroupsX, threadGroupsY, 1);
            }
        }
    }
}
