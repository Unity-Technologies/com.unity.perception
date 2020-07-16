using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Samplers
{
    [SamplerMetaData("Constant")]
    public class ConstantSampler : Sampler
    {
        public float value;

        public override uint GetRandomSeed(int iteration)
        {
            return RandomUtility.defaultBaseSeed;
        }

        public override float Sample(int iteration)
        {
            return value;
        }

        public override float Sample(ref Unity.Mathematics.Random rng)
        {
            return value;
        }
    }
}
