﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Serialization
{
    [Serializable]
    class InvalidParameterJsonException : Exception
    {
        public InvalidParameterJsonException(string message) : base(message) { }
        public InvalidParameterJsonException(string message, Exception innerException)
            : base(message, innerException) { }
    }

    class ParameterConfigurationJsonConverter : JsonConverter
    {
        ParameterConfiguration m_Config;

        public ParameterConfigurationJsonConverter(ParameterConfiguration config)
        {
            m_Config = config;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ParameterConfiguration);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var configObj = new JObject();
            var parametersObj = new JObject();
            configObj["parameters"] = parametersObj;
            foreach (var parameter in m_Config.parameters)
            {
                var fields = parameter.GetType().GetFields();
                foreach (var field in fields)
                {
                    var fieldValue = field.GetValue(parameter);
                    if (field.FieldType == typeof(Sampler) && fieldValue is RandomSampler sampler)
                    {
                        var floatRangeObj = new JObject();
                        parametersObj[parameter.parameterName + "." + field.Name] = floatRangeObj;
                        var range = sampler.range;
                        floatRangeObj["minimum"] = range.minimum;
                        floatRangeObj["maximum"] = range.maximum;
                    }
                }
            }

            configObj.WriteTo(writer);
        }

        public override object ReadJson
            (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            ReadParameters(jo["parameters"]);
            return m_Config;
        }

        void ReadParameters(JToken token)
        {
            if (token.Type != JTokenType.Object)
                throw new InvalidParameterJsonException("Expected json object at parameter key");

            var properties = ((JObject)token).Properties();
            foreach (var prop in properties)
            {
                var value = (JObject)prop.Value;
                var names = prop.Name.Split('.');
                var parameterName = names[0];
                var samplerFieldName = names[1];
                var parameter = m_Config.GetParameter(parameterName);

                var fields = parameter.GetType().GetFields();
                var foundField = false;
                foreach (var field in fields)
                {
                    if (field.Name != samplerFieldName)
                        continue;
                    var fieldValue = field.GetValue(parameter);
                    if (field.FieldType == typeof(Sampler) && fieldValue is RangedSampler sampler)
                    {
                        sampler.range.minimum = value["minimum"].Value<float>();
                        sampler.range.maximum = value["maximum"].Value<float>();
                        foundField = true;
                        break;
                    }
                }
                if (!foundField)
                    throw new ParameterConfigurationException($"Could not find parameter of name \"{parameterName}\" " +
                        $"with a RandomSampler field named \"{samplerFieldName}\"");
            }
        }
    }
}

