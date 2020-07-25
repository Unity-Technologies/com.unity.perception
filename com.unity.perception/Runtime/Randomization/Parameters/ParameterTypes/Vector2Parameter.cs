using System;
using Unity.Collections;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Vector2")]
    public class Vector2Parameter : StructParameter<Vector2>
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

        public override Vector2[] Samples(int iteration, int totalSamples)
        {
            var samples = new Vector2[totalSamples];
            var xRng = x.Samples(iteration, totalSamples);
            var yRng = y.Samples(iteration, totalSamples);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = new Vector2(xRng[i], yRng[i]);
            return samples;
        }

        public override NativeArray<Vector2> Samples(int iteration, int totalSamples, Allocator allocator)
        {
            var samples = new NativeArray<Vector2>(totalSamples, allocator, NativeArrayOptions.UninitializedMemory);
            using (var xRng = x.Samples(iteration, totalSamples, allocator))
            using (var yRng = y.Samples(iteration, totalSamples, allocator))
            {
                for (var i = 0; i < totalSamples; i++)
                    samples[i] = new Vector2(xRng[i], yRng[i]);
            }
            return samples;
        }
    }
}
