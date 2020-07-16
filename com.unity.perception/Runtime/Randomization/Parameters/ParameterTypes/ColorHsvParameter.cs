using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("ColorHSV")]
    public class ColorHsvParameter : Parameter
    {
        public Sampler hue;
        public Sampler saturation;
        public Sampler value;

        public override Type OutputType => typeof(Color);

        public override Sampler[] Samplers => new []{ hue, saturation, value };

        public Color Sample(int iteration)
        {
            return Color.HSVToRGB(
                hue.Sample(iteration),
                saturation.Sample(iteration),
                value.Sample(iteration));
        }
    }
}
