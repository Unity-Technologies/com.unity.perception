using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityEngine.Perception.Randomization.Scenarios.Serialization
{
    #region Interfaces
    interface IGroupItem { }

    interface IParameterItem { }

    interface ISamplerOption { }

    interface IScalarValue { }
    #endregion

    #region GroupedObjects
    class TemplateConfigurationOptions
    {
        public Dictionary<string, Group> groups = new Dictionary<string, Group>();
    }

    class StandardMetadata
    {
        public string name = string.Empty;
        public string description = string.Empty;
    }

    class Group
    {
        public StandardMetadata metadata = new StandardMetadata();
        [JsonConverter(typeof(GroupItemsConverter))]
        public Dictionary<string, IGroupItem> items = new Dictionary<string, IGroupItem>();
    }

    class Parameter : IGroupItem
    {
        public StandardMetadata metadata = new StandardMetadata();
        [JsonConverter(typeof(ParameterItemsConverter))]
        public Dictionary<string, IParameterItem> items = new Dictionary<string, IParameterItem>();
    }
    #endregion

    #region SamplerOptions
    [JsonConverter(typeof(SamplerOptionsConverter))]
    class SamplerOptions : IParameterItem
    {
        public StandardMetadata metadata = new StandardMetadata();
        public ISamplerOption defaultSampler;
    }

    class UniformSampler : ISamplerOption
    {
        public double min;
        public double max;
    }

    class NormalSampler : ISamplerOption
    {
        public double min;
        public double max;
        public double mean;
        public double standardDeviation;
    }

    class ConstantSampler : ISamplerOption
    {
        public double value;
    }
    #endregion

    #region ScalarValues
    [JsonConverter(typeof(ScalarConverter))]
    class Scalar : IGroupItem, IParameterItem
    {
        public StandardMetadata metadata = new StandardMetadata();
        public IScalarValue value;
    }

    class StringScalarValue : IScalarValue
    {
        public string str;
    }

    class DoubleScalarValue : IScalarValue
    {
        public double num;
    }

    class BooleanScalarValue : IScalarValue
    {
        [JsonProperty("bool")]
        public bool boolean;
    }
    #endregion
}
