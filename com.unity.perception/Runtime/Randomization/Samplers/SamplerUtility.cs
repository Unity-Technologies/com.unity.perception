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
        internal const uint largePrime = 0x202A96CF;
        const int k_SamplingBatchSize = 64;

        /// <summary>
        /// Non-deterministically generates a random seed
        /// </summary>
        /// <returns>A non-deterministically generated random seed</returns>
        public static uint GenerateRandomSeed()
        {
            return (uint)Random.Range(1, uint.MaxValue);
        }

        /// <summary>
        /// Generates new a new random state by deterministically combining a base seed and an iteration index
        /// </summary>
        /// <param name="index">Usually the current scenario iteration or framesSinceInitialization</param>
        /// <param name="baseSeed">The seed to be offset</param>
        /// <returns>A new random state</returns>
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
        /// Schedules a multi-threaded job to generate an array of samples
        /// </summary>
        /// <param name="sampler">The sampler to generate samples from</param>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <typeparam name="T">The type of sampler to sample</typeparam>
        /// <returns>A NativeArray of generated samples</returns>
        public static NativeArray<float> GenerateSamples<T>(
            T sampler, int sampleCount, out JobHandle jobHandle) where T : struct, ISampler
        {
            var samples = new NativeArray<float>(
                sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            jobHandle = new SampleJob<T>
            {
                sampler = sampler,
                samples = samples
            }.ScheduleBatch(sampleCount, k_SamplingBatchSize);
            return samples;
        }

        [BurstCompile]
        struct SampleJob<T> : IJobParallelForBatch where T : ISampler
        {
            public T sampler;
            public NativeArray<float> samples;

            public void Execute(int startIndex, int count)
            {
                var endIndex = startIndex + count;
                var batchIndex = startIndex / k_SamplingBatchSize;
                sampler.IterateState(batchIndex);
                for (var i = startIndex; i < endIndex; i++)
                    samples[i] = sampler.Sample();
            }
        }

        /// <summary>
        /// https://www.johndcook.com/blog/csharp_phi/
        /// </summary>
        static float NormalCdf(float x)
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
        /// <param name="probability">Must be with the range (0, 1)</param>
        static float NormalCdfInverse(float probability)
        {
            if (probability <= 0f || probability >= 1.0f)
                throw new ArgumentOutOfRangeException($"Probability {probability} is outside the range (0, 1)");

            return probability < 0.5f
                ? -RationalApproximation(math.sqrt(-2.0f * math.log(probability)))
                : RationalApproximation(math.sqrt(-2.0f * math.log(1.0f - probability)));
        }

        /// <summary>
        /// Generates samples from a truncated normal distribution.
        /// Further reading about this distribution can be found here:
        /// https://en.wikipedia.org/wiki/Truncated_normal_distribution
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
