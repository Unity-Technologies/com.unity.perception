using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns uniformly distributed random values within a designated range.
    /// </summary>
    [Serializable]
    [SamplerMetaData("Uniform")]
    public struct UniformSampler : ISampler, IRandomRangedSampler
    {
        Unity.Mathematics.Random m_Random;

        [field: SerializeField]
        public FloatRange range { get; set; }

        [field: SerializeField]
        public uint baseSeed { get; set; }

        public uint state
        {
            get => m_Random.state;
            set => m_Random = new Unity.Mathematics.Random { state = value };
        }

        public UniformSampler(float min, float max, uint seed=SamplerUtility.largePrime)
        {
            range = new FloatRange(min, max);
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
            return math.lerp(range.minimum, range.maximum, m_Random.NextFloat());
        }

        public NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle)
        {
            var samples = SamplerUtility.GenerateSamples(this, sampleCount, out jobHandle);
            IterateState(sampleCount);
            return samples;
        }
    }
}
