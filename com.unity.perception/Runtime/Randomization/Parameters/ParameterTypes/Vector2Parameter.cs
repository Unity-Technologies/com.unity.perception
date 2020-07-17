using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Vector2")]
    public class Vector2Parameter : TypedParameter<Vector2>
    {
        public Sampler x;
        public Sampler y;

        public override Sampler[] Samplers => new []{ x, y };

        public override Vector2 Sample(int iteration)
        {
            return new Vector2(
                x.Sample(iteration),
                y.Sample(iteration));
        }

        public override Vector2[] Samples(int iteration, int sampleCount)
        {
            var samples = new Vector2[sampleCount];
            var xRng = x.GetRandom(iteration);
            var yRng = y.GetRandom(iteration);
            for (var i = 0; i < sampleCount; i++)
            {
                samples[i] = new Vector3(
                    x.Sample(ref xRng),
                    y.Sample(ref yRng));
            }
            return samples;
        }
    }
}
