using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using Unity.Mathematics;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.Vector3Samplers
{
    public class GridVector3Sampler : Sampler<Vector3>
    {
        [Min(1)] public int sampleCount = 1;
        public Vector3 minSample;
        public Vector3 maxSample;
        public override int SampleCount => sampleCount + 1;
        public override Vector3 NextSample()
        {
            return math.lerp(
                minSample,
                maxSample,
                (float)parameter.iterationData.localSampleIndex / sampleCount);
        }
    }
}
