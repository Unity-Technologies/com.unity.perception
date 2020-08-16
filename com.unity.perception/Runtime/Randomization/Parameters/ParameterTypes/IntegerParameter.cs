using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    [ParameterMetaData("Int")]
    public class IntegerParameter : NumericParameter<int>
    {
        /// <summary>
        /// The sampler used as a source of random values for this parameter
        /// </summary>
        [SerializeReference] public ISampler value = new UniformSampler(0f, 1f);

        /// <summary>
        /// Returns the sampler employed by this parameter
        /// </summary>
        public override ISampler[] samplers => new[] { value };

        /// <summary>
        /// Generates an integer sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public override int Sample() => (int)value.Sample();

        /// <summary>
        /// Schedules a job to generate an array of samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of samples</returns>
        public override NativeArray<int> Samples(int sampleCount, out JobHandle jobHandle)
        {
            var samples = new NativeArray<int>(sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
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
            public NativeArray<int> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = (int)rngSamples[i];
            }
        }
    }
}
