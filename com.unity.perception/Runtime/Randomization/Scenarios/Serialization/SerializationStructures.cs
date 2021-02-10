using System.Collections.Generic;
using Newtonsoft.Json;

namespace UnityEngine.Perception.Randomization.Scenarios.Serialization
{
    #region Interfaces
    public interface IGroupItem { }

    public interface IParameterItem { }

    public interface ISamplerOption { }

    public interface IScalarValue { }
    #endregion

    #region GroupedObjects
    public class TemplateConfigurationOptions
    {
        public Dictionary<string, Group> groups = new Dictionary<string, Group>();
    }

    public class StandardMetadata
    {
        public string name = string.Empty;
        public string description = string.Empty;
    }

    public class Group
    {
        public StandardMetadata metadata = new StandardMetadata();
        [JsonConverter(typeof(GroupItemsConverter))]
        public Dictionary<string, IGroupItem> items = new Dictionary<string, IGroupItem>();
    }

    public class Parameter : IGroupItem
    {
        public StandardMetadata metadata = new StandardMetadata();
        [JsonConverter(typeof(ParameterItemsConverter))]
        public Dictionary<string, IParameterItem> items = new Dictionary<string, IParameterItem>();
    }
    #endregion

    #region SamplerOptions
    [JsonConverter(typeof(SamplerOptionsConverter))]
    public class SamplerOptions : IParameterItem
    {
        public StandardMetadata metadata = new StandardMetadata();
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
        public StandardMetadata metadata = new StandardMetadata();
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
