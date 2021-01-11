using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers;
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

        /// <inheritdoc/>
        public override ScenarioConstants genericConstants => constants;

        /// <inheritdoc/>
        public override string SerializeToJson()
        {
            var configObj = new JObject
            {
                ["constants"] = SerializeConstants(),
                ["randomizers"] = SerializeRandomizers()
            };
            return JsonConvert.SerializeObject(configObj, Formatting.Indented);
        }

        JObject SerializeConstants()
        {
            var constantsObj = new JObject();
            var constantsFields = constants.GetType().GetFields();
            foreach (var constantsField in constantsFields)
                constantsObj.Add(new JProperty(constantsField.Name, constantsField.GetValue(constants)));
            return constantsObj;
        }

        JObject SerializeRandomizers()
        {
            var randomizersObj = new JObject();
            foreach (var randomizer in m_Randomizers)
            {
                var randomizerObj = SerializeRandomizer(randomizer);
                if (randomizerObj.Count > 0)
                    randomizersObj.Add(new JProperty(randomizer.GetType().Name, randomizerObj));
            }
            return randomizersObj;
        }

        static JObject SerializeRandomizer(Randomizer randomizer)
        {
            var randomizerObj = new JObject();
            var parameterFields = randomizer.GetType().GetFields();
            foreach (var parameterField in parameterFields)
            {
                if (!IsSubclassOfRawGeneric(typeof(NumericParameter<>), parameterField.FieldType))
                    continue;
                var parameter = (Parameter)parameterField.GetValue(randomizer);
                var parameterObj = SerializeParameter(parameter);
                if (parameterObj.Count > 0)
                    randomizerObj.Add(new JProperty(parameterField.Name, parameterObj));
            }
            return randomizerObj;
        }

        static JObject SerializeParameter(Parameter parameter)
        {
            var parameterObj = new JObject();
            var samplerFields = parameter.GetType().GetFields();
            foreach (var samplerField in samplerFields)
            {
                if (samplerField.FieldType != typeof(ISampler))
                    continue;
                var sampler = (ISampler)samplerField.GetValue(parameter);
                var samplerObj = SerializeSampler(sampler);
                parameterObj.Add(new JProperty(samplerField.Name, samplerObj));
            }
            return parameterObj;
        }

        static JObject SerializeSampler(ISampler sampler)
        {
            var samplerObj = new JObject();
            var fields = sampler.GetType().GetFields();
            foreach (var field in fields)
            {
                samplerObj.Add(new JProperty(field.Name, JToken.FromObject(field.GetValue(sampler))));
            }

            if (sampler.GetType() != typeof(ConstantSampler))
            {
                var rangeProperty = sampler.GetType().GetProperty("range");
                if (rangeProperty != null)
                {
                    var range = (FloatRange)rangeProperty.GetValue(sampler);
                    var rangeObj = new JObject
                    {
                        new JProperty("minimum", range.minimum),
                        new JProperty("maximum", range.maximum)
                    };
                    samplerObj.Add(new JProperty("range", rangeObj));
                }
            }
            return samplerObj;
        }

        /// <inheritdoc/>
        public override void DeserializeFromFile(string configFilePath)
        {
            if (string.IsNullOrEmpty(configFilePath))
                throw new ArgumentNullException();
            if (!File.Exists(configFilePath))
                throw new FileNotFoundException($"A scenario configuration file does not exist at path {configFilePath}");
#if UNITY_EDITOR
            Debug.Log($"Deserialized scenario configuration from <a href=\"file:///${configFilePath}\">{configFilePath}</a>. " +
                "Using undo in the editor will revert these changes to your scenario.");
#else
            Debug.Log($"Deserialized scenario configuration from <a href=\"file:///${configFilePath}\">{configFilePath}</a>");
#endif
            var jsonText = File.ReadAllText(configFilePath);
            DeserializeFromJson(jsonText);
        }

        /// <inheritdoc/>
        public override void DeserializeFromJson(string json)
        {
            var jsonObj = JObject.Parse(json);
            var constantsObj = (JObject)jsonObj["constants"];
            DeserializeConstants(constantsObj);

            var randomizersObj = (JObject)jsonObj["randomizers"];
            DeserializeRandomizers(randomizersObj);
        }

        void DeserializeConstants(JObject constantsObj)
        {
            constants = constantsObj.ToObject<T>();
        }

        void DeserializeRandomizers(JObject randomizersObj)
        {
            var randomizerTypeMap = new Dictionary<string, Randomizer>();
            foreach (var randomizer in randomizers)
                randomizerTypeMap.Add(randomizer.GetType().Name, randomizer);

            foreach (var randomizerPair in randomizersObj)
            {
                if (!randomizerTypeMap.ContainsKey(randomizerPair.Key))
                    continue;
                var randomizer = randomizerTypeMap[randomizerPair.Key];
                var randomizerObj = (JObject)randomizerPair.Value;
                DeserializeRandomizer(randomizer, randomizerObj);
            }
        }

        static void DeserializeRandomizer(Randomizer randomizer, JObject randomizerObj)
        {
            foreach (var parameterPair in randomizerObj)
            {
                var parameterField = randomizer.GetType().GetField(parameterPair.Key);
                if (parameterField == null)
                    continue;
                var parameter = (Parameter)parameterField.GetValue(randomizer);
                var parameterObj = (JObject)parameterPair.Value;
                DeserializeParameter(parameter, parameterObj);
            }
        }

        static void DeserializeParameter(Parameter parameter, JObject parameterObj)
        {
            foreach (var samplerPair in parameterObj)
            {
                var samplerField = parameter.GetType().GetField(samplerPair.Key);
                if (samplerField == null)
                    continue;
                var sampler = (ISampler)samplerField.GetValue(parameter);
                var samplerObj = (JObject)samplerPair.Value;
                DeserializeSampler(sampler, samplerObj);
            }
        }

        static void DeserializeSampler(ISampler sampler, JObject samplerObj)
        {
            foreach (var samplerFieldPair in samplerObj)
            {
                if (samplerFieldPair.Key == "range")
                {
                    var rangeObj = (JObject)samplerFieldPair.Value;
                    sampler.range = new FloatRange(
                        rangeObj["minimum"].ToObject<float>(), rangeObj["maximum"].ToObject<float>());
                }
                else
                {
                    var field = sampler.GetType().GetField(samplerFieldPair.Key);
                    if (field != null)
                    {
                        field.SetValue(sampler, JsonConvert.DeserializeObject(samplerFieldPair.Value.ToString(), field.FieldType));
                    }
                }
            }
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
    }
}
