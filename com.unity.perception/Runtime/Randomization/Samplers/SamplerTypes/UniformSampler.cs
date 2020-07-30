using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns uniformly distributed random values within a designated range.
    /// </summary>
    [SamplerMetaData("Uniform")]
    public class UniformSampler : RandomSampler
    {
        public override float Sample(ref Unity.Mathematics.Random rng)
        {
            return math.lerp(range.minimum, range.maximum, rng.NextFloat());
        }

        public override NativeArray<float> Samples(int iteration, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<float>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            jobHandle = new SampleJob
            {
                seed = GetRandomSeed(iteration),
                range = range,
                samples = samples
            }.Schedule();
            return samples;
        }

        [BurstCompile]
        struct SampleJob : IJob
        {
            public uint seed;
            public FloatRange range;
            public NativeArray<float> samples;

            public void Execute()
            {
                var rng = new Unity.Mathematics.Random(seed);
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = rng.NextFloat(range.minimum, range.maximum);
            }
        }
    }
}
