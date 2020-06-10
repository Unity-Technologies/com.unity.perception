using UnityEngine.Perception.Randomization.Parameters.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.Abstractions
{
    public abstract class SamplerBase : MonoBehaviour
    {
        [HideInInspector] public ParameterBase parameter;
        public abstract int SampleCount { get; }

        public abstract string GetSampleString();
    }
}
