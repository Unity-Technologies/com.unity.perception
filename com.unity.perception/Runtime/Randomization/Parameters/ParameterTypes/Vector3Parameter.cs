using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    [ParameterMetaData("Vector3")]
    public class Vector3Parameter : NumericParameter<Vector3>
    {
        [SerializeReference] public ISampler x = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler y = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler z = new UniformSampler(0f, 1f);

        /// <summary>
        /// Returns the samplers employed by this parameter
        /// </summary>
        public override ISampler[] samplers => new []{ x, y, z };

        /// <summary>
        /// Generates a Vector3 sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public override Vector3 Sample()
        {
            return new Vector3(x.Sample(), y.Sample(), z.Sample());
        }

        /// <summary>
        /// Schedules a job to generate an array of samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of samples</returns>
        public override NativeArray<Vector3> Samples(int sampleCount, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Vector3>(sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var xRng = x.Samples(sampleCount, out var xHandle);
            var yRng = y.Samples(sampleCount, out var yHandle);
            var zRng = z.Samples(sampleCount, out var zHandle);
            var combinedJobHandles = JobHandle.CombineDependencies(xHandle, yHandle, zHandle);
            jobHandle = new SamplesJob
            {
                xRng = xRng,
                yRng = yRng,
                zRng = zRng,
                samples = samples
            }.Schedule(combinedJobHandles);
            return samples;
        }

        [BurstCompile]
        struct SamplesJob : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<float> xRng;
            [DeallocateOnJobCompletion] public NativeArray<float> yRng;
            [DeallocateOnJobCompletion] public NativeArray<float> zRng;
            public NativeArray<Vector3> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = new Vector3(xRng[i], yRng[i], zRng[i]);
            }
        }
    }
}
