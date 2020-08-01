using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Int")]
    public class IntegerParameter : StructParameter<int>
    {
        [SerializeReference] public Sampler value = new UniformSampler();
        public override Sampler[] Samplers => new[] { value };

        public override int Sample(int seedOffset) => (int)value.Sample(seedOffset);

        public override int[] Samples(int seedOffset, int totalSamples)
        {
            var samples = new int[totalSamples];
            var rngSamples = value.Samples(seedOffset, totalSamples);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = (int)rngSamples[i];
            return samples;
        }

        public override NativeArray<int> Samples(int seedOffset, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<int>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
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
            public NativeArray<int> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = (int)rngSamples[i];
            }
        }
    }
}
