using System;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// Used to apply sampled parameter values to a particular GameObject, Component, and property.
    /// Typically managed by a parameter configuration.
    /// </summary>
    [Serializable]
    public class ParameterTarget
    {
        public GameObject gameObject;
        public Component component;
        public string propertyName = "";
        public FieldOrProperty fieldOrProperty;
        public ParameterApplicationFrequency applicationFrequency;

        public void Set(
            GameObject obj, Component comp, string fieldOrPropertyName, ParameterApplicationFrequency frequency)
        {
            gameObject = obj;
            component = comp;
            propertyName = fieldOrPropertyName;
            applicationFrequency = frequency;
            var componentType = component.GetType();
            fieldOrProperty = componentType.GetField(fieldOrPropertyName) != null
                ? FieldOrProperty.Field
                : FieldOrProperty.Property;
        }

        public void Clear()
        {
            gameObject = null;
            component = null;
            propertyName = string.Empty;
        }

        /// <summary>
        /// Writes a sampled value to the target GameObject and property
        /// </summary>
        public void ApplyValueToTarget(object value)
        {
            var componentType = component.GetType();
            if (fieldOrProperty == FieldOrProperty.Field)
            {
                var field = componentType.GetField(propertyName);
                if (field == null)
                    throw new ParameterException(
                        $"Component type {componentType.Name} does not have a field named {propertyName}");
                field.SetValue(component, value);
            }
            else
            {
                var property = componentType.GetProperty(propertyName);
                if (property == null)
                    throw new ParameterException(
                        $"Component type {componentType.Name} does not have a property named {propertyName}");
                property.SetValue(component, value);
            }
        }
    }

    public enum ParameterApplicationFrequency
    {
        OnIterationSetup,
        EveryFrame
    }

    public enum FieldOrProperty
    {
        Field, Property
    }
}
