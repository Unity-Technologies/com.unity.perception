using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Float")]
    public class FloatParameter : TypedParameter<float>
    {
        public Sampler value;

        public override Sampler[] Samplers => new []{ value };

        public override float Sample(int iteration)
        {
            return value.Sample(iteration);
        }

        public override float[] Samples(int iteration, int sampleCount)
        {
            var samples = new float[sampleCount];
            var rng = value.GetRandom(iteration);
            for (var i = 0; i < sampleCount; i++)
                samples[i] = value.Sample(ref rng);
            return samples;
        }
    }
}
