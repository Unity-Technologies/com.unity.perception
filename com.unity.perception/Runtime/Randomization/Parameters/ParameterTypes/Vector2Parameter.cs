using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    [ParameterMetaData("Vector2")]
    public class Vector2Parameter : NumericParameter<Vector2>
    {
        [SerializeReference] public ISampler x = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler y = new UniformSampler(0f, 1f);

        public override ISampler[] samplers => new []{ x, y };

        public override Vector2 Sample()
        {
            return new Vector2(x.Sample(), y.Sample());
        }

        public override NativeArray<Vector2> Samples(int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Vector2>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var xRng = x.Samples(totalSamples, out var xHandle);
            var yRng = y.Samples(totalSamples, out var yHandle);
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
