using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Utilities
{
    /// <summary>
    /// A utility for copying RenderTexture pixel data into a ComputeBuffer.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    static class CopyUtility
    {
        static readonly int k_PropTextureWidth = Shader.PropertyToID("textureWidth");
        static readonly int k_PropUIntTexture = Shader.PropertyToID("uintTexture");
        static readonly int k_PropUIntBuffer = Shader.PropertyToID("uintBuffer");
        static readonly int k_PropFloatTexture = Shader.PropertyToID("floatTexture");
        static readonly int k_PropFloatBuffer = Shader.PropertyToID("floatBuffer");

        static ComputeShader s_Shader;

        static CopyUtility()
        {
            s_Shader = ComputeUtilities.LoadShader("CopyTextureToBuffer");
        }

        /// <summary>
        /// Copies the pixel data of an input RenderTexture with a graphics format of type R32_UInt
        /// to a ComputeBuffer that stores unsigned integers.
        /// </summary>
        /// <param name="cmd">The CommandBuffer to enqueue this operation to.</param>
        /// <param name="texture">A RenderTexture of type R32_UInt.</param>
        /// <returns>A ComputeBuffer filled with the input texture's unsigned integer pixel data.</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static ComputeBuffer CopyUIntTextureToBuffer(CommandBuffer cmd, RenderTexture texture)
        {
            if (texture.graphicsFormat != GraphicsFormat.R32_UInt)
                throw new NotSupportedException("Only R32_UInt textures can be copied to buffers");
            var buffer = new ComputeBuffer(texture.width * texture.height, sizeof(uint));

            var threadGroupsX = Mathf.CeilToInt(texture.width / (float)16);
            var threadGroupsY = Mathf.CeilToInt(texture.height / (float)16);

            cmd.SetComputeIntParam(s_Shader, k_PropTextureWidth, texture.width);
            cmd.SetComputeTextureParam(s_Shader, 0, k_PropUIntTexture, texture);
            cmd.SetComputeBufferParam(s_Shader, 0, k_PropUIntBuffer, buffer);
            cmd.DispatchCompute(s_Shader, 0, threadGroupsX, threadGroupsY, 1);

            return buffer;
        }

        /// <summary>
        /// Copies the pixel data of an input RenderTexture with a graphics format of type R32_SFloat
        /// to a ComputeBuffer that stores float values.
        /// </summary>
        /// <param name="cmd">The CommandBuffer to enqueue this operation to.</param>
        /// <param name="texture">A RenderTexture of type R32_SFloat.</param>
        /// <returns>A ComputeBuffer filled with the input texture's float value pixel data.</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static ComputeBuffer CopyFloatTextureToBuffer(CommandBuffer cmd, RenderTexture texture)
        {
            if (texture.graphicsFormat != GraphicsFormat.R32_SFloat)
                throw new NotSupportedException("Only R32_SFloat textures can be copied to buffers");
            var buffer = new ComputeBuffer(texture.width * texture.height, sizeof(float));

            var threadGroupsX = Mathf.CeilToInt(texture.width / (float)16);
            var threadGroupsY = Mathf.CeilToInt(texture.height / (float)16);

            cmd.SetComputeIntParam(s_Shader, k_PropTextureWidth, texture.width);
            cmd.SetComputeTextureParam(s_Shader, 1, k_PropUIntTexture, texture);
            cmd.SetComputeBufferParam(s_Shader, 1, k_PropUIntBuffer, buffer);
            cmd.DispatchCompute(s_Shader, 1, threadGroupsX, threadGroupsY, 1);

            return buffer;
        }
    }
}
