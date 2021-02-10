using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;

namespace UnityEngine.Perception.Randomization.Scenarios.Serialization
{
    public static class ScenarioSerialization
    {
        [MenuItem("Tests/Deserialize Test")]
        public static void DeserializeTest()
        {
            var jsonString = File.ReadAllText($"{Application.streamingAssetsPath}/data.json");
            var schema = JsonConvert.DeserializeObject<TemplateConfigurationOptions>(jsonString);
            var backToJson = JsonConvert.SerializeObject(schema, Formatting.Indented);
            Debug.Log(backToJson);
        }
    }

    #region Interfaces
    public interface IGroupItem { }

    public interface IParameterItem { }

    public interface ISamplerOption { }

    public interface IScalarValue { }
    #endregion

    #region GroupedObjects
    public class TemplateConfigurationOptions
    {
        public Dictionary<string, Group> groups;
    }

    public class StandardMetadata
    {
        public string name = string.Empty;
        public string description = string.Empty;
    }

    public class Group
    {
        public StandardMetadata metadata;
        [JsonConverter(typeof(GroupItemsConverter))]
        public Dictionary<string, IGroupItem> items;
    }

    public class Parameter : IGroupItem
    {
        public StandardMetadata metadata;
        [JsonConverter(typeof(ParameterItemsConverter))]
        public Dictionary<string, IParameterItem> items;
    }
    #endregion

    #region SamplerOptions
    [JsonConverter(typeof(SamplerOptionsConverter))]
    public class SamplerOptions : IParameterItem
    {
        public StandardMetadata metadata;
        public ISamplerOption defaultSampler;
    }

    public class UniformSampler : ISamplerOption
    {
        public double min;
        public double max;
    }

    public class NormalSampler : ISamplerOption
    {
        public double min;
        public double max;
        public double mean;
        public double standardDeviation;
    }

    public class ConstantSampler : ISamplerOption
    {
        public double value;
    }
    #endregion

    #region ScalarValues
    [JsonConverter(typeof(ScalarConverter))]
    public class Scalar : IGroupItem, IParameterItem
    {
        public StandardMetadata metadata;
        public IScalarValue value;
    }

    public class StringScalarValue : IScalarValue
    {
        public string str;
    }

    public class DoubleScalarValue : IScalarValue
    {
        public double num;
    }

    public class BooleanScalarValue : IScalarValue
    {
        public bool boolean;
    }
    #endregion
}
