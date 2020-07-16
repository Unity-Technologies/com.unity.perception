using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Samplers
{
    public abstract class RandomSampler : Sampler
    {
        public AdrFloat adrFloat = new AdrFloat();

        public override uint GetRandomSeed(int iteration)
        {
            return RandomUtility.SeedFromIndex((uint)iteration, adrFloat.baseRandomSeed);
        }

        public override float Sample(int iteration)
        {
            var random = new Unity.Mathematics.Random(GetRandomSeed(iteration));
            return Sample(ref random);
        }
    }
}
