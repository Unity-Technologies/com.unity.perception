using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Vector4")]
    public class Vector4Parameter : TypedParameter<Vector4>
    {
        public Sampler x;
        public Sampler y;
        public Sampler z;
        public Sampler w;

        public override Sampler[] Samplers => new []{ x, y, z, w };

        public override Vector4 Sample(int iteration)
        {
            return new Vector4(
                x.Sample(iteration),
                y.Sample(iteration),
                z.Sample(iteration),
                w.Sample(iteration));
        }

        public override Vector4[] Samples(int iteration, int sampleCount)
        {
            var samples = new Vector4[sampleCount];
            var xRng = x.GetRandom(iteration);
            var yRng = y.GetRandom(iteration);
            var zRng = z.GetRandom(iteration);
            var wRng = w.GetRandom(iteration);
            for (var i = 0; i < sampleCount; i++)
            {
                samples[i] = new Vector4(
                    x.Sample(ref xRng),
                    y.Sample(ref yRng),
                    z.Sample(ref zRng),
                    w.Sample(ref wRng));
            }
            return samples;
        }
    }
}
