using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Editor
{
    static class StaticData
    {
        const string k_RandomizationDir = "Packages/com.unity.perception/Editor/Randomization";
        internal const string uxmlDir = k_RandomizationDir + "/Uxml";

        internal static readonly string samplerSerializedFieldType;

        internal static Type[] randomizerTypes;
        internal static Type[] samplerTypes;

        static StaticData()
        {
            randomizerTypes = GetConstructableDerivedTypes<Randomizer>();
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

        internal static object GetManagedReferenceValue(SerializedProperty prop, bool parent=false)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            if (parent)
                elements = elements.Take(elements.Count() - 1).ToArray();

            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[","").Replace("]",""));
                    obj = GetArrayValue(obj, elementName, index);
                }
                else
                    obj = GetValue(obj, element);
            }
            return obj;
        }

        static object GetValue(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f == null)
            {
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                return p == null ? null : p.GetValue(source, null);
            }
            return f.GetValue(source);
        }

        static object GetArrayValue(object source, string name, int index)
        {
            var value = GetValue(source, name);
            if (!(value is IEnumerable enumerable))
                return null;
            var enumerator = enumerable.GetEnumerator();
            while (index-- >= 0)
                enumerator.MoveNext();
            return enumerator.Current;
        }

        public static bool IsSubclassOfRawGeneric(Type generic, Type toCheck) {
            while (toCheck != null && toCheck != typeof(object)) {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}
