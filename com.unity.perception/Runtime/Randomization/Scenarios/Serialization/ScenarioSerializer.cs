using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Scenarios.Serialization
{
    static class ScenarioSerializer
    {
        #region Serialization
        public static string SerializeToJsonString(ScenarioBase scenario)
        {
            return JsonConvert.SerializeObject(SerializeToJsonObject(scenario), Formatting.Indented);
        }

        public static void SerializeToFile(ScenarioBase scenario, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            using (var writer = new StreamWriter(filePath, false))
                writer.Write(SerializeToJsonString(scenario));
        }

        public static JObject SerializeToJsonObject(ScenarioBase scenario)
        {
            return new JObject
            {
                ["constants"] = SerializeConstants(scenario.genericConstants),
                ["randomizers"] = JObject.FromObject(SerializeScenarioToTemplate(scenario))
            };
        }

        static JObject SerializeConstants(ScenarioConstants constants)
        {
            var constantsObj = new JObject();
            var constantsFields = constants.GetType().GetFields();
            foreach (var constantsField in constantsFields)
                constantsObj.Add(new JProperty(constantsField.Name, constantsField.GetValue(constants)));
            return constantsObj;
        }

        static TemplateConfigurationOptions SerializeScenarioToTemplate(ScenarioBase scenario)
        {
            return new TemplateConfigurationOptions
            {
                randomizerGroups = SerializeRandomizers(scenario.randomizers)
            };
        }

        static List<Group> SerializeRandomizers(IEnumerable<Randomizer> randomizers)
        {
            var serializedRandomizers = new List<Group>();
            foreach (var randomizer in randomizers)
            {
                var randomizerData = SerializeRandomizer(randomizer);
                if (randomizerData.items.Count == 0 && !randomizerData.state.canBeSwitchedByUser)
                    continue;

                serializedRandomizers.Add(randomizerData);
            }
            return serializedRandomizers;
        }

        static Group SerializeRandomizer(Randomizer randomizer)
        {
            var randomizerData = new Group();
            randomizerData.state = new RandomizerStateData
            {
                enabled = randomizer.enabled,
                canBeSwitchedByUser = randomizer.enabledStateCanBeSwitchedByUser
            };

            var fields = randomizer.GetType().GetFields();
            randomizerData.randomizerId = randomizer.GetType().Name;
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
                    value = constantSampler.value,
                    limits = constantSampler.shouldCheckValidRange ? new Limits
                    {
                        min = constantSampler.minAllowed,
                        max = constantSampler.maxAllowed,
                    } : null
                };
            else if (sampler is Samplers.UniformSampler uniformSampler)
                samplerData.defaultSampler = new UniformSampler
                {
                    min = uniformSampler.range.minimum,
                    max = uniformSampler.range.maximum,
                    limits = uniformSampler.shouldCheckValidRange ? new Limits
                    {
                        min = uniformSampler.minAllowed,
                        max = uniformSampler.maxAllowed,
                    } : null
                };
            else if (sampler is Samplers.NormalSampler normalSampler)
                samplerData.defaultSampler = new NormalSampler
                {
                    min = normalSampler.range.minimum,
                    max = normalSampler.range.maximum,
                    mean = normalSampler.mean,
                    stddev = normalSampler.standardDeviation,
                    limits = normalSampler.shouldCheckValidRange ? new Limits
                    {
                        min = normalSampler.minAllowed,
                        max = normalSampler.maxAllowed,
                    } : null
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
            {
                var minAllowed = 0f;
                var maxAllowed = 0f;
                var shouldCheckValidRange = false;
                var rangeAttributes = field.GetCustomAttributes(typeof(RangeAttribute));
                if (rangeAttributes.Any())
                {
                    var rangeAttribute = (RangeAttribute)rangeAttributes.First();
                    minAllowed = rangeAttribute.min;
                    maxAllowed = rangeAttribute.max;
                    shouldCheckValidRange = true;
                }
                return new DoubleScalarValue
                {
                    num = Convert.ToDouble(field.GetValue(obj)),
                    limits = shouldCheckValidRange ? new Limits
                    {
                        min = minAllowed,
                        max = maxAllowed,
                    } : null
                };
            }

            return null;
        }

        #endregion

        #region Deserialization
        public static void Deserialize(ScenarioBase scenario, string json)
        {
            var jsonData = JObject.Parse(json);
            if (jsonData.ContainsKey("constants"))
                DeserializeConstants(scenario.genericConstants, (JObject)jsonData["constants"]);
            if (jsonData.ContainsKey("randomizers"))
                DeserializeTemplateIntoScenario(
                    scenario, jsonData["randomizers"].ToObject<TemplateConfigurationOptions>());
        }

        static void DeserializeConstants(ScenarioConstants constants, JObject constantsData)
        {
            JsonUtility.FromJsonOverwrite(constantsData.ToString(), constants);
        }

        static void DeserializeTemplateIntoScenario(ScenarioBase scenario, TemplateConfigurationOptions template)
        {
            DeserializeRandomizers(scenario.randomizers, template.randomizerGroups);
        }

        static void DeserializeRandomizers(IEnumerable<Randomizer> randomizers, List<Group> groups)
        {
            var randomizerTypeMap = new Dictionary<string, Randomizer>();
            foreach (var randomizer in randomizers)
                randomizerTypeMap.Add(randomizer.GetType().Name, randomizer);

            foreach (var randomizerData in groups)
            {
                if (!randomizerTypeMap.ContainsKey(randomizerData.randomizerId))
                    continue;
                var randomizer = randomizerTypeMap[randomizerData.randomizerId];
                DeserializeRandomizer(randomizer, randomizerData);
            }
        }

        static void DeserializeRandomizer(Randomizer randomizer, Group randomizerData)
        {
            if (randomizerData.state != null)
            {
                randomizer.enabled = randomizerData.state.enabled;
                randomizer.enabledStateCanBeSwitchedByUser = randomizerData.state.canBeSwitchedByUser;
            }

            foreach (var pair in randomizerData.items)
            {
                var field = randomizer.GetType().GetField(pair.Key);
                if (field == null)
                    continue;
                if (pair.Value is Parameter parameterData)
                    DeserializeParameter((Randomization.Parameters.Parameter)field.GetValue(randomizer), parameterData);
                else
                    DeserializeScalarValue(randomizer, field, (Scalar)pair.Value);
            }
        }

        static void DeserializeParameter(Randomization.Parameters.Parameter parameter, Parameter parameterData)
        {
            foreach (var pair in parameterData.items)
            {
                var field = parameter.GetType().GetField(pair.Key);
                if (field == null)
                    continue;
                if (pair.Value is SamplerOptions samplerOptions)
                    field.SetValue(parameter, DeserializeSampler(samplerOptions.defaultSampler));
                else
                    DeserializeScalarValue(parameter, field, (Scalar)pair.Value);
            }
        }

        static ISampler DeserializeSampler(ISamplerOption samplerOption)
        {
            if (samplerOption is ConstantSampler constantSampler)
                return new Samplers.ConstantSampler
                {
                    value = (float)constantSampler.value,
                    minAllowed = constantSampler.limits != null ? (float)constantSampler.limits.min : 0,
                    maxAllowed = constantSampler.limits != null ? (float)constantSampler.limits.max : 0,
                    shouldCheckValidRange = constantSampler.limits != null
                };
            if (samplerOption is UniformSampler uniformSampler)
                return new Samplers.UniformSampler
                {
                    range = new FloatRange
                    {
                        minimum = (float)uniformSampler.min,
                        maximum = (float)uniformSampler.max,
                    },
                    minAllowed = uniformSampler.limits != null ? (float)uniformSampler.limits.min : 0,
                    maxAllowed = uniformSampler.limits != null ? (float)uniformSampler.limits.max : 0,
                    shouldCheckValidRange = uniformSampler.limits != null
                };
            if (samplerOption is NormalSampler normalSampler)
                return new Samplers.NormalSampler
                {
                    range = new FloatRange
                    {
                        minimum = (float)normalSampler.min,
                        maximum = (float)normalSampler.max
                    },
                    mean = (float)normalSampler.mean,
                    standardDeviation = (float)normalSampler.stddev,
                    minAllowed = normalSampler.limits != null ? (float)normalSampler.limits.min : 0,
                    maxAllowed = normalSampler.limits != null ? (float)normalSampler.limits.max : 0,
                    shouldCheckValidRange = normalSampler.limits != null
                };
            throw new ArgumentException($"Cannot deserialize unsupported sampler type {samplerOption.GetType()}");
        }

        static void DeserializeScalarValue(object obj, FieldInfo field, Scalar scalar)
        {
            var rangeAttributes = field.GetCustomAttributes(typeof(RangeAttribute));
            RangeAttribute rangeAttribute = null;

            if (rangeAttributes.Any())
            {
                rangeAttribute = (RangeAttribute)rangeAttributes.First();
            }

            var readScalar = ReadScalarValue(obj, scalar);
            var tolerance = 0.00001f;

            if (readScalar.Item1 is double num)
            {
                if (rangeAttribute != null)
                {
                    if (readScalar.Item2 != null &&
                        (Math.Abs(rangeAttribute.min - readScalar.Item2.min) > tolerance || Math.Abs(rangeAttribute.max - readScalar.Item2.max) > tolerance))
                    {
                        //the field has a range attribute and the json has a limits block for this field, but the numbers don't match
                        Debug.LogError($"The limits provided in the Scenario JSON for the field \"{field.Name}\" of \"{obj.GetType().Name}\" do not match this field's range set in the code. Ranges for scalar fields can only be set in code using the Range attribute and not from the Scenario JSON.");
                    }
                    else if (readScalar.Item2 == null)
                    {
                        //the field has a range attribute but the json has no limits block for this field
                        Debug.LogError($"The provided Scenario JSON specifies limits for the field \"{field.Name}\" of \"{obj.GetType().Name}\", while the field has no Range attribute in the code. Ranges for scalar fields can only be set in code using the Range attribute and not from the Scenario JSON.");
                    }

                    if (num < rangeAttribute.min || num > rangeAttribute.max)
                    {
                        Debug.LogError($"The provided value for the field \"{field.Name}\" of \"{obj.GetType().Name}\" exceeds the allowed valid range. Clamping to valid range.");
                        var clamped = Mathf.Clamp((float)num, rangeAttribute.min, rangeAttribute.max);
                        field.SetValue(obj, Convert.ChangeType(clamped, field.FieldType));
                    }
                    else
                        field.SetValue(obj, Convert.ChangeType(readScalar.Item1, field.FieldType));
                }
                else
                {
                    if (readScalar.Item2 != null)
                        //the field does not have a range attribute but the json has a limits block for this field
                        Debug.LogError($"The provided Scenario JSON specifies limits for the field \"{field.Name}\" of \"{obj.GetType().Name}\", but the field has no Range attribute in code. Ranges for scalar fields can only be set in code using the Range attribute and not from the Scenario JSON.");

                    field.SetValue(obj, Convert.ChangeType(readScalar.Item1, field.FieldType));
                }
            }
            else
            {
                field.SetValue(obj, Convert.ChangeType(readScalar.Item1, field.FieldType));
            }
        }

        static (object, Limits) ReadScalarValue(object obj, Scalar scalar)
        {
            object value;
            Limits limits = null;
            if (scalar.value is StringScalarValue stringValue)
                value = stringValue.str;
            else if (scalar.value is BooleanScalarValue booleanValue)
                value = booleanValue.boolean;
            else if (scalar.value is DoubleScalarValue doubleValue)
            {
                value = doubleValue.num;
                limits = doubleValue.limits;
            }
            else
                throw new ArgumentException(
                    $"Cannot deserialize unsupported scalar type {scalar.value.GetType()}");

            return (value, limits);
        }

        #endregion

        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}
