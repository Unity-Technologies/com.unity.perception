using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using Unity.Mathematics;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.IntSamplers
{
    public class GridIntSampler : Sampler<int>
    {
        [Min(1)] public int sampleCount = 1;
        public int minSample;
        public int maxSample = 1;
        public override int SampleCount => sampleCount + 1;

        public override int NextSample()
        {
            var value = math.lerp(
                minSample,
                maxSample,
                (float)parameter.iterationData.localSampleIndex / sampleCount);
            return (int)math.round(value);
        }
    }
}
