using System;
using System.CodeDom;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Scenarios
{
    /// <summary>
    /// The base class of scenarios with serializable constants
    /// </summary>
    /// <typeparam name="T">The type of scenario constants to serialize</typeparam>
    public abstract class Scenario<T> : ScenarioBase where T : ScenarioConstants, new()
    {
        /// <summary>
        /// A construct containing serializable constants that control the execution of this scenario
        /// </summary>
        public T constants = new T();

        /// <summary>
        /// Returns this scenario's non-typed serialized constants
        /// </summary>
        public override ScenarioConstants genericConstants => constants;

        /// <summary>
        /// Serializes this scenario's constants to a json file in the Unity StreamingAssets folder
        /// </summary>
        public override void Serialize()
        {
            SerializeRandomizers();
            Directory.CreateDirectory(Application.dataPath + "/StreamingAssets/");
            using (var writer = new StreamWriter(serializedConstantsFilePath, false))
                writer.Write(JsonUtility.ToJson(constants, true));
        }

        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck) {
            while (toCheck != null && toCheck != typeof(object)) {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        void SerializeRandomizers()
        {
            var configObj = new JObject();
            var randomizersObj = new JObject();
            configObj["randomizers"] = randomizersObj;
            foreach (var randomizer in m_Randomizers)
            {
                var randomizerObj = new JObject();
                var parameterFields = randomizer.GetType().GetFields();
                foreach (var parameterField in parameterFields)
                {
                    if (!IsSubclassOfRawGeneric(typeof(NumericParameter<>), parameterField.FieldType))
                        continue;
                    var parameter = parameterField.GetValue(randomizer);
                    var parameterObj = new JObject();
                    var samplerFields = parameter.GetType().GetFields();
                    foreach (var samplerField in samplerFields)
                    {
                        if (samplerField.FieldType != typeof(ISampler))
                            continue;
                        var sampler = samplerField.GetValue(parameter);
                        var samplerObj = new JObject();
                        var fields = sampler.GetType().GetFields();
                        Debug.Log(fields.Length);
                        foreach (var field in fields)
                        {
                            samplerObj.Add(new JProperty(field.Name, field.GetValue(sampler)));
                        }
                        parameterObj.Add(new JProperty(samplerField.Name, samplerObj));
                    }
                    if (parameterObj.Count > 0)
                        randomizerObj.Add(new JProperty(parameterField.Name, parameterObj));
                }
                if (randomizerObj.Count > 0)
                    randomizersObj.Add(new JProperty(randomizer.GetType().Name, randomizerObj));
            }
            Debug.Log(JsonConvert.SerializeObject(configObj, Formatting.Indented));
        }

        /// <summary>
        /// Deserializes this scenario's constants from a json file in the Unity StreamingAssets folder
        /// </summary>
        /// <exception cref="ScenarioException"></exception>
        public override void Deserialize()
        {
            if (string.IsNullOrEmpty(serializedConstantsFilePath))
            {
                Debug.Log("No constants file specified. Running scenario with built in constants.");
            }
            else if (File.Exists(serializedConstantsFilePath))
            {
                var jsonText = File.ReadAllText(serializedConstantsFilePath);
                constants = JsonUtility.FromJson<T>(jsonText);
            }
            else
            {
                Debug.LogWarning($"JSON scenario constants file does not exist at path {serializedConstantsFilePath}");
            }
        }
    }
}
