using Unity.Mathematics;

namespace UnityEngine.Perception.Randomization.Samplers
{
    [AddComponentMenu("")]
    [SamplerMetaData("Uniform")]
    public class UniformSampler : RandomSampler
    {
        public override float Sample(ref Unity.Mathematics.Random rng)
        {
            return math.lerp(range.minimum, range.maximum, rng.NextFloat());
        }
    }
}
