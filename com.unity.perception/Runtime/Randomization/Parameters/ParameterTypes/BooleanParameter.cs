using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Bool")]
    public class BooleanParameter : TypedParameter<bool>
    {
        public Sampler value;
        public override Sampler[] Samplers => new[] { value };

        static bool Sample(float t) => t >= 0.5f;

        public override bool Sample(int iteration)
        {
            return Sample(value.Sample(iteration));
        }

        public override bool[] Samples(int iteration, int sampleCount)
        {
            var samples = new bool[sampleCount];
            var rng = value.GetRandom(iteration);
            for (var i = 0; i < sampleCount; i++)
                samples[i] = Sample(value.Sample(ref rng));
            return samples;
        }
    }
}
