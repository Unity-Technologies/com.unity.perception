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
        [SerializeReference] public Sampler x;
        [SerializeReference] public Sampler y;

        public override Sampler[] Samplers => new []{ x, y };

        public override Vector2 Sample(int iteration)
        {
            return new Vector2(
                x.Sample(iteration),
                y.Sample(iteration));
        }

        public override Vector2[] Samples(int iteration, int totalSamples)
        {
            var samples = new Vector2[totalSamples];
            var xRng = x.Samples(iteration, totalSamples);
            var yRng = y.Samples(iteration, totalSamples);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = new Vector2(xRng[i], yRng[i]);
            return samples;
        }

        public override NativeArray<Vector2> Samples(int iteration, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Vector2>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var xRng = x.Samples(iteration, totalSamples, out var xHandle);
            var yRng = y.Samples(iteration, totalSamples, out var yHandle);
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
