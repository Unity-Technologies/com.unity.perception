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

        public override ISampler[] samplers => new []{ hue, saturation, value, alpha };

        public override Color Sample()
        {
            var color = Color.HSVToRGB(hue.Sample(), saturation.Sample(), value.Sample());
            color.a = alpha.Sample();
            return color;
        }

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
    }
}
