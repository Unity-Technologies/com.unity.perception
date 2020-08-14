using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    [ParameterMetaData("Float")]
    public class FloatParameter : NumericParameter<float>
    {
        [SerializeReference] public ISampler value = new UniformSampler(0f, 1f);

        public override ISampler[] samplers => new []{ value };

        public override float Sample()
        {
            return value.Sample();
        }

        public override NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle)
        {
            return value.Samples(sampleCount, out jobHandle);
        }
    }
}
