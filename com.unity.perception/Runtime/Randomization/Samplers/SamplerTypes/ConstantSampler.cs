using System;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns a constant value when sampled
    /// </summary>
    [Serializable]
    [SamplerMetaData("Constant")]
    public struct ConstantSampler : ISampler
    {
        public float value;

        public uint seed
        {
            get => 0;
            set { }
        }

        public FloatRange range
        {
            get => new FloatRange(value, value);
            set { }
        }

        public ConstantSampler(float value)
        {
            this.value = value;
        }

        public ISampler CopyAndIterate(int index)
        {
            return new ConstantSampler(value);
        }

        public float NextSample()
        {
            return value;
        }

        public NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle)
        {
            return SamplerUtility.GenerateSamples(this, sampleCount, out jobHandle);
        }
    }
}
