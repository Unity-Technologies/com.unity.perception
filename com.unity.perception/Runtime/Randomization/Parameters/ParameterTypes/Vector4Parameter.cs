using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Vector4")]
    public class Vector4Parameter : StructParameter<Vector4>
    {
        [SerializeReference] public Sampler x = new UniformSampler();
        [SerializeReference] public Sampler y = new UniformSampler();
        [SerializeReference] public Sampler z = new UniformSampler();
        [SerializeReference] public Sampler w = new UniformSampler();

        public override Sampler[] Samplers => new []{ x, y, z, w };

        public override Vector4 Sample(int seedOffset)
        {
            return new Vector4(
                x.Sample(seedOffset),
                y.Sample(seedOffset),
                z.Sample(seedOffset),
                w.Sample(seedOffset));
        }

        public override Vector4[] Samples(int seedOffset, int totalSamples)
        {
            var samples = new Vector4[totalSamples];
            var xRng = x.Samples(seedOffset, totalSamples);
            var yRng = y.Samples(seedOffset, totalSamples);
            var zRng = z.Samples(seedOffset, totalSamples);
            var wRng = w.Samples(seedOffset, totalSamples);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = new Vector4(xRng[i], yRng[i], zRng[i], wRng[i]);
            return samples;
        }

        public override NativeArray<Vector4> Samples(int seedOffset, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Vector4>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var xRng = x.Samples(seedOffset, totalSamples, out var xHandle);
            var yRng = y.Samples(seedOffset, totalSamples, out var yHandle);
            var zRng = z.Samples(seedOffset, totalSamples, out var zHandle);
            var wRng = w.Samples(seedOffset, totalSamples, out var wHandle);

            var handles = new NativeArray<JobHandle>(4, Allocator.Temp)
            {
                [0] = xHandle,
                [1] = yHandle,
                [2] = zHandle,
                [3] = wHandle
            };
            var combinedJobHandles = JobHandle.CombineDependencies(handles);
            handles.Dispose();

            jobHandle = new SamplesJob
            {
                xRng = xRng,
                yRng = yRng,
                zRng = zRng,
                wRng = wRng,
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
            [DeallocateOnJobCompletion] public NativeArray<float> wRng;
            public NativeArray<Vector4> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = new Vector4(xRng[i], yRng[i], zRng[i], wRng[i]);
            }
        }
    }
}
