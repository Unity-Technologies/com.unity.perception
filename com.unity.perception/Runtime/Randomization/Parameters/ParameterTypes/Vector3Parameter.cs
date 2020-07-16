using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Vector3")]
    public class Vector3Parameter : Parameter
    {
        public Sampler x;
        public Sampler y;
        public Sampler z;

        public override Type OutputType => typeof(Vector3);

        public override Sampler[] Samplers => new []{ x, y, z };

        public Vector3 Sample(int iteration)
        {
            return new Vector3(
                x.Sample(iteration),
                y.Sample(iteration),
                z.Sample(iteration));
        }
    }
}
