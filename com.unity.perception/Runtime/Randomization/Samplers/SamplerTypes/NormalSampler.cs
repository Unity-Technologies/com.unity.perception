using System;
using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Samplers
{
    [SamplerMetaData("Normal")]
    public class NormalSampler : RandomSampler
    {
        public float mean;
        public float stdDev;

        public override float Sample(ref Unity.Mathematics.Random rng)
        {
            return RandomUtility.TruncatedNormalSample(
                rng.NextFloat(), adrFloat.minimum, adrFloat.maximum, mean, stdDev);
        }
    }
}
