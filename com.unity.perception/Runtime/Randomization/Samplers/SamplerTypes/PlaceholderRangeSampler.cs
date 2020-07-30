using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// This sampler is useful for configuring sample ranges for non-perception related scripts,
    /// particularly when these scripts have a public interface for manipulating a sample range
    /// but perform the actual sampling logic internally.
    /// </summary>
    [SamplerMetaData("Placeholder Range")]
    public class PlaceholderRangeSampler : RangedSampler
    {
        public override float Sample(int iteration)
        {
            throw new SamplerException("Cannot sample PlaceholderRangeSampler");
        }

        public override float[] Samples(int iteration, int totalSamples)
        {
            throw new SamplerException("Cannot sample PlaceholderRangeSampler");
        }

        public override NativeArray<float> Samples(
            int iteration, int totalSamples, out JobHandle jobHandle)
        {
            throw new SamplerException("Cannot sample PlaceholderRangeSampler");
        }
    }
}
