using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;

namespace UnityEngine.Perception.Randomization.Configuration
{
    /// <summary>
    /// Used to create parameter interfaces for randomizing simulations
    /// </summary>
    [AddComponentMenu("Randomization/ParameterConfiguration")]
    public class ParameterConfiguration : MonoBehaviour
    {
        public static HashSet<ParameterConfiguration> configurations = new HashSet<ParameterConfiguration>();

        [SerializeReference]
        public List<Parameter> parameters = new List<Parameter>();

        void OnEnable()
        {
            configurations.Add(this);
        }

        void OnDisable()
        {
            configurations.Remove(this);
        }

        /// <summary>
        /// Find a parameter in this configuration by name
        /// </summary>
        public Parameter GetParameter(string parameterName)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.parameterName == parameterName)
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
                if (parameter.parameterName == parameterName && parameter is T typedParameter)
                    return typedParameter;
            }
            throw new ParameterConfigurationException(
                $"Parameter with name {parameterName} and type {typeof(T).Name} not found");
        }

        /// <summary>
        /// Apply all parameters with GameObject targets
        /// </summary>
        public void ApplyParameters(int iteration)
        {
            foreach (var parameter in parameters)
                parameter.Apply(iteration);
        }

        /// <summary>
        /// Validates settings on all parameters
        /// </summary>
        public void ValidateParameters()
        {
            foreach (var parameter in parameters)
                parameter.Validate();
        }
    }
}
