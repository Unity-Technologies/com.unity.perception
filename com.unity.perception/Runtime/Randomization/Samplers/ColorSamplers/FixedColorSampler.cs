using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.ColorSamplers
{
    public class FixedColorSampler : Sampler<Color>
    {
        public Color[] samples = { new Color() };
        public override int SampleCount => samples.Length;

        public override Color NextSample()
        {
            return samples[parameter.iterationData.localSampleIndex];
        }
    }
}
