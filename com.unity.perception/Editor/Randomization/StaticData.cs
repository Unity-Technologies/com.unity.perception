using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Editor
{
    static class StaticData
    {
        const string k_RandomizationDir = "Packages/com.unity.perception/Editor/Randomization";
        internal const string uxmlDir = k_RandomizationDir + "/Uxml";

        internal static readonly string samplerSerializedFieldType;

        internal static Type[] parameterTypes;
        internal static Type[] samplerTypes;

        static StaticData()
        {
            parameterTypes = GetConstructableDerivedTypes<Parameter>();
            samplerTypes = GetConstructableDerivedTypes<ISampler>();
            var samplerType = typeof(ISampler);
            samplerSerializedFieldType = $"{samplerType.Assembly.GetName().Name} {samplerType.FullName}";
        }

        static Type[] GetConstructableDerivedTypes<T>()
        {
            var collection = TypeCache.GetTypesDerivedFrom<T>();
            var types = new List<Type>();
            foreach (var type in collection)
            {
                if (!type.IsAbstract && !type.IsInterface)
                    types.Add(type);
            }
            return types.ToArray();
        }
    }
}
