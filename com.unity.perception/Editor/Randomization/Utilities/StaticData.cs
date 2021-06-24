using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEditor.Perception.Randomization
{
    static class StaticData
    {
        const string k_RandomizationDir = "Packages/com.unity.perception/Editor/Randomization";
        internal const string uxmlDir = k_RandomizationDir + "/Uxml";

        internal static Type[] randomizerTypes;
        internal static Type[] samplerTypes;

        static StaticData()
        {
            randomizerTypes = GetConstructableDerivedTypes<Randomizer>();
            samplerTypes = GetConstructableDerivedTypes<ISampler>();
        }

        static Type[] GetConstructableDerivedTypes<T>()
        {
            var collection = TypeCache.GetTypesDerivedFrom<T>();
            var types = new List<Type>();
            foreach (var type in collection)
                if (!type.IsAbstract && !type.IsInterface)
                    types.Add(type);
            return types.ToArray();
        }

        public static object GetManagedReferenceValue(SerializedProperty prop, bool parent = false)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            if (parent)
                elements = elements.Take(elements.Length - 1).ToArray();

            foreach (var element in elements)
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetArrayValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }

            return obj;
        }

        static object GetValue(object source, string name)
        {
            if (source == null)
                return null;
            var type = source.GetType();
            var field = GetField(type, name);
            if (field == null)
            {
                var property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                return property == null ? null : property.GetValue(source, null);
            }
            return field.GetValue(source);
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

        public static FieldInfo GetField(Type type, string fieldName)
        {
            if (type == null)
                return null;
            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |BindingFlags.DeclaredOnly;
            var fields = type.GetFields(flags);
            foreach (var field in fields)
                if (field.Name == fieldName)
                    return field;
            return GetField(type.BaseType, fieldName);
        }

        public static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) return true;
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}
