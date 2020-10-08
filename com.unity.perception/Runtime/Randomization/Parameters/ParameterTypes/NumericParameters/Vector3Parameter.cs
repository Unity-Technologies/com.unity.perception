using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Parameters
{
    /// <summary>
    /// A numeric parameter for generating Vector3 samples
    /// </summary>
    [Serializable]
    public class Vector3Parameter : NumericParameter<Vector3>
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
        /// The sampler used for randomizing the z component of generated samples
        /// </summary>
        [SerializeReference] public ISampler z = new UniformSampler(0f, 1f);

        /// <summary>
        /// Returns an IEnumerable that iterates over each sampler field in this parameter
        /// </summary>
        internal override IEnumerable<ISampler> samplers
        {
            get
            {
                yield return x;
                yield return y;
                yield return z;
            }
        }

        /// <summary>
        /// Generates a Vector3 sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public override Vector3 Sample()
        {
            return new Vector3(x.Sample(), y.Sample(), z.Sample());
        }

        /// <summary>
        /// Schedules a job to generate an array of samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of samples</returns>
        public override NativeArray<Vector3> Samples(int sampleCount, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Vector3>(sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var xRng = x.Samples(sampleCount, out var xHandle);
            var yRng = y.Samples(sampleCount, out var yHandle);
            var zRng = z.Samples(sampleCount, out var zHandle);
            var combinedJobHandles = JobHandle.CombineDependencies(xHandle, yHandle, zHandle);
            jobHandle = new SamplesJob
            {
                xRng = xRng,
                yRng = yRng,
                zRng = zRng,
                samples = samples
            }.Schedule(combinedJobHandles);
            return samples;
        }

        [BurstCompile]
        struct SamplesJob : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<float> xRng;
            [DeallocateOnJobCompletion] public NativeArray<float> yRng;
            [DeallocateOnJobCompletion] public NativeArray<float> zRng;
            public NativeArray<Vector3> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = new Vector3(xRng[i], yRng[i], zRng[i]);
            }
        }
    }
}
