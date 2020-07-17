using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("ColorHSV")]
    public class ColorHsvParameter : TypedParameter<Color>
    {
        public Sampler hue;
        public Sampler saturation;
        public Sampler value;

        public override Sampler[] Samplers => new []{ hue, saturation, value };

        public override Color Sample(int iteration)
        {
            return Color.HSVToRGB(
                hue.Sample(iteration),
                saturation.Sample(iteration),
                value.Sample(iteration));
        }

        public override Color[] Samples(int iteration, int sampleCount)
        {
            var samples = new Color[sampleCount];
            var hueRng = hue.GetRandom(iteration);
            var satRng = saturation.GetRandom(iteration);
            var valRng = value.GetRandom(iteration);
            for (var i = 0; i < sampleCount; i++)
            {
                samples[i] = Color.HSVToRGB(
                    hue.Sample(ref hueRng),
                    saturation.Sample(ref satRng),
                    value.Sample(ref valRng));
            }
            return samples;
        }
    }
}
