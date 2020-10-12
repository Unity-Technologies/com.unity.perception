using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Parameters
{
    /// <summary>
    /// A numeric parameter for generating boolean samples
    /// </summary>
    [Serializable]
    public class BooleanParameter : NumericParameter<bool>
    {
        /// <summary>
        /// The sampler used as a source of random values for this parameter
        /// </summary>
        [HideInInspector, SerializeReference] public ISampler value = new UniformSampler(0f, 1f);

        /// <summary>
        /// A threshold value that transforms random values within the range [0, 1] to boolean values.
        /// Values greater than the threshold are true, and values less than the threshold are false.
        /// </summary>
        [Range(0, 1)] public float threshold = 0.5f;

        /// <summary>
        /// Returns an IEnumerable that iterates over each sampler field in this parameter
        /// </summary>
        internal override IEnumerable<ISampler> samplers
        {
            get { yield return value; }
        }

        bool Sample(float t) => t >= threshold;

        /// <summary>
        /// Generates a boolean sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public override bool Sample()
        {
            return Sample(value.Sample());
        }

        /// <summary>
        /// Schedules a job to generate an array of samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of samples</returns>
        public override NativeArray<bool> Samples(int sampleCount, out JobHandle jobHandle)
        {
            var samples = new NativeArray<bool>(sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var rngSamples = value.Samples(sampleCount, out jobHandle);
            jobHandle = new SamplesJob
            {
                rngSamples = rngSamples,
                samples = samples,
                threshold = threshold
            }.Schedule(jobHandle);
            return samples;
        }

        [BurstCompile]
        struct SamplesJob : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<float> rngSamples;
            public NativeArray<bool> samples;
            public float threshold;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = rngSamples[i] >= threshold;
            }
        }
    }
}
