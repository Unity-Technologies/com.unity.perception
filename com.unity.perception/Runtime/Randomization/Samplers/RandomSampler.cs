using System;
using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// A random sampler utilizes a seed value to deterministically generate
    /// random values from various probability distributions.
    /// </summary>
    public abstract class RandomSampler : RangedSampler
    {
        public uint seed = RandomUtility.defaultBaseSeed;

        public abstract float Sample(ref Unity.Mathematics.Random rng);

        protected uint GetRandomSeed(int iteration)
        {
            return RandomUtility.SeedFromIndex((uint)iteration, seed);
        }

        Unity.Mathematics.Random GetRandom(int iteration)
        {
            return new Unity.Mathematics.Random(GetRandomSeed(iteration));
        }

        public override float Sample(int iteration)
        {
            var random = GetRandom(iteration);
            return Sample(ref random);
        }

        public override float[] Samples(int iteration, int totalSamples)
        {
            var random = GetRandom(iteration);
            var samples = new float[totalSamples];
            for (var i = 0; i < totalSamples; i++)
                samples[i] = Sample(ref random);
            return samples;
        }
    }
}
