using Unity.Collections;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Int")]
    public class IntegerParameter : StructParameter<int>
    {
        public Sampler value;
        public override Sampler[] Samplers => new[] { value };

        public override int Sample(int iteration) => (int)value.Sample(iteration);

        public override int[] Samples(int iteration, int totalSamples)
        {
            var samples = new int[totalSamples];
            var rngSamples = value.Samples(iteration, totalSamples);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = (int)rngSamples[i];
            return samples;
        }

        public override NativeArray<int> Samples(int iteration, int totalSamples, Allocator allocator)
        {
            var samples = new NativeArray<int>(totalSamples, allocator, NativeArrayOptions.UninitializedMemory);
            using (var rngSamples = value.Samples(iteration, totalSamples, allocator))
            {
                for (var i = 0; i < totalSamples; i++)
                    samples[i] = (int)rngSamples[i];
            }
            return samples;
        }
    }
}
