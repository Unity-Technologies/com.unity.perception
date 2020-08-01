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
        [SerializeReference] public Sampler value = new UniformSampler();

        public override Sampler[] Samplers => new []{ value };

        public override float Sample(int seedOffset)
        {
            return value.Sample(seedOffset);
        }

        public override float[] Samples(int seedOffset, int totalSamples)
        {
            return value.Samples(seedOffset, totalSamples);
        }

        public override NativeArray<float> Samples(int seedOffset, int totalSamples, out JobHandle jobHandle)
        {
            return value.Samples(seedOffset, totalSamples, out jobHandle);
        }
    }
}
