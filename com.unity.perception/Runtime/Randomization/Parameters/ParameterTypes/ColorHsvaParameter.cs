using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("ColorHSVA")]
    public class ColorHsvaParameter : TypedParameter<Color>
    {
        public Sampler hue;
        public Sampler saturation;
        public Sampler value;
        public Sampler alpha;

        public override Sampler[] Samplers => new []{ hue, saturation, value, alpha };

        public override Color Sample(int iteration)
        {
            var color = Color.HSVToRGB(
                hue.Sample(iteration),
                saturation.Sample(iteration),
                value.Sample(iteration));
            color.a = alpha.Sample(iteration);
            return color;
        }

        public override Color[] Samples(int iteration, int sampleCount)
        {
            var samples = new Color[sampleCount];
            var hueRng = hue.GetRandom(iteration);
            var satRng = saturation.GetRandom(iteration);
            var valRng = value.GetRandom(iteration);
            var alphaRng = value.GetRandom(iteration);
            for (var i = 0; i < sampleCount; i++)
            {
                var color = Color.HSVToRGB(
                    hue.Sample(ref hueRng),
                    saturation.Sample(ref satRng),
                    value.Sample(ref valRng));
                color.a = alpha.Sample(ref alphaRng);
                samples[i] = color;
            }
            return samples;
        }
    }
}
