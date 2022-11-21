using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth.Sensors;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Utilities
{
    /// <summary>
    /// A utility for downscaling render textures to support super resolution anti-aliasing.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public static class SuperSamplingUtility
    {
        static readonly int k_Temp16BitLinearRGBTexture = Shader.PropertyToID("_TempDownScaleRGBTexture");
        static readonly int k_SuperSamplingTextureProp = Shader.PropertyToID("superSamplingTexture");
        static readonly int k_DownscaledTextureProp = Shader.PropertyToID("downscaledTexture");
        static ComputeShader s_SuperSamplingShader = ComputeUtilities.LoadShader("SuperSampling");

        /// <summary>
        /// Down samples a super resolution texture using pixel averaging where the sample kernel size is determined by
        /// the input super resolution scale factor.
        /// </summary>
        /// <example>
        /// Downscaling a texture using a 4X scale factor would create a new downscaled texture that has dimensions one
        /// quarter of the magnitude of the input texture or 1/16th the total resolution. During the actual down
        /// sampling process, each pixel in the down scaled texture would correspond to a 4x4 block of pixels in the
        /// input super resolution texture that are averaged together into a single output pixel.
        /// </example>
        /// <param name="cmd">The command buffer to enqueue to down scaling graphics commands to.</param>
        /// <param name="superResTexture">The input super resolution texture.</param>
        /// <param name="outputTexture">The output downscaled texture.</param>
        /// <param name="outputWidth">The width of the output downscaled texture.</param>
        /// <param name="outputHeight">The height of the output downscaled texture.</param>
        /// <param name="scaleFactor">
        /// The scale factor between the super resolution texture and the output downscaled texture
        /// (scale factor == super res texture width / down scale texture width).
        /// </param>
        public static void Downscale(
            CommandBuffer cmd,
            RenderTargetIdentifier superResTexture,
            RenderTargetIdentifier outputTexture,
            int outputWidth,
            int outputHeight,
            SuperSamplingFactor scaleFactor)
        {
            var threadGroupsX = ComputeUtilities.ThreadGroupsCount(outputWidth, 16);
            var threadGroupsY = ComputeUtilities.ThreadGroupsCount(outputHeight, 16);
            var kernelIndex = SuperSamplingKernelIndex(scaleFactor);

            // Create a 16-bit temporary texture to capture the extra bit fidelity of super sampled
            // pixels before performing the linear-to-gamma conversion.
            cmd.GetTemporaryRT(k_Temp16BitLinearRGBTexture, outputWidth, outputHeight, 32,
                FilterMode.Bilinear, GraphicsFormat.R16G16B16A16_UNorm, 1, true);

            // Down-sample the RGB texture by averaging super resolution pixels in a compute shader.
            cmd.SetComputeTextureParam(s_SuperSamplingShader, kernelIndex,
                k_SuperSamplingTextureProp, superResTexture);
            cmd.SetComputeTextureParam(s_SuperSamplingShader, kernelIndex,
                k_DownscaledTextureProp, k_Temp16BitLinearRGBTexture);
            cmd.DispatchCompute(s_SuperSamplingShader, kernelIndex, threadGroupsX, threadGroupsY , 1);

            // Blit the 16-bit linear color space temporary texture to the output texture.
            cmd.Blit(k_Temp16BitLinearRGBTexture, outputTexture);
            cmd.ReleaseTemporaryRT(k_Temp16BitLinearRGBTexture);
        }

        static int SuperSamplingKernelIndex(SuperSamplingFactor scaleFactor)
        {
            switch (scaleFactor)
            {
                case SuperSamplingFactor._2X:
                    return 0;
                case SuperSamplingFactor._4X:
                    return 1;
                case SuperSamplingFactor._8X:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scaleFactor), scaleFactor, null);
            }
        }
    }
}
