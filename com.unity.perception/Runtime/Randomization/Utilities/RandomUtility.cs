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
    }
}
