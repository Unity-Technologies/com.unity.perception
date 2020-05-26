using System;
using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Parameters.Abstractions
{
    public abstract class ParameterBase : MonoBehaviour
    {
        public string parameterName = "Parameter Name";
        public SamplerBase sampler;
        public bool hasTarget = true;
        public GameObject target;
        public PropertyTarget propertyTarget;
        [HideInInspector] public IterationData iterationData;

        public abstract Type SamplerType();
        public abstract Type SampleType();
        public abstract string ParameterTypeName { get; }
        protected abstract void SetupFieldOrPropertySetters();
        public abstract void Apply(IterationData data);

        void Awake()
        {
            SetupFieldOrPropertySetters();
        }
    }

    [Serializable]
    public struct IterationData
    {
        public int localSampleIndex;
        public int globalSampleIndex;
    }

    [Serializable]
    public class PropertyTarget
    {
        public Component targetComponent;
        public string propertyName;
        public TargetKind targetKind;
    }

    public enum TargetKind
    {
        Field, Property
    }
}
