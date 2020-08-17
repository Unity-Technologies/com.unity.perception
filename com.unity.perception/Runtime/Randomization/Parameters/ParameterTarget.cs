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
        [SerializeField] internal GameObject gameObject;
        [SerializeField] internal Component component;
        [SerializeField] internal string propertyName = "";
        [SerializeField] internal FieldOrProperty fieldOrProperty;
        [SerializeField] internal ParameterApplicationFrequency applicationFrequency;

        /// <summary>
        /// Assigns a new target
        /// </summary>
        /// <param name="targetObject">The target GameObject</param>
        /// <param name="targetComponent">The target component on the target GameObject</param>
        /// <param name="fieldOrPropertyName">The name of the property to apply the parameter to</param>
        /// <param name="frequency">How often to apply the parameter to its target</param>
        public void AssignNewTarget(
            GameObject targetObject,
            Component targetComponent,
            string fieldOrPropertyName,
            ParameterApplicationFrequency frequency)
        {
            gameObject = targetObject;
            component = targetComponent;
            propertyName = fieldOrPropertyName;
            applicationFrequency = frequency;
            var componentType = component.GetType();
            fieldOrProperty = componentType.GetField(fieldOrPropertyName) != null
                ? FieldOrProperty.Field
                : FieldOrProperty.Property;
        }

        internal void Clear()
        {
            gameObject = null;
            component = null;
            propertyName = string.Empty;
        }

        internal void ApplyValueToTarget(object value)
        {
            var componentType = component.GetType();
            if (fieldOrProperty == FieldOrProperty.Field)
            {
                var field = componentType.GetField(propertyName);
                if (field == null)
                    throw new ParameterValidationException(
                        $"Component type {componentType.Name} does not have a field named {propertyName}");
                field.SetValue(component, value);
            }
            else
            {
                var property = componentType.GetProperty(propertyName);
                if (property == null)
                    throw new ParameterValidationException(
                        $"Component type {componentType.Name} does not have a property named {propertyName}");
                property.SetValue(component, value);
            }
        }
    }

    /// <summary>
    /// How often to apply a new sample to a parameter's target
    /// </summary>
    public enum ParameterApplicationFrequency
    {
        OnIterationSetup,
        EveryFrame
    }

    enum FieldOrProperty
    {
        Field, Property
    }
}
