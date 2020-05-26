using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine.Perception.Randomization.Utilities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.ColorSamplers
{
    public class GridColorSampler : Sampler<Color>
    {
        [Min(1)] public int sampleCount = 1;
        public Color minSample;
        public Color maxSample;
        public override int SampleCount => sampleCount + 1;
        public override Color NextSample()
        {
            var elements = new NativeArray<float4>(1, Allocator.Temp);
            var colors = elements.Reinterpret<Color>();
            colors[0] = minSample;
            var colorSample = math.lerp(
                minSample.ToFloat4(),
                maxSample.ToFloat4(),
                (float)parameter.iterationData.localSampleIndex / sampleCount);
            return colorSample.ToColor();
        }
    }
}
