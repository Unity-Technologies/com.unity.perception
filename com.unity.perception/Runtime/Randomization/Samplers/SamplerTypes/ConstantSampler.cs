using System;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns a constant value when sampled
    /// </summary>
    [Serializable]
    [SamplerDisplayName("Constant")]
    public struct ConstantSampler : ISampler
    {
        public float value;

        public ConstantSampler(float value)
        {
            this.value = value;
        }

        public void ResetState() { }

        public void ResetState(int index) { }

        public void Rebase(uint seed) { }

        public void IterateState(int batchIndex) { }

        public float Sample()
        {
            return value;
        }

        public NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle)
        {
            return SamplerUtility.GenerateSamples(this, sampleCount, out jobHandle);
        }
    }
}
