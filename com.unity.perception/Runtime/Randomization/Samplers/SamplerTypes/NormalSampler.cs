using System;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns normally distributed random values bounded within a specified range
    /// https://en.wikipedia.org/wiki/Truncated_normal_distribution
    /// </summary>
    [Serializable]
    [SamplerMetaData("Normal")]
    public struct NormalSampler : ISampler
    {
        public float mean;
        public float standardDeviation;
        [SerializeField] Unity.Mathematics.Random m_Random;

        [field: SerializeField]
        public FloatRange range { get; set; }

        public uint seed
        {
            get => m_Random.state;
            set => m_Random = new Unity.Mathematics.Random { state = value };
        }

        public NormalSampler(float min, float max, float mean, float standardDeviation)
        {
            range = new FloatRange(min, max);
            this.mean = mean;
            this.standardDeviation = standardDeviation;
            m_Random = new Unity.Mathematics.Random();
            m_Random.InitState();
        }

        public ISampler CopyAndIterate(int index)
        {
            var newSampler = this;
            newSampler.seed = SamplerUtility.IterateSeed((uint)index, seed);
            return newSampler;
        }

        public float NextSample()
        {
            return SamplerUtility.TruncatedNormalSample(
                m_Random.NextFloat(), range.minimum, range.maximum, mean, standardDeviation);
        }

        public NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle)
        {
            return SamplerUtility.GenerateSamples(this, sampleCount, out jobHandle);
        }
    }
}
