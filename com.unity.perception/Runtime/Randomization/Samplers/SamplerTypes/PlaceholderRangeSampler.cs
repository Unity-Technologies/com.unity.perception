using System;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// This sampler is useful for configuring sample ranges for non-perception related scripts,
    /// particularly when these scripts have a public interface for manipulating a sample range
    /// but perform the actual sampling logic internally.
    /// </summary>
    [Serializable]
    [SamplerMetaData("Placeholder Range")]
    public struct PlaceholderRangeSampler : ISampler
    {
        public uint seed
        {
            get => 0;
            set { }
        }

        [field: SerializeField]
        public FloatRange range { get; set; }

        public PlaceholderRangeSampler(FloatRange floatRange)
        {
            range = floatRange;
        }

        public ISampler CopyAndIterate(int index)
        {
            return new PlaceholderRangeSampler(range);
        }

        public float NextSample()
        {
            throw new SamplerException("Cannot sample PlaceholderRangeSampler");
        }

        public NativeArray<float> NativeSamples(int sampleCount, out JobHandle jobHandle)
        {
            throw new SamplerException("Cannot sample PlaceholderRangeSampler");
        }
    }
}
