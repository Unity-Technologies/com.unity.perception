using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;

namespace UnityEngine.Perception.Randomization.Configuration
{
    /// <summary>
    /// Creates parameter interfaces for randomizing simulations
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Perception/Randomization/ParameterConfiguration")]
    public class ParameterConfiguration : MonoBehaviour
    {
        internal static HashSet<ParameterConfiguration> configurations = new HashSet<ParameterConfiguration>();
        [SerializeReference] internal List<Parameter> parameters = new List<Parameter>();

        /// <summary>
        /// Find a parameter in this configuration by name
        /// </summary>
        /// <param name="parameterName">The name of the parameter to lookup</param>
        /// <param name="parameterType">The type of parameter to lookup</param>
        /// <returns>The parameter if found, null otherwise</returns>
        /// <exception cref="ParameterConfigurationException"></exception>
        public Parameter GetParameter(string parameterName, Type parameterType)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.name == parameterName && parameter.GetType() ==  parameterType)
                    return parameter;
            }
            return null;
        }

        /// <summary>
        /// Find a parameter in this configuration by name and type
        /// </summary>
        /// <param name="parameterName"></param>
        /// <typeparam name="T">The type of parameter to look for</typeparam>
        /// <returns>The parameter if found, null otherwise</returns>
        public T GetParameter<T>(string parameterName) where T : Parameter
        {
            foreach (var parameter in parameters)
            {
                if (parameter.name == parameterName && parameter is T typedParameter)
                    return typedParameter;
            }
            return null;
        }

        string PlaceholderParameterName() => $"Parameter{parameters.Count}";

        internal T AddParameter<T>() where T : Parameter, new()
        {
            var parameter = new T();
            parameter.name = PlaceholderParameterName();
            parameters.Add(parameter);
            return parameter;
        }

        internal Parameter AddParameter(Type parameterType)
        {
            if (!parameterType.IsSubclassOf(typeof(Parameter)))
                throw new ParameterConfigurationException($"Cannot add non-parameter types ({parameterType})");
            var parameter = (Parameter)Activator.CreateInstance(parameterType);
            parameter.name = PlaceholderParameterName();
            parameters.Add(parameter);
            return parameter;
        }

        internal void ApplyParameters(int seedOffset, ParameterApplicationFrequency frequency)
        {
            foreach (var parameter in parameters)
                if (parameter.target.applicationFrequency == frequency)
                    parameter.ApplyToTarget(seedOffset);
        }

        internal void ResetParameterStates(int scenarioIteration)
        {
            foreach (var parameter in parameters)
                parameter.ResetState(scenarioIteration);
        }

        internal void ValidateParameters()
        {
            var parameterNames = new HashSet<string>();
            foreach (var parameter in parameters)
            {
                if (parameterNames.Contains(parameter.name))
                    throw new ParameterConfigurationException(
                        $"Two or more parameters cannot share the same name (\"{parameter.name}\")");
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
