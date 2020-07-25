using Unity.Mathematics;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns uniformly distributed random values within a designated range.
    /// </summary>
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
