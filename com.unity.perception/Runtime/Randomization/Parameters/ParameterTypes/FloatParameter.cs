using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [AddComponentMenu("")]
    [ParameterMetaData("Float")]
    public class FloatParameter : StructParameter<float>
    {
        [SerializeReference] public ISampler value = new UniformSampler(0f, 1f);

        public override ISampler[] Samplers => new []{ value };

        public override float Sample(int index)
        {
            return value.CopyAndIterate(index).NextSample();
        }

        public override float[] Samples(int index, int sampleCount)
        {
            return SamplerUtility.GenerateSamples(value.CopyAndIterate(index), sampleCount);
        }

        public override NativeArray<float> Samples(int index, int sampleCount, out JobHandle jobHandle)
        {
            return value.CopyAndIterate(index).Samples(sampleCount, out jobHandle);
        }
    }
}
