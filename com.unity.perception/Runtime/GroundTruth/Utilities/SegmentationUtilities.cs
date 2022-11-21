using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Utilities
{
    /// <summary>
    /// A utility for creating color textures from an instance segmentation indices texture.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public static class SegmentationUtilities
    {
        static readonly int k_ColorBuffer = Shader.PropertyToID("colorBuffer");
        static readonly int k_ColorTexture = Shader.PropertyToID("colorTexture");
        static readonly int k_InstanceIdTexture = Shader.PropertyToID("instanceIdTexture");

        static ComputeShader s_InstanceIdToColorShader;
        static int3 s_ThreadGroupSizes;

        static SegmentationUtilities()
        {
            s_InstanceIdToColorShader = ComputeUtilities.LoadShader("InstanceIdToColor");
            s_ThreadGroupSizes = ComputeUtilities.GetKernelThreadGroupSizes(s_InstanceIdToColorShader, 0);
        }

        /// <summary>
        /// Creates a color texture from an instance segmentation indices texture.
        /// </summary>
        /// <param name="cmd">The CommandBuffer for which to enqueue this operation.</param>
        /// <param name="inputIndicesTexture">The instance segmentation indices texture.</param>
        /// <param name="outputColorTexture">The output color segmentation texture.</param>
        /// <param name="colorPerIndexBuffer">
        /// An array that maps a color to each unique instance index assigned to each labeled object in the scene.
        /// </param>
        public static void CreateSegmentationColorTexture(
            CommandBuffer cmd, RenderTexture inputIndicesTexture,
            RenderTexture outputColorTexture, ComputeBuffer colorPerIndexBuffer)
        {
            cmd.SetComputeTextureParam(s_InstanceIdToColorShader, 0, k_InstanceIdTexture, inputIndicesTexture);
            cmd.SetComputeTextureParam(s_InstanceIdToColorShader, 0, k_ColorTexture, outputColorTexture);
            cmd.SetComputeBufferParam(s_InstanceIdToColorShader, 0, k_ColorBuffer, colorPerIndexBuffer);

            var textureExists = inputIndicesTexture != null;
            var width = textureExists ? inputIndicesTexture.width : 1f;
            var height = textureExists ? inputIndicesTexture.height : 1f;

            var threadGroupsX = Mathf.CeilToInt(width / s_ThreadGroupSizes.x);
            var threadGroupsY = Mathf.CeilToInt(height / s_ThreadGroupSizes.y);
            cmd.DispatchCompute(s_InstanceIdToColorShader, 0, threadGroupsX, threadGroupsY, 1);
        }
    }
}
