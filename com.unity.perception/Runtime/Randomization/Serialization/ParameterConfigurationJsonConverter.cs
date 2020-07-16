/*
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Curriculum;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Parameters.ParameterSpace;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Serialization
{
    [Serializable]
    class InvalidParameterJsonException : Exception
    {
        public InvalidParameterJsonException(string message) : base(message) { }
        public InvalidParameterJsonException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ParameterConfigurationJsonConverter : JsonConverter
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

            // Serialize Parameters
            var parametersObj = new JObject();
            configObj["parameters"] = parametersObj;
            foreach (var parameter in m_Config.parameters)
            {
                var paramObj = new JObject();
                paramObj["type"] = parameter.space.TypeName;
                var spaceObj = JObject.FromObject(parameter.space, serializer);
                foreach (var property in spaceObj.Properties())
                    paramObj[property.Name] = property.Value;
                parametersObj[parameter.name] = paramObj;
            }

            // Serialize Curriculum
            JObject curriculumObj;
            switch (m_Config.curriculum.Type)
            {
                case "grid":
                    curriculumObj = SerializeGridCurriculum(serializer);
                    break;
                default:
                    throw new InvalidParameterJsonException(
                        $"Cannot serialize curriculum of type {m_Config.curriculum.Type}");
            }
            configObj["curriculum"] = curriculumObj;
            configObj.WriteTo(writer);
        }

        public override object ReadJson
            (JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            m_Config.parameters = ReadParameters(jo["parameters"]);
            m_Config.curriculum = ReadCurriculum(jo["curriculum"]);
            return m_Config;
        }

        JObject SerializeGridCurriculum(JsonSerializer serializer)
        {
            var gridCurriculum = (GridCurriculum)m_Config.curriculum;
            var jo = new JObject();
            jo["samplerPerCell"] = gridCurriculum.samplesPerCell;

            var gridDimensionsObj = new JObject();
            foreach (var gridSampler in gridCurriculum.gridSamplers)
            {
                var dimensionObj = new JObject();
                dimensionObj["binCount"] = gridSampler.binCount;
                gridDimensionsObj[gridSampler.parameter.name] = dimensionObj;
            }
            jo["gridDimensions"] = gridDimensionsObj;

            var executionRangeObj = new JObject();
            executionRangeObj["start"] = gridCurriculum.executionRange.start;
            executionRangeObj["length"] = gridCurriculum.executionRange.length;
            jo["executionRange"] = executionRangeObj;

            var samplersObj = new JObject();
            foreach (var sampler in gridCurriculum.randomSamplers)
            {
                var samplerObj = new JObject();
                samplerObj["type"] = sampler.Type;
                foreach (var property in JObject.FromObject(sampler, serializer))
                    samplerObj[property.Key] = property.Value;
                samplersObj[sampler.parameter.name] = samplerObj;
            }
            jo["randomSamplers"] = samplersObj;

            return jo;
        }

        static List<Parameter> ReadParameters(JToken token)
        {
            if (token.Type != JTokenType.Object)
                throw new InvalidParameterJsonException("Expected json object at parameter key");

            var parameters = new List<Parameter>();
            var properties = ((JObject)token).Properties();
            foreach (var prop in properties)
            {
                var value = (JObject)prop.Value;
                var name = prop.Name;
                var type = value["type"].Value<string>();
                var space = value["values"] != null ? GetArraySpace(value, type) : GetRangeSpace(value, type);
                parameters.Add(new Parameter
                {
                    name = name,
                    space = space
                });
            }
            return parameters;
        }

        static ParameterSpaceBase GetRangeSpace(JObject jo, string type)
        {
            switch (type)
            {
                case "int":
                    return jo.ToObject<IntRangeSpace>();
                case "float":
                    return jo.ToObject<FloatRangeSpace>();
                default:
                    throw new InvalidParameterJsonException($"Unknown range parameter type: {type}");
            }
        }

        static ParameterSpaceBase GetArraySpace(JObject jo, string type)
        {
            switch (type)
            {
                case "bool":
                    return jo.ToObject<BoolArraySpace>();
                case "int":
                    return jo.ToObject<IntArraySpace>();
                case "float":
                    return jo.ToObject<FloatArraySpace>();
                case "Vector3":
                    return jo.ToObject<Vector3ArraySpace>();
                case "Color":
                    return jo.ToObject<ColorArraySpace>();
                case "String":
                    return jo.ToObject<StringArraySpace>();
                default:
                    throw new InvalidParameterJsonException($"Unknown array parameter type: {type}");
            }
        }

        CurriculumBase ReadCurriculum(JToken token)
        {
            if (token.Type != JTokenType.Object)
                throw new InvalidParameterJsonException("Expected json object at curriculum key");

            CurriculumBase curriculum;
            var jo = ((JObject)token);
            var type = jo["type"].Value<string>();
            switch (type)
            {
                case "grid":
                    curriculum = GetGridCurriculum(jo);
                    break;
                default:
                    throw new InvalidParameterJsonException($"Unknown curriculum type {type}");
            }

            return curriculum;
        }

        GridCurriculum GetGridCurriculum(JObject jo)
        {
            var gridCurriculum = new GridCurriculum();

            // Get samples per cell
            gridCurriculum.samplesPerCell = jo["samplesPerCell"].Value<int>();

            // Get execution range
            var executionRangeJo = jo["executionRange"];
            gridCurriculum.executionRange = new ExecutionRange
            {
                start = executionRangeJo["start"].Value<int>(),
                length = executionRangeJo["length"].Value<int>()
            };

            // Get grid dimensions as grid samplers
            var gridSamplers = new List<GridSampler>();
            foreach (var property in ((JObject)jo["gridDimensions"]).Properties())
            {
                var parameter = m_Config.GetParameter(property.Name);
                var sampler = new GridSampler(((JObject)property.Value)["binCount"].Value<int>());
                sampler.parameter = parameter;
                parameter.sampler = sampler;
                gridSamplers.Add(sampler);
            }
            gridCurriculum.gridSamplers = gridSamplers;

            // Get random samplers
            var samplers = new List<RandomSamplerBase>();
            foreach (var property in ((JObject)jo["randomSamplers"]).Properties())
                samplers.Add(GetRandomSampler(property));
            gridCurriculum.randomSamplers = samplers;

            return gridCurriculum;
        }

        RandomSamplerBase GetRandomSampler(JProperty property)
        {
            var parameter = m_Config.GetParameter(property.Name);
            var value = (JObject)property.Value;
            var type = value["type"].Value<string>();

            RandomSamplerBase sampler;
            switch (type)
            {
                case "uniform":
                    sampler = value.ToObject<UniformRandomSampler>();
                    break;
                case "normal":
                    sampler = value.ToObject<NormalDistributionSampler>();
                    break;
                default:
                    throw new InvalidParameterJsonException($"Unknown sampler type {type}");
            }

            sampler.parameter = parameter;
            parameter.sampler = sampler;
            return sampler;
        }
    }
}
*/
