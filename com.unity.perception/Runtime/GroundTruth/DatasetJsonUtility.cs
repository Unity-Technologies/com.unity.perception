using System;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Utilities for producing Json for datasets.
    /// </summary>
    static class DatasetJsonUtility
    {
        //static JsonConverter[] s_Converters = { new Vector3Converter(), new QuaternionConverter(), new Float3X3Converter(), new Float3Converter() };

        public static JToken ToJToken(Vector3 value)
        {
            var obj = new JArray();
            obj.Add(value.x);
            obj.Add(value.y);
            obj.Add(value.z);
            return obj;
        }

        public static JToken ToJToken(Quaternion value)
        {
            var obj = new JArray();
            obj.Add(value.x);
            obj.Add(value.y);
            obj.Add(value.z);
            obj.Add(value.w);
            return obj;
        }

        public static JToken ToJToken(float3x3 value)
        {
            var obj = new JArray();
            obj.Add(ToJToken(value.c0));
            obj.Add(ToJToken(value.c1));
            obj.Add(ToJToken(value.c2));
            return obj;
        }

        public static JToken ToJToken(float3 value)
        {
            var obj = new JArray();
            obj.Add(value.x);
            obj.Add(value.y);
            obj.Add(value.z);
            return obj;
        }

        public static JToken ToJToken<T>(T value)
        {
            switch (value)
            {
                case float3 v:
                    return ToJToken(v);
                case float3x3 v:
                    return ToJToken(v);
                case Quaternion v:
                    return ToJToken(v);
                case Vector3 v:
                    return ToJToken(v);
                case float v:
                    return new JValue(v);
                case int v:
                    return new JValue(v);
                case double v:
                    return new JValue(v);
                case string v:
                    return new JValue($"\"{v}\"");
                case uint v:
                    return new JValue(v);
            }
            //Unfortunate solution of creating Json, and immediately parsing to a structure to work around the lack of
            //Reflection.Emit in players. We could just use this json directly, but then line endings and indentation are inconsistent
            var rawJson = JsonUtility.ToJson(value, true);
            return JObject.Parse(rawJson);
        }
    }
}
