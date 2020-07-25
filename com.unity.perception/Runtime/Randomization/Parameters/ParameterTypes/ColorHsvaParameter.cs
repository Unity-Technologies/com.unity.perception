using System;
using Unity.Collections;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("ColorHSVA")]
    public class ColorHsvaParameter : StructParameter<Color>
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

        public override Color[] Samples(int iteration, int totalSamples)
        {
            var samples = new Color[totalSamples];
            var hueRng = hue.Samples(iteration, totalSamples);
            var satRng = saturation.Samples(iteration, totalSamples);
            var valRng = value.Samples(iteration, totalSamples);
            var alphaRng = value.Samples(iteration, totalSamples);
            for (var i = 0; i < totalSamples; i++)
            {
                var color = Color.HSVToRGB(hueRng[i], satRng[i], valRng[i]);
                color.a = alphaRng[i];
                samples[i] = color;
            }
            return samples;
        }

        public override NativeArray<Color> Samples(int iteration, int totalSamples, Allocator allocator)
        {
            var samples = new NativeArray<Color>(totalSamples, allocator, NativeArrayOptions.UninitializedMemory);
            using (var hueRng = hue.Samples(iteration, totalSamples, allocator))
            using (var satRng = saturation.Samples(iteration, totalSamples, allocator))
            using (var valRng = value.Samples(iteration, totalSamples, allocator))
            using (var alphaRng = value.Samples(iteration, totalSamples, allocator))
            {
                for (var i = 0; i < totalSamples; i++)
                {
                    var color = Color.HSVToRGB(hueRng[i], satRng[i], valRng[i]);
                    color.a = alphaRng[i];
                    samples[i] = color;
                }
            }
            return samples;
        }
    }
}
