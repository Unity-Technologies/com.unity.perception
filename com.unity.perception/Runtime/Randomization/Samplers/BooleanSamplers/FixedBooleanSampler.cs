using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.BooleanSamplers
{
    public class FixedBooleanSampler : Sampler<bool>
    {
        public bool[] samples = { false, true };
        public override int SampleCount => samples.Length;

        public override bool NextSample()
        {
            return samples[parameter.iterationData.localSampleIndex];
        }
    }
}
