using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Perception.Randomization.Samplers
{
    public static class SamplerUtility
    {
        public const uint largePrime = 0x202A96CF;
        public const int samplingBatchSize = 64;

        /// <summary>
        /// Non-deterministically generates a random seed
        /// </summary>
        public static uint GenerateRandomSeed()
        {
            return (uint)Random.Range(1, uint.MaxValue);
        }

        /// <summary>
        /// Generates new a new random state by deterministically combining a base seed and an iteration index
        /// </summary>
        /// <param name="index">Usually the current scenario iteration or framesSinceInitialization</param>
        /// <param name="baseSeed">The seed to be offset</param>
        public static uint IterateSeed(uint index, uint baseSeed)
        {
            return ShuffleSeed(index + 1) * baseSeed;
        }

        /// <summary>
        /// Returns a shuffled a seed value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint ShuffleSeed(uint seed)
        {
            seed ^= seed << 13;
            seed ^= seed >> 17;
            seed ^= seed << 5;
            return seed;
        }

        /// <summary>
        /// Throws an exception if a sampler has an invalid range
        /// </summary>
        public static void ValidateRange(FloatRange range)
        {
            if (range.minimum > range.maximum)
                throw new ArgumentException("Invalid sampling range");
        }

        /// <summary>
        /// Generates an array of samples
        /// </summary>
        public static float[] GenerateSamples(ISampler sampler, int totalSamples)
        {
            var samples = new float[totalSamples];
            for (var i = 0; i < totalSamples; i++)
                samples[i] = sampler.NextSample();
            return samples;
        }

        /// <summary>
        /// Schedules a multi-threaded job to generate an array of samples
        /// </summary>
        public static NativeArray<float> GenerateSamples<T>(
            T sampler, int totalSamples, out JobHandle jobHandle) where T : struct, ISampler
        {
            var samples = new NativeArray<float>(
                totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            jobHandle = new SampleJob<T>
            {
                sampler = sampler,
                samples = samples
            }.ScheduleBatch(totalSamples, samplingBatchSize);
            return samples;
        }

        /// <summary>
        /// A multi-threaded job for generating an array of samples
        /// </summary>
        [BurstCompile]
        struct SampleJob<T> : IJobParallelForBatch where T : ISampler
        {
            public T sampler;
            public NativeArray<float> samples;

            public void Execute(int startIndex, int count)
            {
                var endIndex = startIndex + count;
                var batchIndex = (uint)startIndex / samplingBatchSize;
                sampler.seed = IterateSeed(batchIndex, sampler.seed);
                for (var i = startIndex; i < endIndex; i++)
                    samples[i] = sampler.NextSample();
            }
        }

        /// <summary>
        /// https://www.johndcook.com/blog/csharp_phi/
        /// </summary>
        public static float NormalCdf(float x)
        {
            const float a1 = 0.254829592f;
            const float a2 = -0.284496736f;
            const float a3 = 1.421413741f;
            const float a4 = -1.453152027f;
            const float a5 = 1.061405429f;
            const float p = 0.3275911f;

            var sign = 1;
            if (x < 0)
                sign = -1;
            x = math.abs(x) / math.sqrt(2.0f);

            var t = 1.0f / (1.0f + p*x);
            var y = 1.0f - (((((a5*t + a4)*t) + a3)*t + a2)*t + a1)*t * math.exp(-x*x);

            return 0.5f * (1.0f + sign*y);
        }

        /// <summary>
        /// https://www.johndcook.com/blog/csharp_phi_inverse/
        /// </summary>
        static float RationalApproximation(float t)
        {
            const float c0 = 2.515517f;
            const float c1 = 0.802853f;
            const float c2 = 0.010328f;
            const float d0 = 1.432788f;
            const float d1 = 0.189269f;
            const float d2 = 0.001308f;
            return t - ((c2*t + c1)*t + c0) / (((d2*t + d1)*t + d0)*t + 1.0f);
        }

        /// <summary>
        /// https://www.johndcook.com/blog/csharp_phi_inverse/
        /// </summary>
        /// <param name="p">Must be with the range (0, 1)</param>
        public static float NormalCdfInverse(float p)
        {
            if (p <= 0f || p >= 1.0f)
                throw new ArgumentOutOfRangeException($"p == {p}");

            return p < 0.5f
                ? -RationalApproximation(math.sqrt(-2.0f * math.log(p)))
                : RationalApproximation(math.sqrt(-2.0f * math.log(1.0f - p)));
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Truncated_normal_distribution
        /// TODO: fix issues with sampling at the either ends of the distribution
        /// </summary>
        public static float TruncatedNormalSample(float u, float min, float max, float mean, float stdDev)
        {
            if (min > max)
                throw new ArgumentException("Invalid range");

            if (u == 0f)
                return min;
            if (u == 1f)
                return max;
            if (stdDev == 0f)
                return math.clamp(mean, min, max);

            var a = NormalCdf((min - mean) / stdDev);
            var b = NormalCdf((max - mean) / stdDev);
            var c = math.lerp(a, b, u);

            if (c == 0f)
                return max;
            if (c == 1f)
                return min;

            var stdTruncNorm = NormalCdfInverse(c);
            return stdTruncNorm * stdDev + mean;
        }
    }
}
