using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.SceneManagement;

namespace UnityEngine.Perception.Randomization.Configuration
{
    /// <summary>
    /// Creates parameter interfaces for randomizing simulations
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Perception/Randomization/ParameterConfiguration")]
    public class ParameterConfiguration : MonoBehaviour
    {
        public static HashSet<ParameterConfiguration> configurations = new HashSet<ParameterConfiguration>();

        [SerializeReference]
        public List<Parameter> parameters = new List<Parameter>();

        /// <summary>
        /// Find a parameter in this configuration by name
        /// </summary>
        public Parameter GetParameter(string parameterName)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.name == parameterName)
                    return parameter;
            }
            throw new ParameterConfigurationException(
                $"Parameter with name {parameterName} not found");
        }

        /// <summary>
        /// Find a parameter in this configuration by name
        /// </summary>
        public T GetParameter<T>(string parameterName) where T : Parameter
        {
            foreach (var parameter in parameters)
            {
                if (parameter.name == parameterName && parameter is T typedParameter)
                    return typedParameter;
            }
            throw new ParameterConfigurationException(
                $"Parameter with name {parameterName} and type {typeof(T).Name} not found");
        }

        void NameAndAddParameterToList(Parameter parameter)
        {
            parameter.name = $"Parameter{parameters.Count}";
            parameters.Add(parameter);
        }

        /// <summary>
        /// Adds a new typed parameter to this configuration
        /// </summary>
        public T AddParameter<T>() where T : Parameter, new()
        {
            var parameter = new T();
            NameAndAddParameterToList(parameter);
            return parameter;
        }

        /// <summary>
        /// Adds a new parameter to this configuration
        /// </summary>
        public Parameter AddParameter(Type parameterType)
        {
            if (!parameterType.IsSubclassOf(typeof(Parameter)))
                throw new ParameterConfigurationException($"Cannot add non-parameter types ({parameterType})");
            var parameter = (Parameter)Activator.CreateInstance(parameterType);
            NameAndAddParameterToList(parameter);
            return parameter;
        }

        /// <summary>
        /// Calls apply on all parameters with GameObject targets in this configuration
        /// </summary>
        public void ApplyParameters(int seedOffset, ParameterApplicationFrequency frequency)
        {
            foreach (var parameter in parameters)
                if (parameter.target.applicationFrequency == frequency)
                    parameter.ApplyToTarget(seedOffset);
        }

        /// <summary>
        /// Calls Validate() on all parameters within this configuration
        /// </summary>
        public void ValidateParameters()
        {
            var parameterNames = new HashSet<string>();
            foreach (var parameter in parameters)
            {
                if (parameterNames.Contains(parameter.name))
                    throw new ParameterConfigurationException($"Two or more parameters cannot share the same name " +
                        $"(\"{parameter.name}\")");
                parameterNames.Add(parameter.name);
                parameter.Validate();
            }
        }

        void OnEnable()
        {
            configurations.Add(this);
        }

        void OnDisable()
        {
            configurations.Remove(this);
        }
    }
}
