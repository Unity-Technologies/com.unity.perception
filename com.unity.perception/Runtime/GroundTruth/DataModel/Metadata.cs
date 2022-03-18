using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// Metadata.
    /// </summary>
    public class Metadata : IMessageProducer
    {
        public Metadata()
        {
            m_Metadata = new Dictionary<string, MetadataEntry>();
        }

        Dictionary<string, MetadataEntry> m_Metadata;

        /// <inheritdoc/>
        public void ToMessage(IMessageBuilder builder)
        {
            foreach (var md in m_Metadata)
                switch (md.Value.valueType)
                {
                    case ValueType.Bool:
                        builder.AddBool(md.Key, (bool)md.Value.value);
                        break;
                    case ValueType.Int:
                        builder.AddInt(md.Key, (int)md.Value.value);
                        break;
                    case ValueType.UInt:
                        builder.AddUInt(md.Key, (uint)md.Value.value);
                        break;
                    case ValueType.Float:
                        builder.AddFloat(md.Key, (float)md.Value.value);
                        break;
                    case ValueType.String:
                        builder.AddString(md.Key, (string)md.Value.value);
                        break;
                    case ValueType.SubMetadata:
                    {
                        var nested = builder.AddNestedMessage(md.Key);
                        ((Metadata)md.Value.value).ToMessage(nested);
                        break;
                    }
                    case ValueType.IntArray:
                        builder.AddIntArray(md.Key, ((IEnumerable<int>)md.Value.value).ToArray());
                        break;
                    case ValueType.FloatArray:
                        builder.AddFloatArray(md.Key, ((IEnumerable<float>)md.Value.value).ToArray());
                        break;
                    case ValueType.StringArray:
                        builder.AddStringArray(md.Key, ((IEnumerable<string>)md.Value.value).ToArray());
                        break;
                    case ValueType.BoolArray:
                        builder.AddBoolArray(md.Key, ((IEnumerable<bool>)md.Value.value).ToArray());
                        break;
                    case ValueType.SubMetadataArray:
                    {
                        var children = (IEnumerable<Metadata>)md.Value.value;
                        foreach (var child in children)
                        {
                            var nested = builder.AddNestedMessageToVector(md.Key);
                            child.ToMessage(nested);
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
        }
        /// <summary>
        /// Adds a new metadata value
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public void Add(string key, int value)
        {
            m_Metadata[key] = new MetadataEntry
            {
                valueType = ValueType.Int,
                value = value
            };
        }
        /// <summary>
        /// Adds a new metadata value
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public void Add(string key, float value)
        {
            m_Metadata[key] = new MetadataEntry
            {
                valueType = ValueType.Float,
                value = value
            };
        }
        /// <summary>
        /// Adds a new metadata value
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public void Add(string key, uint value)
        {
            m_Metadata[key] = new MetadataEntry
            {
                valueType = ValueType.UInt,
                value = value
            };
        }

        /// <summary>
        /// Adds a new metadata value
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public void Add(string key, string value)
        {
            m_Metadata[key] = new MetadataEntry
            {
                valueType = ValueType.String,
                value = value
            };
        }
        /// <summary>
        /// Adds a new metadata value
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public void Add(string key, bool value)
        {
            m_Metadata[key] = new MetadataEntry
            {
                valueType = ValueType.Bool,
                value = value
            };
        }

        public void Add(string key, Metadata value)
        {
            m_Metadata[key] = new MetadataEntry
            {
                valueType = ValueType.SubMetadata,
                value = value
            };
        }

        /// <summary>
        /// Adds a new metadata value
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public void Add(string key, IEnumerable<int> value)
        {
            m_Metadata[key] = new MetadataEntry
            {
                valueType = ValueType.IntArray,
                value = value
            };
        }
        /// <summary>
        /// Adds a new metadata value
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public void Add(string key, IEnumerable<float> value)
        {
            m_Metadata[key] = new MetadataEntry
            {
                valueType = ValueType.FloatArray,
                value = value
            };
        }
        /// <summary>
        /// Adds a new metadata value
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public void Add(string key, IEnumerable<string> value)
        {
            m_Metadata[key] = new MetadataEntry
            {
                valueType = ValueType.StringArray,
                value = value
            };
        }
        /// <summary>
        /// Adds a new metadata value
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public void Add(string key, IEnumerable<bool> value)
        {
            m_Metadata[key] = new MetadataEntry
            {
                valueType = ValueType.BoolArray,
                value = value
            };
        }

        public void Add(string key, IEnumerable<Metadata> value)
        {
            m_Metadata[key] = new MetadataEntry
            {
                valueType = ValueType.SubMetadataArray,
                value = value
            };
        }

        /// <summary>
        /// Gets a value out of the metadata. If the value does not exist, or if a request is made for the improper
        /// data type, an exception will be thrown.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <returns>The metadata value</returns>
        /// <exception cref="ArgumentException">The key was not found in the metadata dictionary</exception>
        /// <exception cref="InvalidOperationException">The query was for the wrong data type</exception>
        public int GetInt(string key)
        {
            if (!m_Metadata.TryGetValue(key, out var value))
                throw new ArgumentException($"{key} does not exist in metadata");

            if (value.valueType != ValueType.Int)
                throw new InvalidOperationException($"{key} is not associated to an int");

            return (int)value.value;
        }

        /// <summary>
        /// Tries to get a value from the metadata. This method will fail if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <param name="value">The value associated with the key</param>
        /// <returns>Returns true if the data was properly retrieved. This method returns false if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.</returns>
        public bool TryGetValue(string key, out int value)
        {
            value = 0;
            if (!m_Metadata.TryGetValue(key, out var objValue))
                return false;

            if (objValue.valueType != ValueType.Int)
                return false;

            value = (int)objValue.value;

            return true;
        }
        /// <summary>
        /// Gets a value out of the metadata. If the value does not exist, or if a request is made for the improper
        /// data type, an exception will be thrown.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <returns>The metadata value</returns>
        /// <exception cref="ArgumentException">The key was not found in the metadata dictionary</exception>
        /// <exception cref="InvalidOperationException">The query was for the wrong data type</exception>
        public float GetFloat(string key)
        {
            if (!m_Metadata.TryGetValue(key, out var value))
                throw new ArgumentException($"{key} does not exist in metadata");

            if (value.valueType != ValueType.Float)
                throw new InvalidOperationException($"{key} is not associated to a float");

            return (float)value.value;
        }
        /// <summary>
        /// Tries to get a value from the metadata. This method will fail if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <param name="value">The value associated with the key</param>
        /// <returns>Returns true if the data was properly retrieved. This method returns false if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.</returns>
        public bool TryGetValue(string key, out float value)
        {
            value = 0;
            if (!m_Metadata.TryGetValue(key, out var objValue))
                return false;

            if (objValue.valueType != ValueType.Float)
                return false;

            value = (float)objValue.value;

            return true;
        }
        /// <summary>
        /// Gets a value out of the metadata. If the value does not exist, or if a request is made for the improper
        /// data type, an exception will be thrown.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <returns>The metadata value</returns>
        /// <exception cref="ArgumentException">The key was not found in the metadata dictionary</exception>
        /// <exception cref="InvalidOperationException">The query was for the wrong data type</exception>
        public string GetString(string key)
        {
            if (!m_Metadata.TryGetValue(key, out var value))
                throw new ArgumentException($"{key} does not exist in metadata");

            if (value.valueType != ValueType.String)
                throw new InvalidOperationException($"{key} is not associated to a string");

            return (string)value.value;
        }
        /// <summary>
        /// Tries to get a value from the metadata. This method will fail if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <param name="value">The value associated with the key</param>
        /// <returns>Returns true if the data was properly retrieved. This method returns false if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.</returns>
        public bool TryGetValue(string key, out string value)
        {
            value = string.Empty;
            if (!m_Metadata.TryGetValue(key, out var objValue))
                return false;

            if (objValue.valueType != ValueType.String)
                return false;

            value = (string)objValue.value;

            return true;
        }
        /// <summary>
        /// Gets a value out of the metadata. If the value does not exist, or if a request is made for the improper
        /// data type, an exception will be thrown.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <returns>The metadata value</returns>
        /// <exception cref="ArgumentException">The key was not found in the metadata dictionary</exception>
        /// <exception cref="InvalidOperationException">The query was for the wrong data type</exception>
        public uint GetUInt(string key)
        {
            if (!m_Metadata.TryGetValue(key, out var value))
                throw new ArgumentException($"{key} does not exist in metadata");

            if (value.valueType != ValueType.UInt)
                throw new InvalidOperationException($"{key} is not associated to a string");

            return (uint)value.value;
        }
        /// <summary>
        /// Tries to get a value from the metadata. This method will fail if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <param name="value">The value associated with the key</param>
        /// <returns>Returns true if the data was properly retrieved. This method returns false if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.</returns>
        public bool TryGetValue(string key, out uint value)
        {
            value = default;
            if (!m_Metadata.TryGetValue(key, out var objValue))
                return false;

            if (objValue.valueType != ValueType.UInt)
                return false;

            value = (uint)objValue.value;

            return true;
        }
        /// <summary>
        /// Gets a value out of the metadata. If the value does not exist, or if a request is made for the improper
        /// data type, an exception will be thrown.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <returns>The metadata value</returns>
        /// <exception cref="ArgumentException">The key was not found in the metadata dictionary</exception>
        /// <exception cref="InvalidOperationException">The query was for the wrong data type</exception>
        public bool GetBool(string key)
        {
            if (!m_Metadata.TryGetValue(key, out var value))
                throw new ArgumentException($"{key} does not exist in metadata");

            if (value.valueType != ValueType.Bool)
                throw new InvalidOperationException($"{key} is not associated to a bool");

            return (bool)value.value;
        }
        /// <summary>
        /// Tries to get a value from the metadata. This method will fail if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <param name="value">The value associated with the key</param>
        /// <returns>Returns true if the data was properly retrieved. This method returns false if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.</returns>
        public bool TryGetValue(string key, out bool value)
        {
            value = false;
            if (!m_Metadata.TryGetValue(key, out var objValue))
                return false;

            if (objValue.valueType != ValueType.Bool)
                return false;

            value = (bool)objValue.value;

            return true;
        }

        public Metadata GetSubMetadata(string key)
        {
            if (!m_Metadata.TryGetValue(key, out var value))
                throw new ArgumentException($"{key} does not exist in metadata");

            if (value.valueType != ValueType.SubMetadata)
                throw new InvalidOperationException($"{key} is not associated to sub-metadata");

            return (Metadata)value.value;
        }

        public bool TryGetValue(string key, out Metadata value)
        {
            value = null;
            if (!m_Metadata.TryGetValue(key, out var objValue))
                return false;

            if (objValue.valueType != ValueType.SubMetadata)
                return false;

            value = (Metadata)objValue.value;

            return true;
        }

        /// <summary>
        /// Gets a value out of the metadata. If the value does not exist, or if a request is made for the improper
        /// data type, an exception will be thrown.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <returns>The metadata value</returns>
        /// <exception cref="ArgumentException">The key was not found in the metadata dictionary</exception>
        /// <exception cref="InvalidOperationException">The query was for the wrong data type</exception>
        public int[] GetIntArray(string key)
        {
            if (!m_Metadata.TryGetValue(key, out var value))
                throw new ArgumentException($"{key} does not exist in metadata");

            if (value.valueType != ValueType.IntArray)
                throw new InvalidOperationException($"{key} is not associated to a int array");

            return (int[])value.value;
        }

        /// <summary>
        /// Tries to get a value from the metadata. This method will fail if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <param name="value">The value associated with the key</param>
        /// <returns>Returns true if the data was properly retrieved. This method returns false if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.</returns>
        public bool TryGetValue(string key, out int[] value)
        {
            value = null;
            if (!m_Metadata.TryGetValue(key, out var objValue))
                return false;

            if (objValue.valueType != ValueType.IntArray)
                return false;

            value = (int[])objValue.value;

            return true;
        }
        /// <summary>
        /// Gets a value out of the metadata. If the value does not exist, or if a request is made for the improper
        /// data type, an exception will be thrown.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <returns>The metadata value</returns>
        /// <exception cref="ArgumentException">The key was not found in the metadata dictionary</exception>
        /// <exception cref="InvalidOperationException">The query was for the wrong data type</exception>
        public float[] GetFloatArray(string key)
        {
            if (!m_Metadata.TryGetValue(key, out var value))
                throw new ArgumentException($"{key} does not exist in metadata");

            if (value.valueType != ValueType.FloatArray)
                throw new InvalidOperationException($"{key} is not associated to a float array");

            return (float[])value.value;
        }
        /// <summary>
        /// Tries to get a value from the metadata. This method will fail if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <param name="value">The value associated with the key</param>
        /// <returns>Returns true if the data was properly retrieved. This method returns false if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.</returns>
        public bool TryGetValue(string key, out float[] value)
        {
            value = null;
            if (!m_Metadata.TryGetValue(key, out var objValue))
                return false;

            if (objValue.valueType != ValueType.FloatArray)
                return false;

            value = (float[])objValue.value;

            return true;
        }
        /// <summary>
        /// Gets a value out of the metadata. If the value does not exist, or if a request is made for the improper
        /// data type, an exception will be thrown.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <returns>The metadata value</returns>
        /// <exception cref="ArgumentException">The key was not found in the metadata dictionary</exception>
        /// <exception cref="InvalidOperationException">The query was for the wrong data type</exception>
        public string[] GetStringArray(string key)
        {
            if (!m_Metadata.TryGetValue(key, out var value))
                throw new ArgumentException($"{key} does not exist in metadata");

            if (value.valueType != ValueType.StringArray)
                throw new InvalidOperationException($"{key} is not associated to a string array");

            return (string[])value.value;
        }
        /// <summary>
        /// Tries to get a value from the metadata. This method will fail if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <param name="value">The value associated with the key</param>
        /// <returns>Returns true if the data was properly retrieved. This method returns false if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.</returns>
        public bool TryGetValue(string key, out string[] value)
        {
            value = null;
            if (!m_Metadata.TryGetValue(key, out var objValue))
                return false;

            if (objValue.valueType != ValueType.StringArray)
                return false;

            value = (string[])objValue.value;

            return true;
        }
        /// <summary>
        /// Gets a value out of the metadata. If the value does not exist, or if a request is made for the improper
        /// data type, an exception will be thrown.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <returns>The metadata value</returns>
        /// <exception cref="ArgumentException">The key was not found in the metadata dictionary</exception>
        /// <exception cref="InvalidOperationException">The query was for the wrong data type</exception>
        public bool[] GetBoolArray(string key)
        {
            if (!m_Metadata.TryGetValue(key, out var value))
                throw new ArgumentException($"{key} does not exist in metadata");

            if (value.valueType != ValueType.BoolArray)
                throw new InvalidOperationException($"{key} is not associated to a bool array");

            return (bool[])value.value;
        }
        /// <summary>
        /// Tries to get a value from the metadata. This method will fail if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.
        /// </summary>
        /// <param name="key">The key of the metadata value</param>
        /// <param name="value">The value associated with the key</param>
        /// <returns>Returns true if the data was properly retrieved. This method returns false if the key was not found in the metadata, or
        /// if the metadata stored with the passed in key is associated with a different data type.</returns>
        public bool TryGetValue(string key, out bool[] value)
        {
            value = null;
            if (!m_Metadata.TryGetValue(key, out var objValue))
                return false;

            if (objValue.valueType != ValueType.BoolArray)
                return false;

            value = (bool[])objValue.value;

            return true;
        }

        public Metadata[] GetSubMetadataArray(string key)
        {
            if (!m_Metadata.TryGetValue(key, out var value))
                throw new ArgumentException($"{key} does not exist in metadata");

            if (value.valueType != ValueType.BoolArray)
                throw new InvalidOperationException($"{key} is not associated to a sub-metadata array");

            return (Metadata[])value.value;
        }

        public bool TryGetValue(string key, out Metadata[] value)
        {
            value = null;
            if (!m_Metadata.TryGetValue(key, out var objValue))
                return false;

            if (objValue.valueType != ValueType.SubMetadataArray)
                return false;

            value = (Metadata[])objValue.value;

            return true;
        }

        enum ValueType
        {
            Int,
            UInt,
            Float,
            String,
            Bool,
            SubMetadata,
            IntArray,
            FloatArray,
            StringArray,
            BoolArray,
            SubMetadataArray
        }

        struct MetadataEntry
        {
            public ValueType valueType;
            public object value;
        }
    }
}
