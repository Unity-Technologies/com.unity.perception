using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Curriculum;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Scenarios;

namespace UnityEngine.Perception.Randomization.Configuration
{
    public class ParameterConfiguration : MonoBehaviour
    {
        public int iterationCount;
        public string configurationFileName;
        public Scenario scenario;
        public CurriculumBase curriculum;

        [SerializeReference]
        public List<Parameter> parameters = new List<Parameter>();

        string ConfigurationFilePath =>
            Application.dataPath + "/StreamingAssets/" + configurationFileName + ".json";

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

        public void Awake()
        {
            // Deserialize();
            curriculum.Initialize();
            if (scenario == null)
                scenario = gameObject.AddComponent<DefaultScenario>();
            scenario.parameterConfiguration = this;
        }

        void Start()
        {
            StartCoroutine(UpdateLoop());
        }

        IEnumerator UpdateLoop()
        {
            yield return null;
            scenario.Initialize();
            while (!curriculum.FinishedIterating)
            {
                scenario.Setup();
                while (scenario.Running)
                    yield return null;
                scenario.Teardown();
                curriculum.Iterate();
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

        // string Serialize()
        // {
        //     var converters = new List<JsonConverter> { new ParameterConfigurationJsonConverter(this) };
        //     var settings = new JsonSerializerSettings
        //     {
        //         ContractResolver = new ParameterConfigurationContractResolver(),
        //         Converters = converters
        //     };
        //     return JsonConvert.SerializeObject(this, Formatting.Indented, settings);
        // }
        //
        // void Deserialize()
        // {
        //     var jsonText = System.IO.File.ReadAllText(ConfigurationFilePath);
        //     JsonConvert.DeserializeObject<ParameterConfiguration>(
        //         jsonText, new ParameterConfigurationJsonConverter(this));
        //     Debug.Log(Serialize());
        // }
    }
}
