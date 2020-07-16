using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Float")]
    public class FloatParameter : Parameter
    {
        public Sampler value;

        public override Type OutputType => typeof(float);

        public override Sampler[] Samplers => new []{ value };

        public float Sample(int iteration)
        {
            return value.Sample(iteration);
        }
    }
}
