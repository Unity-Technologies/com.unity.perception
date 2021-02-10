using System;
using System.Collections.Generic;
using System.IO;
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
            var randomizerObj = new Group();
            var parameterFields = randomizer.GetType().GetFields();
            foreach (var parameterField in parameterFields)
            {
                if (!IsSubclassOfRawGeneric(typeof(NumericParameter<>), parameterField.FieldType))
                    continue;
                var parameter = (Randomization.Parameters.Parameter)parameterField.GetValue(randomizer);
                var parameterData = SerializeParameter(parameter);
                if (parameterData.items.Count == 0)
                    continue;
                randomizerObj.items.Add(parameterField.Name, parameterData);
            }
            return randomizerObj;
        }

        static Parameter SerializeParameter(Randomization.Parameters.Parameter parameter)
        {
            var parameterData = new Parameter();
            var samplerFields = parameter.GetType().GetFields();
            foreach (var samplerField in samplerFields)
            {
                if (!samplerField.FieldType.IsAssignableFrom(typeof(ISampler)))
                    continue;
                var sampler = (ISampler)samplerField.GetValue(parameter);
                var samplerData = SerializeSampler(sampler);
                if (samplerData.defaultSampler == null)
                    continue;
                parameterData.items.Add(samplerField.Name, samplerData);
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
