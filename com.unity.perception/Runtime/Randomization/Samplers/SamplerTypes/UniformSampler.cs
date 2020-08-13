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
    public struct UniformSampler : ISampler
    {
        [SerializeField] Unity.Mathematics.Random m_Random;

        [field: SerializeField]
        public FloatRange range { get; set; }

        public uint seed
        {
            get => m_Random.state;
            set => m_Random = new Unity.Mathematics.Random { state = value };
        }

        public UniformSampler(float min, float max)
        {
            range = new FloatRange(min, max);
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
            return math.lerp(range.minimum, range.maximum, m_Random.NextFloat());
        }

        public NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle)
        {
            return SamplerUtility.GenerateSamples(this, sampleCount, out jobHandle);
        }
    }
}
