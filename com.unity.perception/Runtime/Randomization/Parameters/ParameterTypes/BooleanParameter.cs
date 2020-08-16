using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
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
        /// Returns the sampler employed by this parameter
        /// </summary>
        public override ISampler[] samplers => new[] { value };

        static bool Sample(float t) => t >= 0.5f;

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
                samples = samples
            }.Schedule(jobHandle);
            return samples;
        }

        [BurstCompile]
        struct SamplesJob : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<float> rngSamples;
            public NativeArray<bool> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = Sample(rngSamples[i]);
            }
        }
    }
}
