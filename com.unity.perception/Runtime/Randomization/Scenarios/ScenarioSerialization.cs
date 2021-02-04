using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace UnityEngine.Perception.Randomization.Scenarios.Serialization
{
    public static class ScenarioSerialization
    {
        [MenuItem("Tests/Deserialize Test")]
        public static void DeserializeTest()
        {
            var jsonString = File.ReadAllText($"{Application.streamingAssetsPath}/data.json");
            var schema = JsonConvert.DeserializeObject<IncomingSchema>(jsonString);
        }
    }

    public class IncomingSchema
    {
        public Dictionary<string, Group> groups;
    }

    public class MetaData
    {
        public string name = string.Empty;
        public string description = string.Empty;
    }

    public class Group
    {
        public MetaData metadata;

        [JsonConverter(typeof(GroupItemsConverter))]
        public Dictionary<string, IGroupItem> items;
    }

    public interface IGroupItem { }

    public class GroupItemsConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanRead => true;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IGroupItem);
        }

        public override void WriteJson(
            JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(
            JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var groupItems = new Dictionary<string, IGroupItem>();
            foreach (var property in jsonObject.Properties())
            {
                var value = (JObject)property.Value;
                var groupItem = value.ContainsKey("items") ? (IGroupItem)new Parameter() : new Scalar();
                serializer.Populate(value.CreateReader(), groupItem);
                groupItems.Add(property.Name, groupItem);
            }
            return groupItems;
        }
    }

    public interface IParameterItem { }

    public class ParameterItemsConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanRead => true;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IGroupItem);
        }

        public override void WriteJson(
            JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(
            JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var groupItems = new Dictionary<string, IGroupItem>();
            foreach (var property in jsonObject.Properties())
            {
                var value = (JObject)property.Value;
                var groupItem = value.ContainsKey("items") ? (IGroupItem)new SamplerOptions() : new Scalar();
                serializer.Populate(value.CreateReader(), groupItem);
                groupItems.Add(property.Name, groupItem);
            }
            return groupItems;
        }
    }

    public class Parameter : IGroupItem
    {
        public MetaData metadata;
        public Dictionary<string, IParameterItem> items;
    }

    public interface IScalarType { }

    public class Scalar : IGroupItem, IParameterItem
    {
        public MetaData metadata;
        public IScalarType scalarData;
    }

    public class StringScalarType : IScalarType
    {
        public string value;
    }

    public class DoubleScalarType : IScalarType
    {
        public double value;
    }

    public class BooleanScalarType : IScalarType
    {
        public bool value;
    }

    public interface ISamplerOption { }

    public class SamplerOptions : IParameterItem
    {
        public MetaData metadata;
        public List<ISamplerOption> options;
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
        public float value;
    }
}
