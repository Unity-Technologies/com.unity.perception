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
        [SerializeReference] public ISampler value = new UniformSampler(0f, 1f);
        public override ISampler[] samplers => new[] { value };

        public override int Sample() => (int)value.Sample();

        public override NativeArray<int> Samples(int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<int>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var rngSamples = value.Samples(totalSamples, out jobHandle);
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
