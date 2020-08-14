using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    [ParameterMetaData("ColorHSVA")]
    public class ColorHsvaParameter : NumericParameter<Color>
    {
        [SerializeReference] public ISampler hue = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler saturation = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler value = new UniformSampler(0f, 1f);
        [SerializeReference] public ISampler alpha = new ConstantSampler(1f);

        public override ISampler[] Samplers => new []{ hue, saturation, value, alpha };

        static Color CreateColorHsva(float h, float s, float v, float a)
        {
            var color = Color.HSVToRGB(h, s, v);
            color.a = a;
            return color;
        }

        public override Color Sample(int index)
        {
            var color = Color.HSVToRGB(
                hue.CopyAndIterate(index).NextSample(),
                saturation.CopyAndIterate(index).NextSample(),
                value.CopyAndIterate(index).NextSample());
            color.a = alpha.CopyAndIterate(index).NextSample();
            return color;
        }

        public override Color[] Samples(int index, int sampleCount)
        {
            var samples = new Color[sampleCount];
            var hueRng = hue.CopyAndIterate(index);
            var satRng = saturation.CopyAndIterate(index);
            var valRng = value.CopyAndIterate(index);
            var alphaRng = alpha.CopyAndIterate(index);
            for (var i = 0; i < sampleCount; i++)
                samples[i] = CreateColorHsva(
                    hueRng.NextSample(), satRng.NextSample(), valRng.NextSample(), alphaRng.NextSample());
            return samples;
        }

        public override NativeArray<Color> Samples(int index, int sampleCount, out JobHandle jobHandle)
        {
            var samples = new NativeArray<Color>(sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var hueRng = hue.CopyAndIterate(index).Samples(sampleCount, out var hueHandle);
            var satRng = saturation.CopyAndIterate(index).Samples(sampleCount, out var satHandle);
            var valRng = value.CopyAndIterate(index).Samples(sampleCount, out var valHandle);
            var alphaRng = alpha.CopyAndIterate(index).Samples(sampleCount, out var alphaHandle);

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
