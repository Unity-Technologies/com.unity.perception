using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.Vector3Samplers
{
    public class RandomUniformVector3Sampler : RandomSampler<Vector3>
    {
        [Min(1)] public int sampleCount = 1;
        public override int SampleCount => sampleCount;
        public Vector3 minSample;
        public Vector3 maxSample;

        public override Vector3 NextRandomSample(ref Unity.Mathematics.Random random)
        {
            return random.NextFloat3(minSample, maxSample);
        }
    }
}
