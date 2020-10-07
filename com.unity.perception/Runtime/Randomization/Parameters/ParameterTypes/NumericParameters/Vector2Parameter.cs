using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Parameters
{
    /// <summary>
    /// A numeric parameter for generating Vector2 samples
    /// </summary>
    [Serializable]
    public class Vector2Parameter : NumericParameter<Vector2>
    {
        /// <summary>
        /// The sampler used for randomizing the x component of generated samples
        /// </summary>
        [SerializeReference] public ISampler x = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the y component of generated samples
        /// </summary>
        [SerializeReference] public ISampler y = new UniformSampler(0f, 1f);

        /// <summary>
        /// Returns an IEnumerable that iterates over each sampler field in this parameter
        /// </summary>
        internal override IEnumerable<ISampler> samplers
        {
            get
            {
                yield return x;
                yield return y;
            }
        }

        /// <summary>
        /// Generates a Vector2 sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public override Vector2 Sample()
        {
            return new Vector2(x.Sample(), y.Sample());
        }

        /// <summary>
        /// Schedules a job to generate an array of samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of samples</returns>
        public override NativeArray<Vector2> Samples(int sampleCount, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Vector2>(sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var xRng = x.Samples(sampleCount, out var xHandle);
            var yRng = y.Samples(sampleCount, out var yHandle);
            var combinedJobHandles = JobHandle.CombineDependencies(xHandle, yHandle);
            jobHandle = new SamplesJob
            {
                xRng = xRng,
                yRng = yRng,
                samples = samples
            }.Schedule(combinedJobHandles);
            return samples;
        }

        [BurstCompile]
        struct SamplesJob : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<float> xRng;
            [DeallocateOnJobCompletion] public NativeArray<float> yRng;
            public NativeArray<Vector2> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = new Vector2(xRng[i], yRng[i]);
            }
        }
    }
}
