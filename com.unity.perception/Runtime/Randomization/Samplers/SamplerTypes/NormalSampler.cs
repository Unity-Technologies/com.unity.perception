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
    [SamplerDisplayName("Normal")]
    public struct NormalSampler : ISampler, IRandomRangedSampler
    {
        Unity.Mathematics.Random m_Random;

        public float mean;
        public float standardDeviation;

        [field: SerializeField]
        public uint baseSeed { get; set; }

        [field: SerializeField]
        public FloatRange range { get; set; }

        public uint state
        {
            get => m_Random.state;
            set => m_Random = new Unity.Mathematics.Random { state = value };
        }

        public NormalSampler(float min, float max, float mean, float standardDeviation, uint seed=SamplerUtility.largePrime)
        {
            range = new FloatRange(min, max);
            this.mean = mean;
            this.standardDeviation = standardDeviation;
            baseSeed = seed;
            m_Random.state = baseSeed;
        }

        public void ResetState()
        {
            state = baseSeed;
        }

        public void ResetState(int index)
        {
            ResetState();
            IterateState(index);
        }

        public void Rebase(uint seed)
        {
            baseSeed = seed;
        }

        public void IterateState(int batchIndex)
        {
            state = SamplerUtility.IterateSeed((uint)batchIndex, state);
        }

        public float Sample()
        {
            return SamplerUtility.TruncatedNormalSample(
                m_Random.NextFloat(), range.minimum, range.maximum, mean, standardDeviation);
        }

        public NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle)
        {
            var samples = SamplerUtility.GenerateSamples(this, sampleCount, out jobHandle);
            IterateState(sampleCount);
            return samples;
        }
    }
}
