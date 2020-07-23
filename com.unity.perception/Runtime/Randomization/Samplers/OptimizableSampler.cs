namespace UnityEngine.Perception.Randomization.Samplers
{
    public abstract class OptimizableSampler : Sampler
    {
        public FloatRange range = new FloatRange
        {
            minimum = 0f,
            maximum = 1f,
            defaultValue = 0.5f
        };
    }
}
