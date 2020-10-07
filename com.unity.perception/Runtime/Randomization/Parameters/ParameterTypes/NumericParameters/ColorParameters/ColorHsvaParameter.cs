using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Parameters
{
    /// <summary>
    /// A numeric parameter for generating color samples using HSVA samplers
    /// </summary>
    [Serializable]
    public class ColorHsvaParameter : NumericParameter<Color>
    {
        /// <summary>
        /// The sampler used for randomizing the hue component of generated samples
        /// </summary>
        [SerializeReference] public ISampler hue = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the saturation component of generated samples
        /// </summary>
        [SerializeReference] public ISampler saturation = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the value component of generated samples
        /// </summary>
        [SerializeReference] public ISampler value = new UniformSampler(0f, 1f);

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
                yield return hue;
                yield return saturation;
                yield return value;
                yield return alpha;
            }
        }

        /// <summary>
        /// Generates an RGBA color sample
        /// </summary>
        /// <returns>The generated RGBA sample</returns>
        public override Color Sample()
        {
            var color = Color.HSVToRGB(hue.Sample(), saturation.Sample(), value.Sample());
            color.a = alpha.Sample();
            return color;
        }

        /// <summary>
        /// Generates an HSVA color sample
        /// </summary>
        /// <returns>The generated HSVA sample</returns>
        public ColorHsva SampleHsva()
        {
            return new ColorHsva(hue.Sample(), saturation.Sample(), value.Sample(), alpha.Sample());
        }

        /// <summary>
        /// Schedules a job to generate an array of RGBA color samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of samples</returns>
        public override NativeArray<Color> Samples(int sampleCount, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Color>(sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var hueRng = hue.Samples(sampleCount, out var hueHandle);
            var satRng = saturation.Samples(sampleCount, out var satHandle);
            var valRng = value.Samples(sampleCount, out var valHandle);
            var alphaRng = alpha.Samples(sampleCount, out var alphaHandle);

            var handles = new NativeArray<JobHandle>(4, Allocator.TempJob)
            {
                [0] = hueHandle,
                [1] = satHandle,
                [2] = valHandle,
                [3] = alphaHandle
            };
            var combinedJobHandles = JobHandle.CombineDependencies(handles);

            jobHandle = new SamplesJob
            {
                hueRng = hueRng,
                satRng = satRng,
                valRng = valRng,
                alphaRng = alphaRng,
                samples = samples
            }.Schedule(combinedJobHandles);
            handles.Dispose(jobHandle);

            return samples;
        }

        [BurstCompile]
        struct SamplesJob : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<float> hueRng;
            [DeallocateOnJobCompletion] public NativeArray<float> satRng;
            [DeallocateOnJobCompletion] public NativeArray<float> valRng;
            [DeallocateOnJobCompletion] public NativeArray<float> alphaRng;
            public NativeArray<Color> samples;

            static Color CreateColorHsva(float h, float s, float v, float a)
            {
                var color = Color.HSVToRGB(h, s, v);
                color.a = a;
                return color;
            }

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = CreateColorHsva(hueRng[i], satRng[i], valRng[i], alphaRng[i]);
            }
        }

        /// <summary>
        /// Schedules a job to generate an array of HSVA color samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of samples</returns>
        public NativeArray<ColorHsva> SamplesHsva(int sampleCount, out JobHandle jobHandle)
        {
            var samples = new NativeArray<ColorHsva>(sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var hueRng = hue.Samples(sampleCount, out var hueHandle);
            var satRng = saturation.Samples(sampleCount, out var satHandle);
            var valRng = value.Samples(sampleCount, out var valHandle);
            var alphaRng = alpha.Samples(sampleCount, out var alphaHandle);

            var handles = new NativeArray<JobHandle>(4, Allocator.TempJob)
            {
                [0] = hueHandle,
                [1] = satHandle,
                [2] = valHandle,
                [3] = alphaHandle
            };
            var combinedJobHandles = JobHandle.CombineDependencies(handles);

            jobHandle = new SamplesHsvaJob
            {
                hueRng = hueRng,
                satRng = satRng,
                valRng = valRng,
                alphaRng = alphaRng,
                samples = samples
            }.Schedule(combinedJobHandles);
            handles.Dispose(jobHandle);

            return samples;
        }

        [BurstCompile]
        struct SamplesHsvaJob : IJob
        {
            [DeallocateOnJobCompletion] public NativeArray<float> hueRng;
            [DeallocateOnJobCompletion] public NativeArray<float> satRng;
            [DeallocateOnJobCompletion] public NativeArray<float> valRng;
            [DeallocateOnJobCompletion] public NativeArray<float> alphaRng;
            public NativeArray<ColorHsva> samples;

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = new ColorHsva(hueRng[i], satRng[i], valRng[i], alphaRng[i]);
            }
        }
    }
}
