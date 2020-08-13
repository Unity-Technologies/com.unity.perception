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
        [SerializeReference] public ISampler x = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler y = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler z = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler w = new UniformSampler(0f, 1f);

        public override ISampler[] Samplers => new []{ x, y, z, w };

        public override Vector4 Sample(int index)
        {
            return new Vector4(
                x.CopyAndIterate(index).NextSample(),
                y.CopyAndIterate(index).NextSample(),
                z.CopyAndIterate(index).NextSample(),
                w.CopyAndIterate(index).NextSample());
        }

        public override Vector4[] Samples(int index, int sampleCount)
        {
            var samples = new Vector4[sampleCount];
            var xRng = x.CopyAndIterate(index);
            var yRng = y.CopyAndIterate(index);
            var zRng = z.CopyAndIterate(index);
            var wRng = w.CopyAndIterate(index);
            for (var i = 0; i < sampleCount; i++)
                samples[i] = new Vector4(xRng.NextSample(), yRng.NextSample(), zRng.NextSample(), wRng.NextSample());
            return samples;
        }

        public override NativeArray<Vector4> Samples(int index, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Vector4>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var xRng = x.CopyAndIterate(index).Samples(totalSamples, out var xHandle);
            var yRng = y.CopyAndIterate(index).Samples(totalSamples, out var yHandle);
            var zRng = z.CopyAndIterate(index).Samples(totalSamples, out var zHandle);
            var wRng = w.CopyAndIterate(index).Samples(totalSamples, out var wHandle);

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
