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
    [AddComponentMenu("Randomization/ParameterConfiguration")]
    public class ParameterConfiguration : MonoBehaviour
    {
        static ParameterConfiguration s_Configuration;

        public static ParameterConfiguration Configuration
        {
            get => s_Configuration;
            private set
            {
                if (s_Configuration != null)
                    throw new ParameterConfigurationException("There cannot be more than one active ParameterConfiguration");
                s_Configuration = value;
            }
        }

        public bool loadAdrConfigOnStart;
        public string configurationFileName = "parameter-config";
        public Scenario scenario;

        [SerializeReference]
        public List<Parameter> parameters = new List<Parameter>();

        string ConfigurationFilePath =>
            Application.dataPath + "/StreamingAssets/" + configurationFileName + ".json";

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

        public void OnEnable()
        {
            Configuration = this;
            if (loadAdrConfigOnStart)
                Deserialize();
            if (scenario == null)
                throw new ParameterConfigurationException("Scenario is null");
            scenario.parameterConfiguration = this;
        }

        public void OnDisable()
        {
            s_Configuration = null;
        }

        void Start()
        {
            foreach (var parameter in parameters)
                parameter.Validate();
            scenario.Initialize();
            StartCoroutine(UpdateLoop());
        }

        IEnumerator UpdateLoop()
        {
            yield return null;
            while (!scenario.Complete)
            {
                foreach (var parameter in parameters)
                    parameter.Apply(scenario.CurrentIteration);
                scenario.Setup();

                while (scenario.Running)
                    yield return null;

                scenario.Teardown();
                scenario.Iterate();
            }
            StopExecution();
        }

        static void StopExecution()
        {
            Debug.Log("Stopping Execution");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Serialize()
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new ParameterConfigurationJsonConverter(this) }
            };
            var jsonString = JsonConvert.SerializeObject(this, Formatting.Indented, settings);
            Directory.CreateDirectory(Application.dataPath + "/StreamingAssets/");
            using (var writer = new StreamWriter(ConfigurationFilePath, false))
            {
                writer.Write(jsonString);
            }
        }

        public void Deserialize()
        {
            if (!File.Exists(ConfigurationFilePath))
                throw new ParameterConfigurationException($"Parameter JSON configuration file does not exist at path {ConfigurationFilePath}");
            var jsonText = File.ReadAllText(ConfigurationFilePath);
            JsonConvert.DeserializeObject<ParameterConfiguration>(
                jsonText, new ParameterConfigurationJsonConverter(this));
        }
    }
}
