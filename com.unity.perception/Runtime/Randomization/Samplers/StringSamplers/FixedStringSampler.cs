using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.StringSamplers
{
    public class FixedStringSampler : Sampler<string>
    {
        public string[] samples = { "sample" };
        public override int SampleCount => samples.Length;

        public override string NextSample()
        {
            return samples[parameter.iterationData.localSampleIndex];
        }
    }
}
