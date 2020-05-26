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
    [AddComponentMenu("Randomization/ParameterConfiguration")]
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

        void NameAndAddParameterToList(Parameter parameter)
        {
            parameter.parameterName = $"Parameter{parameters.Count}";
            parameters.Add(parameter);
        }

        /// <summary>
        /// Adds a new typed parameter to this configuration
        /// </summary>
        public T AddParameter<T>() where T : Parameter
        {
            var parameter = gameObject.AddComponent<T>();
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
            var parameter = (Parameter)gameObject.AddComponent(parameterType);
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
                if (parameterNames.Contains(parameter.parameterName))
                    throw new ParameterConfigurationException($"Two or more parameters cannot share the same name " +
                        $"(\"{parameter.parameterName}\")");
                parameterNames.Add(parameter.parameterName);
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

        void OnDestroy()
        {
#if UNITY_EDITOR
            // Cleaning up child parameters requires detecting if the scene is changing.
            // A scene change causes all objects to be destroyed and destroying objects twice throws an error.

            // Check if in play mode and not changing from play mode to edit mode
            if (EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying)
            {
                foreach(var parameter in parameters)
                    if (parameter != null) Destroy(parameter);
            }
            // Check if in the editor and not changing from edit mode to play mode
            else if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                // Delaying the destroy call avoids the issue of destroying child parameters
                // twice when the user changes scenes in the editor
                EditorApplication.delayCall += () =>
                {
                    foreach (var parameter in parameters)
                    {
                        var param = parameter;
                        if (param != null) DestroyImmediate(param);
                    }
                };
            }
#else
            foreach(var parameter in parameters)
                if (parameter != null) Destroy(parameter);
#endif
        }
    }
}
