using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using Unity.Mathematics;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.FloatSamplers
{
    public class GridFloatSampler : Sampler<float>
    {
        [Min(1)] public int sampleCount = 1;
        public override int SampleCount => sampleCount + 1;
        public float minSample;
        public float maxSample = 1.0f;

        public override float NextSample()
        {
            return math.lerp(
                minSample,
                maxSample,
                (float)parameter.iterationData.localSampleIndex / sampleCount);
        }
    }
}
