using System;
using Unity.Collections;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Vector3")]
    public class Vector3Parameter : StructParameter<Vector3>
    {
        [SerializeReference] public Sampler x;
        [SerializeReference] public Sampler y;
        [SerializeReference] public Sampler z;

        public override Sampler[] Samplers => new []{ x, y, z };

        public override Vector3 Sample(int iteration)
        {
            return new Vector3(
                x.Sample(iteration),
                y.Sample(iteration),
                z.Sample(iteration));
        }

        public override Vector3[] Samples(int iteration, int totalSamples)
        {
            var samples = new Vector3[totalSamples];
            var xRng = x.Samples(iteration, totalSamples);
            var yRng = y.Samples(iteration, totalSamples);
            var zRng = z.Samples(iteration, totalSamples);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = new Vector3(xRng[i], yRng[i], zRng[i]);
            return samples;
        }

        public override NativeArray<Vector3> Samples(int iteration, int totalSamples, Allocator allocator)
        {
            var samples = new NativeArray<Vector3>(totalSamples, allocator, NativeArrayOptions.UninitializedMemory);
            using (var xRng = x.Samples(iteration, totalSamples, allocator))
            using (var yRng = y.Samples(iteration, totalSamples, allocator))
            using (var zRng = z.Samples(iteration, totalSamples, allocator))
            {
                for (var i = 0; i < totalSamples; i++)
                    samples[i] = new Vector3(xRng[i], yRng[i], zRng[i]);
            }
            return samples;
        }
    }
}
