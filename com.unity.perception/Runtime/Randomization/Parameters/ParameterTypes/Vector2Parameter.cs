using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Vector2")]
    public class Vector2Parameter : StructParameter<Vector2>
    {
        [SerializeReference] public ISampler x = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler y = new UniformSampler(0f, 1f);

        public override ISampler[] Samplers => new []{ x, y };

        public override Vector2 Sample(int index)
        {
            return new Vector2(
                x.CopyAndIterate(index).NextSample(),
                y.CopyAndIterate(index).NextSample());
        }

        public override Vector2[] Samples(int index, int sampleCount)
        {
            var samples = new Vector2[sampleCount];
            var xRng = x.CopyAndIterate(index);
            var yRng = y.CopyAndIterate(index);
            for (var i = 0; i < sampleCount; i++)
                samples[i] = new Vector2(xRng.NextSample(), yRng.NextSample());
            return samples;
        }

        public override NativeArray<Vector2> Samples(int index, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Vector2>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var xRng = x.CopyAndIterate(index).NativeSamples(totalSamples, out var xHandle);
            var yRng = y.CopyAndIterate(index).NativeSamples(totalSamples, out var yHandle);
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
