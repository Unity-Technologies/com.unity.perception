using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;
using Sampler = UnityEngine.Perception.Randomization.Samplers.Sampler;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("ColorHSVA")]
    public class ColorHsvaParameter : StructParameter<Color>
    {
        [SerializeReference] public Sampler hue = new UniformSampler();
        [SerializeReference] public Sampler saturation = new UniformSampler();
        [SerializeReference] public Sampler value = new UniformSampler();
        [SerializeReference] public Sampler alpha = new ConstantSampler(1);

        public override Sampler[] Samplers => new []{ hue, saturation, value, alpha };

        static Color CreateColorHsva(float h, float s, float v, float a)
        {
            var color = Color.HSVToRGB(h, s, v);
            color.a = a;
            return color;
        }

        public override Color Sample(int iteration)
        {
            var color = Color.HSVToRGB(
                hue.Sample(iteration),
                saturation.Sample(iteration),
                value.Sample(iteration));
            color.a = alpha.Sample(iteration);
            return color;
        }

        public override Color[] Samples(int iteration, int totalSamples)
        {
            var samples = new Color[totalSamples];
            var hueRng = hue.Samples(iteration, totalSamples);
            var satRng = saturation.Samples(iteration, totalSamples);
            var valRng = value.Samples(iteration, totalSamples);
            var alphaRng = value.Samples(iteration, totalSamples);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = CreateColorHsva(hueRng[i], satRng[i], valRng[i], alphaRng[i]);
            return samples;
        }

        public override NativeArray<Color> Samples(int iteration, int totalSamples, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Color>(totalSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var hueRng = hue.Samples(iteration, totalSamples, out var hueHandle);
            var satRng = saturation.Samples(iteration, totalSamples, out var satHandle);
            var valRng = value.Samples(iteration, totalSamples, out var valHandle);
            var alphaRng = value.Samples(iteration, totalSamples, out var alphaHandle);

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

            public void Execute()
            {
                for (var i = 0; i < samples.Length; i++)
                    samples[i] = CreateColorHsva(hueRng[i], satRng[i], valRng[i], alphaRng[i]);
            }
        }
    }
}
