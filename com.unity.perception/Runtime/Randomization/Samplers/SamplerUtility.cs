using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Experimental.Perception.Randomization.Samplers
{
    /// <summary>
    /// A set of utility functions for defining sampler interfaces
    /// </summary>
    public static class SamplerUtility
    {
        internal const uint largePrime = 0x202A96CF;
        const int k_SamplingBatchSize = 64;

        /// <summary>
        /// Returns the sampler's display name
        /// </summary>
        /// <param name="samplerType">The sampler type</param>
        /// <returns>The display name</returns>
        public static string GetSamplerDisplayName(Type samplerType)
        {
            return samplerType.Name.Replace("Sampler", string.Empty);
        }

        /// <summary>
        /// Non-deterministically generates a random seed
        /// </summary>
        /// <returns>A non-deterministically generated random seed</returns>
        public static uint GenerateRandomSeed()
        {
            return (uint)Random.Range(1, uint.MaxValue);
        }

        /// <summary>
        /// Hashes using constants generated from a program that maximizes the avalanche effect, independence of
        /// output bit changes, and the probability of a change in each output bit if any input bit is changed.
        /// Source: https://github.com/h2database/h2database/blob/master/h2/src/test/org/h2/test/store/CalculateHashConstant.java
        /// </summary>
        /// <param name="x">Unsigned integer to hash</param>
        /// <returns>The calculated hash value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint Hash32(uint x) {
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = ((x >> 16) ^ x) * 0x45d9f3b;
            x = (x >> 16) ^ x;
            return x;
        }

        /// <summary>
        /// Based on splitmix64: http://xorshift.di.unimi.it/splitmix64.c
        /// </summary>
        /// <param name="x">64-bit value to hash</param>
        /// <returns>The calculated hash value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong Hash64(ulong x) {
            x = (x ^ (x >> 30)) * 0xbf58476d1ce4e5b9ul;
            x = (x ^ (x >> 27)) * 0x94d049bb133111ebul;
            x ^= (x >> 31);
            return x;
        }

        /// <summary>
        /// Generates new a new non-zero random state by deterministically hashing a base seed with an iteration index
        /// </summary>
        /// <param name="index">Usually the current scenario iteration or framesSinceInitialization</param>
        /// <param name="baseSeed">The seed to be offset</param>
        /// <returns>A new random state</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint IterateSeed(uint index, uint baseSeed)
        {
            var state = (uint)Hash64(((ulong)index << 32) | baseSeed);
            return state == 0u ? largePrime : state;
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
        /// Source: https://www.johndcook.com/blog/csharp_phi/
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
        /// Source: https://www.johndcook.com/blog/csharp_phi_inverse/
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
        /// Source: https://www.johndcook.com/blog/csharp_phi_inverse/
        /// Note: generates NaN values for values 0 and 1
        /// </summary>
        /// <param name="uniformSample">A uniform sample value between the range (0, 1)</param>
        static float NormalCdfInverse(float uniformSample)
        {
            return uniformSample < 0.5f
                ? -RationalApproximation(math.sqrt(-2.0f * math.log(uniformSample)))
                : RationalApproximation(math.sqrt(-2.0f * math.log(1.0f - uniformSample)));
        }

        /// <summary>
        /// Generates samples from a truncated normal distribution.
        /// Further reading about this distribution can be found here:
        /// https://en.wikipedia.org/wiki/Truncated_normal_distribution
        /// </summary>
        /// <param name="uniformSample">A sample value between 0 and 1 generated from a uniform distribution</param>
        /// <param name="min">The minimum possible value to generate</param>
        /// <param name="max">The maximum possible value to generate</param>
        /// <param name="mean">The mean of the normal distribution</param>
        /// <param name="stdDev">The standard deviation of the normal distribution</param>
        /// <returns>A value sampled from a truncated normal distribution</returns>
        /// <exception cref="ArgumentException"></exception>
        public static float TruncatedNormalSample(float uniformSample, float min, float max, float mean, float stdDev)
        {
            if (min > max)
                throw new ArgumentException("Invalid range");

            if (uniformSample == 0f)
                return min;
            if (uniformSample == 1f)
                return max;
            if (stdDev == 0f)
                return math.clamp(mean, min, max);

            var a = NormalCdf((min - mean) / stdDev);
            var b = NormalCdf((max - mean) / stdDev);
            var c = math.lerp(a, b, uniformSample);

            if (c == 0f)
                return max;
            if (c == 1f)
                return min;

            var stdTruncNorm = NormalCdfInverse(c);
            return stdTruncNorm * stdDev + mean;
        }
    }
}
