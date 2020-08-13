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
        [SerializeReference] public ISampler value = new UniformSampler(0f, 1f);
        public override ISampler[] Samplers => new[] { value };

        public override int Sample(int index) => (int)value.CopyAndIterate(index).NextSample();

        public override int[] Samples(int index, int sampleCount)
        {
            var samples = new int[sampleCount];
            var sampler = value.CopyAndIterate(index);
            for (var i = 0; i < sampleCount; i++)
                samples[i] = (int)sampler.NextSample();
            return samples;
        }

        public override NativeArray<int> Samples(int index, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<int>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var rngSamples = value.CopyAndIterate(index).Samples(totalSamples, out jobHandle);
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
