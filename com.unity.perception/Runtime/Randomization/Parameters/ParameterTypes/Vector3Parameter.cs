﻿using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Vector3")]
    public class Vector3Parameter : TypedParameter<Vector3>
    {
        public Sampler x;
        public Sampler y;
        public Sampler z;

        public override Sampler[] Samplers => new []{ x, y, z };

        public override Vector3 Sample(int iteration)
        {
            return new Vector3(
                x.Sample(iteration),
                y.Sample(iteration),
                z.Sample(iteration));
        }

        public override Vector3[] Samples(int iteration, int sampleCount)
        {
            var samples = new Vector3[sampleCount];
            var xRng = x.GetRandom(iteration);
            var yRng = y.GetRandom(iteration);
            var zRng = z.GetRandom(iteration);
            for (var i = 0; i < sampleCount; i++)
            {
                samples[i] = new Vector3(
                    x.Sample(ref xRng),
                    y.Sample(ref yRng),
                    z.Sample(ref zRng));
            }
            return samples;
        }
    }
}
