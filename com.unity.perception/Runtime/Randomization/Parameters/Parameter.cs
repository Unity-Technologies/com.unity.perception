using System;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters.Attributes;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
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
        protected abstract object UntypedSample(int iteration);
        public virtual void Validate() {}

        public void Apply(int iteration)
        {
            if (!hasTarget)
                return;
            var value = UntypedSample(iteration);
            var componentType = target.component.GetType();
            switch (target.fieldOrProperty)
            {
                case FieldOrProperty.Field:
                    var fieldInfo = componentType.GetField(target.propertyName);
                    fieldInfo.SetValue(target.component, value);
                    break;
                case FieldOrProperty.Property:
                    var propertyInfo = componentType.GetProperty(target.propertyName);
                    propertyInfo.SetValue(target.component, value);
                    break;
            }
        }
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
