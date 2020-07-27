using Unity.Collections;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Bool")]
    public class BooleanParameter : StructParameter<bool>
    {
        [SerializeReference] public Sampler value;

        public override Sampler[] Samplers => new[] { value };

        static bool Sample(float t) => t >= 0.5f;

        public override bool Sample(int iteration)
        {
            return Sample(value.Sample(iteration));
        }

        public override bool[] Samples(int iteration, int totalSamples)
        {
            var samples = new bool[totalSamples];
            var rngSamples = value.Samples(iteration, totalSamples);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = Sample(rngSamples[i]);
            return samples;
        }

        public override NativeArray<bool> Samples(int iteration, int totalSamples, Allocator allocator)
        {
            var samples = new NativeArray<bool>(totalSamples, allocator, NativeArrayOptions.UninitializedMemory);
            using (var rngSamples = value.Samples(iteration, totalSamples, allocator))
            {
                for (var i = 0; i < totalSamples; i++)
                    samples[i] = Sample(rngSamples[i]);
            }
            return samples;
        }
    }
}
