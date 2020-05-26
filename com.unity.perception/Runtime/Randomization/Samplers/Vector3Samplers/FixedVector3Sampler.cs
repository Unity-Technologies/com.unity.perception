using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.Vector3Samplers
{
    public class FixedVector3Sampler : Sampler<Vector3>
    {
        public Vector3[] samples = { new Vector3() };
        public override int SampleCount => samples.Length;

        public override Vector3 NextSample()
        {
            return samples[parameter.iterationData.localSampleIndex];
        }
    }
}
