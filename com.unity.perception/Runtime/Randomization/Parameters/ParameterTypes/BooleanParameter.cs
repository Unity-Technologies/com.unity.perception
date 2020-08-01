using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Bool")]
    public class BooleanParameter : StructParameter<bool>
    {
        [SerializeReference] public Sampler value = new UniformSampler();

        public override Sampler[] Samplers => new[] { value };

        static bool Sample(float t) => t >= 0.5f;

        public override bool Sample(int seedOffset)
        {
            return Sample(value.Sample(seedOffset));
        }

        public override bool[] Samples(int seedOffset, int totalSamples)
        {
            var samples = new bool[totalSamples];
            var rngSamples = value.Samples(seedOffset, totalSamples);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = Sample(rngSamples[i]);
            return samples;
        }

        public override NativeArray<bool> Samples(int seedOffset, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<bool>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var rngSamples = value.Samples(seedOffset, totalSamples, out jobHandle);
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
