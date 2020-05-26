using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.IntSamplers
{
    public class FixedIntSampler : Sampler<int>
    {
        public int[] samples = { 0, 1 };
        public override int SampleCount => samples.Length;

        public override int NextSample()
        {
            return samples[parameter.iterationData.localSampleIndex];
        }
    }
}
