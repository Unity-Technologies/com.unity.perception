using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth.Consumers
{
    /// <summary>
    /// Builds a json representation of a message produced by a <see cref="IMessageProducer"/>
    /// </summary>
    public class JsonMessageBuilder : IMessageBuilder
    {
        /// <summary>
        /// JToken used to build json output
        /// </summary>
        protected JToken currentJToken { get; } = new JObject();
        /// <summary>
        /// Nested builders for JSON output
        /// </summary>
        protected Dictionary<string, JsonMessageBuilder> nestedValue { get; } = new Dictionary<string, JsonMessageBuilder>();
        /// <summary>
        /// Nested arrays for JSON output
        /// </summary>
        protected Dictionary<string, List<JsonMessageBuilder>> nestedArrays { get; } = new Dictionary<string, List<JsonMessageBuilder>>();

        /// <summary>
        /// Converts the message into a json Newtonsoft.Json.Linq.JToken
        /// </summary>
        /// <returns>JToken json representation of a message</returns>
        public JToken ToJson()
        {
            foreach (var n in nestedValue)
            {
                currentJToken[n.Key] = n.Value.ToJson();
            }

            foreach (var n in nestedArrays)
            {
                var jArray = new JArray();
                foreach (var o in n.Value)
                {
                    jArray.Add(o.ToJson());
                }

                currentJToken[n.Key] = jArray;
            }
            return currentJToken;
        }

        /// <summary>
        /// Adds a byte value to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddByte(string key, byte value)
        {
            currentJToken[key] = value;
        }

        /// <summary>
        /// Adds a char value to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddChar(string key, char value)
        {
            currentJToken[key] = value;
        }

        /// <summary>
        /// Adds an int value to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddInt(string key, int value)
        {
            currentJToken[key] = value;
        }

        /// <summary>
        /// Adds an unsigned int value to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddUInt(string key, uint value)
        {
            currentJToken[key] = value;
        }

        /// <summary>
        /// Adds a long int value to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddLong(string key, long value)
        {
            currentJToken[key] = value;
        }

        /// <summary>
        /// Adds a float value to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddFloat(string key, float value)
        {
            currentJToken[key] = value;
        }

        /// <summary>
        /// Adds a double value to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddDouble(string key, double value)
        {
            currentJToken[key] = value;
        }

        /// <summary>
        /// Adds a string value to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddString(string key, string value)
        {
            currentJToken[key] = value;
        }

        /// <summary>
        /// Adds a bool value to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddBool(string key, bool value)
        {
            currentJToken[key] = value;
        }

        /// <summary>
        /// Placeholder for support to write an encoded image to json. The default handler does not
        /// support this and throws a <exception cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="extension">Image extension for the image type, for example a PNG image would be "png"</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddEncodedImage(string key, string extension, byte[] value)
        {
            throw new NotSupportedException("No support for encoded images in base class. Please extend this class with a customer json builder");
        }

        /// <inheritdoc/>
        [Obsolete("AddByteArray has been deprecated, Use AddEncodedImage instead", true)]
        public void AddByteArray(string key, IEnumerable<byte> value) {}

        /// <summary>
        /// Adds an int array to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddIntArray(string key, IEnumerable<int> value)
        {
            currentJToken[key] = new JArray(value);
        }

        /// <summary>
        /// Adds an unsigned int array to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddUIntArray(string key, IEnumerable<uint> value)
        {
            currentJToken[key] = new JArray(value);
        }

        /// <summary>
        /// Adds a long int array to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddLongArray(string key, IEnumerable<long> value)
        {
            currentJToken[key] = new JArray(value);
        }

        /// <summary>
        /// Adds a float array to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddFloatArray(string key, IEnumerable<float> value)
        {
            currentJToken[key] = new JArray(value);
        }

        /// <summary>
        /// Adds a double array to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddDoubleArray(string key, IEnumerable<double> value)
        {
            currentJToken[key] = new JArray(value);
        }

        /// <summary>
        /// Adds an array of strings to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddStringArray(string key, IEnumerable<string> value)
        {
            currentJToken[key] = new JArray(value);
        }

        /// <summary>
        /// Adds a bool array to the json
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="value">The value to write out to json</param>
        public virtual void AddBoolArray(string key, IEnumerable<bool> value)
        {
            currentJToken[key] = new JArray(value);
        }

        /// <summary>
        /// Adds a tensor to the json. The default json handler does not support tensors
        /// and throws a <exception cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <param name="tensor">The value to write out to json</param>
        public virtual void AddTensor(string key, Tensor tensor)
        {
            throw new NotSupportedException("No support for tensors in base class. Please extend this class with a customer json builder");
        }

        /// <summary>
        /// Adds a nested json element to the json object
        /// </summary>
        /// <param name="key">The key of the json object</param>
        /// <returns>A handle to the nested json builder</returns>
        public virtual IMessageBuilder AddNestedMessage(string key)
        {
            var nested = new JsonMessageBuilder();

            if (nestedValue.ContainsKey(key))
            {
                Debug.LogWarning($"Report data with key [{key}] will be overridden by new values");
            }

            nestedValue[key] = nested;
            return nested;
        }

        /// <summary>
        /// Adds a nested json vector to the json object
        /// </summary>
        /// <param name="arrayKey">The key of the json object</param>
        /// <returns>A handle to the nested json builder</returns>
        public virtual IMessageBuilder AddNestedMessageToVector(string arrayKey)
        {
            if (!nestedArrays.TryGetValue(arrayKey, out var nestedList))
            {
                nestedList = new List<JsonMessageBuilder>();
                nestedArrays[arrayKey] = nestedList;
            }

            var nested = new JsonMessageBuilder();
            nestedList.Add(nested);
            return nested;
        }
    }
}
