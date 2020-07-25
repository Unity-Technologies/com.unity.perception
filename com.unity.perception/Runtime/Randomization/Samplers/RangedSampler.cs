namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Ranged samplers bound their generated values within a designated float range.
    /// </summary>
    public abstract class RangedSampler : Sampler
    {
        public FloatRange range = new FloatRange
        {
            minimum = 0f,
            maximum = 1f,
            defaultValue = 0.5f
        };
    }
}
