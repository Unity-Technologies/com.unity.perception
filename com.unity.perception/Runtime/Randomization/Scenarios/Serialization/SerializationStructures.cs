using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityEngine.Perception.Randomization.Scenarios.Serialization
{
    #region Interfaces
    interface IGroupItem {}

    interface IParameterItem {}

    interface ISamplerOption {}

    interface IScalarValue {}
    #endregion

    #region GroupedObjects
    class TemplateConfigurationOptions
    {
        public List<Group> randomizerGroups = new List<Group>();
    }

    class StandardMetadata
    {
        public string name = string.Empty;
        public string description = string.Empty;
        public string imageLink = string.Empty;
    }

    class RandomizerStateData
    {
        public bool enabled;
        public bool canBeSwitchedByUser;
    }

    class Limits
    {
        public double min;
        public double max;
    }

    class Group
    {
        public string randomizerId;
        public StandardMetadata metadata = new StandardMetadata();
        public RandomizerStateData state = null;
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
        public Limits limits;
    }

    class NormalSampler : ISamplerOption
    {
        public double min;
        public double max;
        public double mean;
        public double stddev;
        public Limits limits;
    }

    class ConstantSampler : ISamplerOption
    {
        public double value;
        public Limits limits;
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
        public Limits limits;
    }

    class BooleanScalarValue : IScalarValue
    {
        [JsonProperty("bool")]
        public bool boolean;
    }
    #endregion
}
