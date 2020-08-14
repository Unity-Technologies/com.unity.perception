using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    [ParameterMetaData("Bool")]
    public class BooleanParameter : NumericParameter<bool>
    {
        [SerializeReference] public ISampler value = new UniformSampler(0f, 1f);

        public override ISampler[] samplers => new[] { value };

        static bool Sample(float t) => t >= 0.5f;

        public override bool Sample()
        {
            return Sample(value.Sample());
        }

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
