using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Utilities
{
    /// <summary>
    /// A helper utility that uses compute shaders to clear ComputeBuffers or RenderTextures that store float values.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    static class ClearUtility
    {
        static readonly int k_PropClearValue = Shader.PropertyToID("clearValue");
        static readonly int k_PropInputBuffer = Shader.PropertyToID("inputBuffer");
        static readonly int k_PropInputTexture = Shader.PropertyToID("inputTexture");

        static ComputeShader s_ClearShader;
        static int3 s_ThreadGroupSizeBuf;
        static int3 s_ThreadGroupSizeTex;

        static ClearUtility()
        {
            s_ClearShader = ComputeUtilities.LoadShader("Clear");
            s_ThreadGroupSizeBuf = ComputeUtilities.GetKernelThreadGroupSizes(s_ClearShader, 0);
            s_ThreadGroupSizeTex = ComputeUtilities.GetKernelThreadGroupSizes(s_ClearShader, 1);
        }

        /// <summary>
        /// Clears ComputeBuffers containing float values.
        /// </summary>
        /// <param name="cmd">The command buffer for which to queue this operation.</param>
        /// <param name="buffer">The compute buffer to clear</param>
        /// <param name="clearValue">The float value to write to each buffer index.</param>
        public static void ClearFloatBuffer(CommandBuffer cmd, ComputeBuffer buffer, float clearValue)
        {
            var threadGroups = ComputeUtilities.ThreadGroupsCount(buffer.count, s_ThreadGroupSizeBuf.x);
            cmd.SetComputeFloatParam(s_ClearShader, k_PropClearValue, clearValue);
            cmd.SetComputeBufferParam(s_ClearShader, 0, k_PropInputBuffer, buffer);
            cmd.DispatchCompute(s_ClearShader, 0, threadGroups, 1, 1);
        }

        /// <summary>
        /// Clears RenderTextures with the graphics format R32_SFloat.
        /// </summary>
        /// <param name="cmd">The command buffer for which to queue this operation.</param>
        /// <param name="renderTexture">The RenderTexture to clear.</param>
        /// <param name="clearValue">The float value to write to each pixel.</param>
        public static void ClearFloatTexture(CommandBuffer cmd, RenderTexture renderTexture, float clearValue)
        {
            var threadGroupsX = ComputeUtilities.ThreadGroupsCount(renderTexture.width, s_ThreadGroupSizeTex.x);
            var threadGroupsY = ComputeUtilities.ThreadGroupsCount(renderTexture.height, s_ThreadGroupSizeTex.y);
            cmd.SetComputeFloatParam(s_ClearShader, k_PropClearValue, clearValue);
            cmd.SetComputeTextureParam(s_ClearShader, 1, k_PropInputTexture, renderTexture);
            cmd.DispatchCompute(s_ClearShader, 1, threadGroupsX, threadGroupsY, 1);
        }
    }
}
