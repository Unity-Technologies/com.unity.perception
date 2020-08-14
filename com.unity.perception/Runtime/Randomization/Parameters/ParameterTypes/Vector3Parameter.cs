using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    [ParameterMetaData("Vector3")]
    public class Vector3Parameter : NumericParameter<Vector3>
    {
        [SerializeReference] public ISampler x = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler y = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler z = new UniformSampler(0f, 1f);

        public override ISampler[] Samplers => new []{ x, y, z };

        public override Vector3 Sample(int index)
        {
            return new Vector3(
                x.CopyAndIterate(index).NextSample(),
                y.CopyAndIterate(index).NextSample(),
                z.CopyAndIterate(index).NextSample());
        }

        public override Vector3[] Samples(int index, int sampleCount)
        {
            var samples = new Vector3[sampleCount];
            var xRng = x.CopyAndIterate(index);
            var yRng = y.CopyAndIterate(index);
            var zRng = z.CopyAndIterate(index);
            for (var i = 0; i < sampleCount; i++)
                samples[i] = new Vector3(xRng.NextSample(), yRng.NextSample(), zRng.NextSample());
            return samples;
        }

        public override NativeArray<Vector3> Samples(int index, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Vector3>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var xRng = x.CopyAndIterate(index).Samples(totalSamples, out var xHandle);
            var yRng = y.CopyAndIterate(index).Samples(totalSamples, out var yHandle);
            var zRng = z.CopyAndIterate(index).Samples(totalSamples, out var zHandle);
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
