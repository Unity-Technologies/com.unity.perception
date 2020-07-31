using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// Parameters, in conjunction with a parameter configuration, are used to create convenient interfaces for
    /// randomizing simulations.
    /// </summary>
    [Serializable]
    public abstract class Parameter : MonoBehaviour
    {
        public string parameterName = "Parameter Name";
        [HideInInspector] public bool hasTarget;
        [HideInInspector] public PropertyTarget target;

        public ParameterMetaData MetaData =>
            (ParameterMetaData)Attribute.GetCustomAttribute(GetType(), typeof(ParameterMetaData));

        public abstract Sampler[] Samplers { get; }

        public abstract Type OutputType { get; }

        public virtual void Validate()
        {
            ValidatePropertyTarget();
        }

        void ValidatePropertyTarget()
        {
            if (!hasTarget)
                return;
            if (target.gameObject == null)
                throw new ParameterException($"Null GameObject target on parameter \"{parameterName}\"");
            if (target.component == null)
                throw new ParameterException($"Null component target on parameter \"{parameterName}\"");
            if (string.IsNullOrEmpty(target.propertyName))
                throw new ParameterException($"Invalid property target on parameter \"{parameterName}\"");
        }

        public abstract void Apply(int iteration);
    }

    [Serializable]
    public class PropertyTarget
    {
        public GameObject gameObject;
        public Component component;
        public string propertyName = "";
        public FieldOrProperty fieldOrProperty;
    }

    public enum FieldOrProperty
    {
        Field, Property
    }
}
