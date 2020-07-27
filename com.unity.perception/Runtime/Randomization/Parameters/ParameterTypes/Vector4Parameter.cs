using System;
using Unity.Collections;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Vector4")]
    public class Vector4Parameter : StructParameter<Vector4>
    {
        [SerializeReference] public Sampler x;
        [SerializeReference] public Sampler y;
        [SerializeReference] public Sampler z;
        [SerializeReference] public Sampler w;

        public override Sampler[] Samplers => new []{ x, y, z, w };

        public override Vector4 Sample(int iteration)
        {
            return new Vector4(
                x.Sample(iteration),
                y.Sample(iteration),
                z.Sample(iteration),
                w.Sample(iteration));
        }

        public override Vector4[] Samples(int iteration, int totalSamples)
        {
            var samples = new Vector4[totalSamples];
            var xRng = x.Samples(iteration, totalSamples);
            var yRng = y.Samples(iteration, totalSamples);
            var zRng = z.Samples(iteration, totalSamples);
            var wRng = w.Samples(iteration, totalSamples);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = new Vector4(xRng[i], yRng[i], zRng[i], wRng[i]);
            return samples;
        }

        public override NativeArray<Vector4> Samples(int iteration, int totalSamples, Allocator allocator)
        {
            var samples = new NativeArray<Vector4>(totalSamples, allocator, NativeArrayOptions.UninitializedMemory);
            using (var xRng = x.Samples(iteration, totalSamples, allocator))
            using (var yRng = y.Samples(iteration, totalSamples, allocator))
            using (var zRng = z.Samples(iteration, totalSamples, allocator))
            using (var wRng = w.Samples(iteration, totalSamples, allocator))
            {
                for (var i = 0; i < totalSamples; i++)
                    samples[i] = new Vector4(xRng[i], yRng[i], zRng[i], wRng[i]);
            }
            return samples;
        }
    }
}
