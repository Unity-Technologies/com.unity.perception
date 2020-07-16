using System;
using Unity.Mathematics;

namespace UnityEngine.Perception.Randomization.Samplers
{
    [SamplerMetaData("Normal")]
    public class NormalSampler : RandomSampler
    {
        public float mean;
        public float stdDev;

        // TODO: Implement truncated normal distribution sampling logic
        public override float Sample(ref Unity.Mathematics.Random rng)
        {
            // // https://stackoverflow.com/questions/218060/random-gaussian-variables
            // var u1 = 1.0f - rng.NextFloat();
            // var u2 = 1.0f - rng.NextFloat();
            // var randStdNormal = math.sqrt(-2.0f * math.log(u1)) * math.sin(2.0f * math.PI * u2);
            // return mean + stdDev * randStdNormal;

            return math.lerp(adrFloat.minimum, adrFloat.maximum, rng.NextFloat());
        }
    }
}
