using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;
using Sampler = UnityEngine.Perception.Randomization.Samplers.Sampler;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Vector3")]
    public class Vector3Parameter : StructParameter<Vector3>
    {
        [SerializeReference] public Sampler x = new UniformSampler();
        [SerializeReference] public Sampler y = new UniformSampler();
        [SerializeReference] public Sampler z = new UniformSampler();

        public override Sampler[] Samplers => new []{ x, y, z };

        public override Vector3 Sample(int iteration)
        {
            return new Vector3(
                x.Sample(iteration),
                y.Sample(iteration),
                z.Sample(iteration));
        }

        public override Vector3[] Samples(int iteration, int totalSamples)
        {
            var samples = new Vector3[totalSamples];
            var xRng = x.Samples(iteration, totalSamples);
            var yRng = y.Samples(iteration, totalSamples);
            var zRng = z.Samples(iteration, totalSamples);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = new Vector3(xRng[i], yRng[i], zRng[i]);
            return samples;
        }

        public override NativeArray<Vector3> Samples(int iteration, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Vector3>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var xRng = x.Samples(iteration, totalSamples, out var xHandle);
            var yRng = y.Samples(iteration, totalSamples, out var yHandle);
            var zRng = z.Samples(iteration, totalSamples, out var zHandle);
            var combinedJobHandles = JobHandle.CombineDependencies(xHandle, yHandle, zHandle);
            jobHandle = new SamplesJob
            {
                xRng = xRng,
                yRng = yRng,
                zRng = zRng,
                samples = samples
            }.Schedule(combinedJobHandles);
            return samples;
        }

        [BurstCompile]
        struct SamplesJob : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<float> xRng;
            [DeallocateOnJobCompletion] public NativeArray<float> yRng;
            [DeallocateOnJobCompletion] public NativeArray<float> zRng;
            public NativeArray<Vector3> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = new Vector3(xRng[i], yRng[i], zRng[i]);
            }
        }
    }
}
