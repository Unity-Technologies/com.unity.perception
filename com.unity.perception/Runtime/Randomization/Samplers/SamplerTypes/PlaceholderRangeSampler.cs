namespace UnityEngine.Perception.Randomization.Samplers
{
    [AddComponentMenu("")]
    [SamplerMetaData("Placeholder Range")]
    public class PlaceholderRangeSampler : OptimizableSampler
    {
        public override uint GetRandomSeed(int iteration)
        {
            throw new SamplerException("Cannot return seed from PlaceholderRangeSampler");
        }

        public override float Sample(int iteration)
        {
            throw new SamplerException("Cannot sample PlaceholderRangeSampler");
        }

        public override float Sample(ref Unity.Mathematics.Random rng)
        {
            throw new SamplerException("Cannot sample PlaceholderRangeSampler");
        }
    }
}
