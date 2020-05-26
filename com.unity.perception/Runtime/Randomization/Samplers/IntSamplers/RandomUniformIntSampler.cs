using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.IntSamplers
{
    public class RandomUniformIntSampler : RandomSampler<int>
    {
        [Min(1)] public int sampleCount = 1;
        public override int SampleCount => sampleCount;
        public int minSample;
        public int maxSample = 1;

        public override int NextRandomSample(ref Unity.Mathematics.Random random)
        {
            return random.NextInt(minSample, maxSample);
        }
    }
}
