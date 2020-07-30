using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns a constant value when sampled
    /// </summary>
    [SamplerMetaData("Constant")]
    public class ConstantSampler : Sampler
    {
        public float value;

        public override float Sample(int iteration)
        {
            return value;
        }

        public override float[] Samples(int iteration, int totalSamples)
        {
            var samples = new float[totalSamples];
            for (var i = 0; i < totalSamples; i++)
                samples[i] = value;
            return samples;
        }

        public override NativeArray<float> Samples(int iteration, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<float>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            jobHandle = new SampleJob
            {
                value = value,
                samples = samples
            }.Schedule();
            return samples;
        }

        [BurstCompile]
        struct SampleJob : IJob
        {
            public float value;
            public NativeArray<float> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = value;
            }
        }
    }
}
