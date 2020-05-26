using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.FloatSamplers
{
    public class RandomUniformFloatSampler : RandomSampler<float>
    {
        [Min(1)] public int sampleCount = 1;
        public override int SampleCount => sampleCount;
        public float minSample;
        public float maxSample = 1.0f;

        public override float NextRandomSample(ref Unity.Mathematics.Random random)
        {
            return random.NextFloat(minSample, maxSample);
        }
    }
}
