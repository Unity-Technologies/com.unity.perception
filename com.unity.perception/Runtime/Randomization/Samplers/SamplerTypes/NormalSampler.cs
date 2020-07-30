using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns normally distributed random values bounded within a specified range
    /// https://en.wikipedia.org/wiki/Truncated_normal_distribution
    /// </summary>
    [SamplerMetaData("Normal")]
    public class NormalSampler : RandomSampler
    {
        public float mean;
        public float stdDev = 1;

        public override float Sample(ref Unity.Mathematics.Random rng)
        {
            return RandomUtility.TruncatedNormalSample(
                rng.NextFloat(), range.minimum, range.maximum, mean, stdDev);
        }

        public override NativeArray<float> Samples(int iteration, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<float>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            jobHandle = new SampleJob
            {
                seed = GetRandomSeed(iteration),
                mean = mean,
                stdDev = stdDev,
                range = range,
                samples = samples
            }.Schedule();
            return samples;
        }

        [BurstCompile]
        struct SampleJob : IJob
        {
            public float mean;
            public float stdDev;
            public uint seed;
            public FloatRange range;
            public NativeArray<float> samples;

            public void Execute()
            {
                var rng = new Unity.Mathematics.Random(seed);
                for (var i = 0; i < samples.Length; i++)
                {
                    samples[i] = RandomUtility.TruncatedNormalSample(
                        rng.NextFloat(), range.minimum, range.maximum, mean, stdDev);
                }
            }
        }
    }
}
