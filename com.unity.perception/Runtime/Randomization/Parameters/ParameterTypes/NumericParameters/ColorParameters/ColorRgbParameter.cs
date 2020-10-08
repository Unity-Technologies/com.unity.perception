using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Parameters
{
    /// <summary>
    /// A numeric parameter for generating RGBA color samples
    /// </summary>
    [Serializable]
    public class ColorRgbParameter : NumericParameter<Color>
    {
        /// <summary>
        /// The sampler used for randomizing the red component of generated samples
        /// </summary>
        [SerializeReference] public ISampler red = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the green component of generated samples
        /// </summary>
        [SerializeReference] public ISampler green = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the blue component of generated samples
        /// </summary>
        [SerializeReference] public ISampler blue = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the alpha component of generated samples
        /// </summary>
        [SerializeReference] public ISampler alpha = new ConstantSampler(1f);

        /// <summary>
        /// Returns an IEnumerable that iterates over each sampler field in this parameter
        /// </summary>
        internal override IEnumerable<ISampler> samplers
        {
            get
            {
                yield return red;
                yield return green;
                yield return blue;
                yield return alpha;
            }
        }

        /// <summary>
        /// Generates an RGBA color sample
        /// </summary>
        /// <returns>The generated RGBA sample</returns>
        public override Color Sample()
        {
            return new Color(red.Sample(), green.Sample(), blue.Sample(), alpha.Sample());
        }

        /// <summary>
        /// Schedules a job to generate an array of samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of samples</returns>
        public override NativeArray<Color> Samples(int sampleCount, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Color>(sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var redRng = red.Samples(sampleCount, out var redHandle);
            var greenRng = green.Samples(sampleCount, out var greenHandle);
            var blueRng = blue.Samples(sampleCount, out var blueHandle);
            var alphaRng = alpha.Samples(sampleCount, out var alphaHandle);

            var handles = new NativeArray<JobHandle>(4, Allocator.TempJob)
            {
                [0] = redHandle,
                [1] = greenHandle,
                [2] = blueHandle,
                [3] = alphaHandle
            };
            var combinedJobHandles = JobHandle.CombineDependencies(handles);

            jobHandle = new SamplesJob
            {
                redRng = redRng,
                greenRng = greenRng,
                blueRng = blueRng,
                alphaRng = alphaRng,
                samples = samples
            }.Schedule(combinedJobHandles);
            handles.Dispose(jobHandle);

            return samples;
        }

        [BurstCompile]
        struct SamplesJob : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<float> redRng;
            [DeallocateOnJobCompletion] public NativeArray<float> greenRng;
            [DeallocateOnJobCompletion] public NativeArray<float> blueRng;
            [DeallocateOnJobCompletion] public NativeArray<float> alphaRng;
            public NativeArray<Color> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = new Color(redRng[i], greenRng[i], blueRng[i], alphaRng[i]);
            }
        }
    }
}
