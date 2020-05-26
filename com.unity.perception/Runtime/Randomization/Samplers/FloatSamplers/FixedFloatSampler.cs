using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.FloatSamplers
{
    public class FixedFloatSampler : Sampler<float>
    {
        public float[] samples = { };
        public override int SampleCount => samples.Length;

        public override float NextSample()
        {
            return samples[parameter.iterationData.localSampleIndex];
        }
    }
}
