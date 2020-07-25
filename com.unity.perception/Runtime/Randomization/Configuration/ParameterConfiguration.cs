using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Perception.Randomization.Serialization;

namespace UnityEngine.Perception.Randomization.Configuration
{
    /// <summary>
    /// Used to create parameter interfaces for randomizing simulations
    /// </summary>
    [AddComponentMenu("Randomization/ParameterConfiguration")]
    public class ParameterConfiguration : MonoBehaviour
    {
        static ParameterConfiguration s_ActiveConfig;

        /// <summary>
        /// Returns the active parameter configuration in the scene
        /// </summary>
        public static ParameterConfiguration ActiveConfig
        {
            get => s_ActiveConfig;
            private set
            {
                if (s_ActiveConfig != null)
                    throw new ParameterConfigurationException("There cannot be more than one active ParameterConfiguration");
                s_ActiveConfig = value;
            }
        }

        [SerializeReference]
        public List<Parameter> parameters = new List<Parameter>();
        public bool loadAdrConfigOnStart;
        public string configurationFileName = "parameter-config";
        Scenario m_Scenario;

        public string configurationFilePath =>
            Application.dataPath + "/StreamingAssets/" + configurationFileName + ".json";

        /// <summary>
        /// Returns the scenario component attached to this configuration
        /// </summary>
        public Scenario scenario
        {
            get
            {
                if (m_Scenario == null)
                {
                    m_Scenario = GetComponent<Scenario>();
                    return m_Scenario;
                }
                return m_Scenario;
            }
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

        void OnEnable()
        {
            ActiveConfig = this;
            if (loadAdrConfigOnStart)
                Deserialize();

            m_Scenario = GetComponent<Scenario>();
            if (m_Scenario == null)
                throw new ParameterConfigurationException("Missing Scenario component");
        }

        void OnDisable()
        {
            s_ActiveConfig = null;
        }

        void Start()
        {
            ValidateParameters();
            m_Scenario.Initialize();
            StartCoroutine(UpdateLoop());
        }

        IEnumerator UpdateLoop()
        {
            yield return null;
            while (!m_Scenario.isScenarioComplete)
            {
                foreach (var parameter in parameters)
                    parameter.Apply(m_Scenario.currentIteration);
                m_Scenario.Setup();

                while (!m_Scenario.isIterationComplete)
                {
                    m_Scenario.NextFrame();
                    yield return null;
                }

                m_Scenario.Teardown();
                m_Scenario.Iterate();
            }
            m_Scenario.OnComplete();
            StopExecution();
        }

        void ValidateParameters()
        {
            var names = new Dictionary<string, int>();
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (names.ContainsKey(parameter.parameterName))
                    throw new ParameterConfigurationException(
                        $"The parameters at indices {names[parameter.parameterName]} and {i} " +
                        $"cannot share the name \"{parameter.parameterName}\"");
                names[parameter.parameterName] = i;
            }
            foreach (var parameter in parameters)
                parameter.Validate();
        }

        static void StopExecution()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        internal void Serialize()
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new ParameterConfigurationJsonConverter(this) }
            };
            var jsonString = JsonConvert.SerializeObject(this, Formatting.Indented, settings);
            Directory.CreateDirectory(Application.dataPath + "/StreamingAssets/");
            using (var writer = new StreamWriter(configurationFilePath, false))
            {
                writer.Write(jsonString);
            }
        }

        internal void Deserialize()
        {
            if (!File.Exists(configurationFilePath))
                throw new ParameterConfigurationException($"Parameter JSON configuration file does not exist at path {configurationFilePath}");
            var jsonText = File.ReadAllText(configurationFilePath);
            JsonConvert.DeserializeObject<ParameterConfiguration>(
                jsonText, new ParameterConfigurationJsonConverter(this));
        }
    }
}
