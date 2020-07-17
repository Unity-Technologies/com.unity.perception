using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Int")]
    public class IntegerParameter : TypedParameter<int>
    {
        public Sampler value;
        public override Sampler[] Samplers => new[] { value };

        public override int Sample(int iteration) => (int)value.Sample(iteration);

        public override int[] Samples(int iteration, int sampleCount)
        {
            var samples = new int[sampleCount];
            var rng = value.GetRandom(iteration);
            for (var i = 0; i < sampleCount; i++)
                samples[i] = (int)value.Sample(ref rng);
            return samples;
        }
    }
}
