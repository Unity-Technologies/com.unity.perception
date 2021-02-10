using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Scenarios.Serialization
{
    static class ScenarioTemplateSerializer
    {
        [MenuItem("Tests/Deserialize Test")]
        public static void DeserializeTest()
        {
            var jsonString = File.ReadAllText($"{Application.streamingAssetsPath}/data.json");
            var schema = JsonConvert.DeserializeObject<TemplateConfigurationOptions>(jsonString);
            var backToJson = JsonConvert.SerializeObject(schema, Formatting.Indented);
            Debug.Log(backToJson);
        }

        [MenuItem("Tests/Serialize Scenario To Json Test")]
        public static void SerializeScenarioToJsonTest()
        {
            var template = SerializeScenarioIntoTemplate(Object.FindObjectOfType<ScenarioBase>());
            Debug.Log(JsonConvert.SerializeObject(template, Formatting.Indented));
        }

        public static TemplateConfigurationOptions SerializeScenarioIntoTemplate(ScenarioBase scenario)
        {
            return new TemplateConfigurationOptions
            {
                groups = SerializeRandomizers(scenario.randomizers)
            };
        }

        static Dictionary<string, Group> SerializeRandomizers(IEnumerable<Randomizer> randomizers)
        {
            var serializedRandomizers = new Dictionary<string, Group>();
            foreach (var randomizer in randomizers)
            {
                var randomizerData = SerializeRandomizer(randomizer);
                if (randomizerData.items.Count == 0)
                    continue;
                serializedRandomizers.Add(randomizer.GetType().Name, randomizerData);
            }
            return serializedRandomizers;
        }

        static Group SerializeRandomizer(Randomizer randomizer)
        {
            var randomizerData = new Group();
            var fields = randomizer.GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.FieldType.IsSubclassOf(typeof(Randomization.Parameters.Parameter)))
                {
                    if (!IsSubclassOfRawGeneric(typeof(NumericParameter<>), field.FieldType))
                        continue;
                    var parameter = (Randomization.Parameters.Parameter)field.GetValue(randomizer);
                    var parameterData = SerializeParameter(parameter);
                    if (parameterData.items.Count == 0)
                        continue;
                    randomizerData.items.Add(field.Name, parameterData);
                }
                else
                {
                    var scalarValue = ScalarFromField(field, randomizer);
                    if (scalarValue != null)
                        randomizerData.items.Add(field.Name, new Scalar { value = scalarValue });
                }
            }
            return randomizerData;
        }

        static Parameter SerializeParameter(Randomization.Parameters.Parameter parameter)
        {
            var parameterData = new Parameter();
            var fields = parameter.GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.FieldType.IsAssignableFrom(typeof(ISampler)))
                {
                    var sampler = (ISampler)field.GetValue(parameter);
                    var samplerData = SerializeSampler(sampler);
                    if (samplerData.defaultSampler == null)
                        continue;
                    parameterData.items.Add(field.Name, samplerData);
                }
                else
                {
                    var scalarValue = ScalarFromField(field, parameter);
                    if (scalarValue != null)
                        parameterData.items.Add(field.Name, new Scalar { value = scalarValue });
                }
            }
            return parameterData;
        }

        static SamplerOptions SerializeSampler(ISampler sampler)
        {
            var samplerData = new SamplerOptions();
            if (sampler is Samplers.ConstantSampler constantSampler)
                samplerData.defaultSampler = new ConstantSampler
                {
                    value = constantSampler.value
                };
            else if (sampler is Samplers.UniformSampler uniformSampler)
                samplerData.defaultSampler = new UniformSampler
                {
                    min = uniformSampler.range.minimum,
                    max = uniformSampler.range.maximum
                };
            else if (sampler is Samplers.NormalSampler normalSampler)
                samplerData.defaultSampler = new NormalSampler
                {
                    min = normalSampler.range.minimum,
                    max = normalSampler.range.maximum,
                    mean = normalSampler.mean,
                    standardDeviation = normalSampler.standardDeviation
                };
            else
                throw new ArgumentException($"Invalid sampler type ({sampler.GetType()})");
            return samplerData;
        }

        static IScalarValue ScalarFromField(FieldInfo field, object obj)
        {
            if (field.FieldType == typeof(string))
                return new StringScalarValue { str = (string)field.GetValue(obj) };
            if (field.FieldType == typeof(bool))
                return new BooleanScalarValue { boolean = (bool)field.GetValue(obj) };
            if (field.FieldType == typeof(float) || field.FieldType == typeof(double) || field.FieldType == typeof(int))
                return new DoubleScalarValue { num = Convert.ToDouble(field.GetValue(obj)) };
            return null;
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
