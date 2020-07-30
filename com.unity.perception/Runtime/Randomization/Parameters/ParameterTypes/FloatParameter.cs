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
        [SerializeReference] public Sampler value;

        public override Sampler[] Samplers => new []{ value };

        public override float Sample(int iteration)
        {
            return value.Sample(iteration);
        }

        public override float[] Samples(int iteration, int totalSamples)
        {
            return value.Samples(iteration, totalSamples);
        }

        public override NativeArray<float> Samples(int iteration, int totalSamples, out JobHandle jobHandle)
        {
            return value.Samples(iteration, totalSamples, out jobHandle);
        }
    }
}
