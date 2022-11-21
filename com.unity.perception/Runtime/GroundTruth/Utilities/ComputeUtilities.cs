using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A set of useful utilities for manipulating ComputeShaders.
    /// </summary>
    static class ComputeUtilities
    {
        /// <summary>
        /// Returns the number of thread groups that should be dispatched for a given buffer and thread group size.
        /// </summary>
        /// <param name="bufferSize"></param>
        /// <param name="threadGroupSize"></param>
        /// <returns></returns>
        public static int ThreadGroupsCount(int bufferSize, int threadGroupSize)
        {
            return Mathf.CeilToInt(bufferSize / (float)threadGroupSize);
        }

        /// <summary>
        /// Queries the given compute shader's kernel's thread group sizes.
        /// </summary>
        /// <param name="shader">The compute shader to query.</param>
        /// <param name="kernelIndex">The kernel index to query.</param>
        /// <returns></returns>
        public static int3 GetKernelThreadGroupSizes(ComputeShader shader, int kernelIndex)
        {
            shader.GetKernelThreadGroupSizes(kernelIndex, out var x, out var y, out var z);
            return new int3((int)x, (int)y, (int)z);
        }

        /// <summary>
        /// Loads an instance of a compute shader from a Resources path
        /// </summary>
        /// <param name="shaderResourcesPath">The Resources path where the shader is located.</param>
        /// <returns></returns>
        public static ComputeShader LoadShader(string shaderResourcesPath)
        {
            return (ComputeShader)Object.Instantiate(Resources.Load("ComputeShaders/" + shaderResourcesPath));
        }

        /// <summary>
        /// Return a compute shader compatible 2D texture of 32-bit float values.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <returns></returns>
        public static RenderTexture CreateFloatTexture(int width, int height)
        {
            var floatTexture = new RenderTexture(
                width, height, 0, GraphicsFormat.R32_SFloat) { enableRandomWrite = true };
            floatTexture.Create();
            return floatTexture;
        }
    }
}
