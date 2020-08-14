namespace UnityEngine.Perception.Randomization.Samplers
{
    public interface IRandomRangedSampler
    {
        uint baseSeed { get; set; }
        uint state { get; set; }
        FloatRange range { get; set; }
    }
}
