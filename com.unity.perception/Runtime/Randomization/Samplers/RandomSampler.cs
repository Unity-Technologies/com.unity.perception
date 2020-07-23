using System;
using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Samplers
{
    public abstract class RandomSampler : OptimizableSampler
    {
        public uint seed = RandomUtility.defaultBaseSeed;

        public override uint GetRandomSeed(int iteration)
        {
            return RandomUtility.SeedFromIndex((uint)iteration, seed);
        }

        public override float Sample(int iteration)
        {
            var random = new Unity.Mathematics.Random(GetRandomSeed(iteration));
            return Sample(ref random);
        }
    }
}
