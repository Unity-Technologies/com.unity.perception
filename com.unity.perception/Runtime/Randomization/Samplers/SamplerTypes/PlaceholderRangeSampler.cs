namespace UnityEngine.Perception.Randomization.Samplers
{
    [SamplerMetaData("Placeholder Range")]
    public class PlaceholderRangeSampler : Sampler
    {
        public FloatRange range;

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
