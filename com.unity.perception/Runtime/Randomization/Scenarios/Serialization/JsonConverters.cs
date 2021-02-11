using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEngine.Perception.Randomization.Scenarios.Serialization
{
    class GroupItemsConverter : JsonConverter
    {
        public override bool CanWrite => true;

        public override bool CanRead => true;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IGroupItem);
        }

        public override void WriteJson(
            JsonWriter writer, object value, JsonSerializer serializer)
        {
            var output = new JObject();
            var groupItems = (Dictionary<string, IGroupItem>)value;
            foreach (var itemKey in groupItems.Keys)
            {
                var itemValue = groupItems[itemKey];
                var newObj = new JObject();
                if (itemValue is Parameter)
                    newObj["param"] = JObject.FromObject(itemValue);
                else
                    newObj["scalar"] = JObject.FromObject(itemValue);
                output[itemKey] = newObj;
            }
            output.WriteTo(writer);
        }

        public override object ReadJson(
            JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var groupItems = new Dictionary<string, IGroupItem>();
            foreach (var property in jsonObject.Properties())
            {
                var value = (JObject)property.Value;
                IGroupItem groupItem;
                if (value.ContainsKey("param"))
                    groupItem = serializer.Deserialize<Parameter>(value["param"].CreateReader());
                else if (value.ContainsKey("scalar"))
                    groupItem = serializer.Deserialize<Scalar>(value["scalar"].CreateReader());
                else
                    throw new KeyNotFoundException("No GroupItem key found");
                groupItems.Add(property.Name, groupItem);
            }
            return groupItems;
        }
    }

    class ParameterItemsConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IParameterItem);
        }

        public override void WriteJson(
            JsonWriter writer, object value, JsonSerializer serializer)
        {
            var output = new JObject();
            var parameterItems = (Dictionary<string, IParameterItem>)value;
            foreach (var itemKey in parameterItems.Keys)
            {
                var itemValue = parameterItems[itemKey];
                var newObj = new JObject();
                if (itemValue is SamplerOptions)
                    newObj["samplerOptions"] = JObject.FromObject(itemValue);
                else
                    newObj["scalar"] = JObject.FromObject(itemValue);
                output[itemKey] = newObj;
            }
            output.WriteTo(writer);
        }

        public override object ReadJson(
            JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var parameterItems = new Dictionary<string, IParameterItem>();
            foreach (var property in jsonObject.Properties())
            {
                var value = (JObject)property.Value;
                IParameterItem parameterItem;
                if (value.ContainsKey("samplerOptions"))
                    parameterItem = serializer.Deserialize<SamplerOptions>(value["samplerOptions"].CreateReader());
                else if (value.ContainsKey("scalar"))
                    parameterItem = serializer.Deserialize<Scalar>(value["scalar"].CreateReader());
                else
                    throw new KeyNotFoundException("No ParameterItem key found");
                parameterItems.Add(property.Name, parameterItem);
            }
            return parameterItems;
        }
    }

    class SamplerOptionsConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SamplerOptions);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var options = (SamplerOptions)value;
            var output = new JObject { ["metadata"] = JObject.FromObject(options.metadata) };

            string key;
            if (options.defaultSampler is ConstantSampler)
                key = "constant";
            else if (options.defaultSampler is UniformSampler)
                key = "uniform";
            else if (options.defaultSampler is NormalSampler)
                key = "normal";
            else
                throw new TypeAccessException($"Cannot serialize type ${options.defaultSampler.GetType()}");
            output[key] = JObject.FromObject(options.defaultSampler);
            output.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var samplerOption = new SamplerOptions { metadata = jsonObject["metadata"].ToObject<StandardMetadata>() };

            if (jsonObject.ContainsKey("constant"))
                samplerOption.defaultSampler = jsonObject["constant"].ToObject<ConstantSampler>();
            else if (jsonObject.ContainsKey("uniform"))
                samplerOption.defaultSampler = jsonObject["uniform"].ToObject<UniformSampler>();
            else if (jsonObject.ContainsKey("normal"))
                samplerOption.defaultSampler = jsonObject["normal"].ToObject<NormalSampler>();
            else
                throw new KeyNotFoundException("No valid SamplerOption key type found");

            return samplerOption;
        }
    }

    class ScalarConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Scalar);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var value = (JObject)jsonObject["value"];
            var scalar = new Scalar { metadata = jsonObject["metadata"].ToObject<StandardMetadata>() };

            if (value.ContainsKey("str"))
                scalar.value = new StringScalarValue { str = value["str"].Value<string>() };
            else if (value.ContainsKey("num"))
                scalar.value  = new DoubleScalarValue { num = value["num"].Value<double>() };
            else if (value.ContainsKey("bool"))
                scalar.value  = new BooleanScalarValue { boolean = value["bool"].Value<bool>() };
            else
                throw new KeyNotFoundException("No valid ScalarValue key type found");
            return scalar;
        }
    }
}
