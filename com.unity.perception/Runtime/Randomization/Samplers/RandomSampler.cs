using System;
using Unity.Collections;
using Unity.Jobs;
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

        public Unity.Mathematics.Random GetRandom(int iteration)
        {
            return new Unity.Mathematics.Random(GetRandomSeed(iteration));
        }

        public uint GetRandomSeed(int iteration)
        {
            return RandomUtility.SeedFromIndex((uint)iteration, seed);
        }

        public override float Sample(int iteration)
        {
            var random = new Unity.Mathematics.Random(GetRandomSeed(iteration));
            return Sample(ref random);
        }

        public override float[] Samples(int iteration, int totalSamples)
        {
            var random = new Unity.Mathematics.Random(GetRandomSeed(iteration));
            var samples = new float[totalSamples];
            for (var i = 0; i < totalSamples; i++)
                samples[i] = Sample(ref random);
            return samples;
        }
    }
}
