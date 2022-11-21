using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Helper utility for calculating the sum all of the float values in a ComputeBuffer using compute shaders.
    /// </summary>
    static class SumUtility
    {
        static readonly int k_PropTempRT = Shader.PropertyToID("PixelWeightsAggregatorTempRT");
        static readonly int k_PropInputTexture = Shader.PropertyToID("inputTexture");
        static readonly int k_PropSumTexture = Shader.PropertyToID("sumTexture");
        static readonly int k_PropOutputBuffer = Shader.PropertyToID("outputBuffer");
        static readonly int k_PropIndex = Shader.PropertyToID("index");

        static ComputeShader s_ShaderSum;
        static ComputeShader s_ShaderStoreSum;
        static int s_ThreadGroupSize;

        static SumUtility()
        {
            s_ShaderSum = ComputeUtilities.LoadShader("SumFloatsInTexture");
            s_ShaderStoreSum = ComputeUtilities.LoadShader("StoreTextureSum");
            s_ThreadGroupSize = ComputeUtilities.GetKernelThreadGroupSizes(s_ShaderSum, 0).x;
        }

        /// <summary>
        /// Calculates the sum of all the pixel values in a float texture using a compute shader.
        /// </summary>
        /// <param name="cmd">The command buffer for which to enqueue this operation.</param>
        /// <param name="input">The input float texture.</param>
        /// <param name="outputBuffer">The compute buffer for which to write the calculated sum.</param>
        /// <param name="index">The index inside the outputBuffer in which to write the calculated sum.</param>
        public static void SumFloatTexture(
            CommandBuffer cmd, RenderTexture input, ComputeBuffer outputBuffer, int index)
        {
            using (new ProfilingScope(cmd, new ProfilingSampler("Count Visible Pixels")))
            {
                // Copy the input texture to a square power of 2 temporary texture.
                // This texture must to be a power of 2 to avoid out-of-bounds texture lookups when executing the
                // shader's parallelized sum operations.
                var sqrWidth = Mathf.NextPowerOfTwo(Mathf.Max(input.width, input.height));
                cmd.GetTemporaryRT(
                    k_PropTempRT, sqrWidth, sqrWidth, 0, FilterMode.Point, GraphicsFormat.R32_SFloat, 1, true);
                cmd.SetRenderTarget(k_PropTempRT);
                cmd.ClearRenderTarget(false, true, Color.clear);
                cmd.CopyTexture(
                    input, 0, 0, 0, 0, input.width, input.height,
                    k_PropTempRT, 0, 0, 0, 0);

                // Configure the sum-shader with the temporary RT.
                cmd.SetComputeTextureParam(s_ShaderSum, 0, k_PropInputTexture, k_PropTempRT);

                // Dispatch the sum compute shader as many times as necessary to add up all the float values in the
                // input texture. Each dispatch effective shrinks the texture size by a factor of 32 in both dimensions,
                // so the shader should be dispatched enough times to reduce the texture to a size of 1x1.
                var threadGroups = sqrWidth;
                while (threadGroups > 1)
                {
                    threadGroups = ComputeUtilities.ThreadGroupsCount(threadGroups, s_ThreadGroupSize);
                    cmd.DispatchCompute(s_ShaderSum, 0, threadGroups, threadGroups, 1);
                }

                // Store the pixel weights sum in the outputBuffer.
                cmd.SetComputeTextureParam(s_ShaderStoreSum, 0, k_PropSumTexture, k_PropTempRT);
                cmd.SetComputeBufferParam(s_ShaderStoreSum, 0, k_PropOutputBuffer, outputBuffer);
                cmd.SetComputeIntParam(s_ShaderStoreSum, k_PropIndex, index);
                cmd.DispatchCompute(s_ShaderStoreSum, 0, 1, 1, 1);

                // Release the temporary RT.
                cmd.ReleaseTemporaryRT(k_PropTempRT);
            }
        }

        /// <summary>
        /// Uses burst and the Unity job system to accelerate the calculation
        /// of the sum of values within a NativeArray of float values.
        /// </summary>
        /// <param name="values">The buffer of float values to aggregate.</param>
        /// <returns></returns>
        public static float FloatArraySum(NativeArray<float> values)
        {
            var threadCount = Mathf.Max(JobsUtility.JobWorkerMaximumCount, 1);
            var sums = new NativeArray<float>(threadCount, Allocator.TempJob);
            var result = new NativeArray<float>(1, Allocator.TempJob);
            var valuesPerThread = ComputeUtilities.ThreadGroupsCount(values.Length, threadCount);
            var jobHandle = new JobHandle();
            for (var i = 0; i < threadCount; i++)
            {
                var startIndex = i * valuesPerThread;
                var endIndex = Mathf.Min(values.Length, startIndex + valuesPerThread);
                var sumJob = new SumJob
                {
                    threadIndex = i,
                    startIndex = startIndex,
                    endIndex = endIndex,
                    sums = sums,
                    values = values
                }.Schedule();
                jobHandle = JobHandle.CombineDependencies(sumJob, jobHandle);
            }
            jobHandle.Complete();

            new CombineSumsJob
            {
                result = result,
                sums = sums
            }.Schedule().Complete();

            var sum = result[0];
            sums.Dispose();
            result.Dispose();

            return sum;
        }

        [BurstCompile]
        struct SumJob : IJob
        {
            public int threadIndex;
            public int startIndex;
            public int endIndex;

            [ReadOnly]
            public NativeArray<float> values;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float> sums;

            public void Execute()
            {
                var sum = 0f;
                for (var i = startIndex; i < endIndex; i++)
                    sum += values[i];
                sums[threadIndex] += sum;
            }
        }

        [BurstCompile]
        struct CombineSumsJob : IJob
        {
            [ReadOnly] public NativeArray<float> sums;
            [WriteOnly] public NativeArray<float> result;

            public void Execute()
            {
                var sumOfSums = 0f;
                for (var i = 0; i < sums.Length; i++)
                    sumOfSums += sums[i];
                result[0] = sumOfSums;
            }
        }
    }
}
