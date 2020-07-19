using System;
using Unity.Mathematics;

namespace UnityEngine.Perception.Randomization.Utilities
{
    public static class RandomUtility
    {
        public const uint largePrime = 0x202A96CF;
        public const uint defaultBaseSeed = 0x00003463;

        public static uint SeedFromIndex(
            uint index, uint baseRandomSeed = defaultBaseSeed, uint largePrime = largePrime)
        {
            return (baseRandomSeed + 1) * (index + 1) * largePrime;
        }

        public static Unity.Mathematics.Random RandomFromIndex(
            uint index, uint baseRandomSeed = defaultBaseSeed, uint largePrime = largePrime)
        {
            var seed = SeedFromIndex(index, baseRandomSeed, largePrime);
            return new Unity.Mathematics.Random(seed);
        }

        public static uint CombineSeeds(uint seed1, uint seed2)
        {
            return seed1 * seed2;
        }

        public static float NormalCdf(float x)
        {
            // constants
            const float a1 = 0.254829592f;
            const float a2 = -0.284496736f;
            const float a3 = 1.421413741f;
            const float a4 = -1.453152027f;
            const float a5 = 1.061405429f;
            const float p = 0.3275911f;

            // Save the sign of x
            var sign = 1;
            if (x < 0)
                sign = -1;
            x = math.abs(x) / math.sqrt(2.0f);

            // A&S formula 7.1.26
            var t = 1.0f / (1.0f + p*x);
            var y = 1.0f - (((((a5*t + a4)*t) + a3)*t + a2)*t + a1)*t * math.exp(-x*x);

            return 0.5f * (1.0f + sign*y);
        }

        /// <summary>
        /// https://www.johndcook.com/blog/csharp_phi_inverse/
        /// </summary>
        static float RationalApproximation(float t)
        {
            // Abramowitz and Stegun formula 26.2.23.
            // The absolute value of the error should be less than 4.5 e-4.
            float[] c = {2.515517f, 0.802853f, 0.010328f};
            float[] d = {1.432788f, 0.189269f, 0.001308f};
            return t - ((c[2]*t + c[1])*t + c[0]) / (((d[2]*t + d[1])*t + d[0])*t + 1.0f);
        }

        /// <summary>
        /// https://www.johndcook.com/blog/csharp_phi_inverse/
        /// </summary>
        public static float NormalCdfInverse(float p)
        {
            if (!(p > 0f && p < 1.0f))
                throw new ArgumentOutOfRangeException($"Invalid input argument: {p}.");

            // See article above for explanation of this section.
            return p < 0.5f
                ? -RationalApproximation(math.sqrt(-2.0f * math.log(p)))
                : RationalApproximation(math.sqrt(-2.0f * math.log(1.0f - p)));
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Truncated_normal_distribution
        /// </summary>
        public static float TruncatedNormalSample(float u, float min, float max, float mean, float stdDev)
        {
            if (u == 0f)
                return min;
            if (u == 1f)
                return max;
            var a = NormalCdf((min - mean) / stdDev);
            var b = NormalCdf((max - mean) / stdDev);
            var stdTruncNorm = NormalCdfInverse(a + u * (b - a));
            return stdTruncNorm * stdDev + mean;
        }
    }
}
