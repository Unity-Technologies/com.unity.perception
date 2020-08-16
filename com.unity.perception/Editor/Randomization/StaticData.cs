using System;
using System.Collections.Generic;
using System.Reflection;
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
            GatherParameterAndSamplerTypes();
            var samplerType = typeof(ISampler);
            samplerSerializedFieldType = $"{samplerType.Assembly.GetName().Name} {samplerType.FullName}";
        }

        static void GatherParameterAndSamplerTypes()
        {
            var paramAssembly = typeof(Parameter).Assembly;
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assemblies = new List<Assembly> { paramAssembly };
            foreach (var assembly in allAssemblies)
            {
                foreach (var asm in assembly.GetReferencedAssemblies())
                {
                    if (asm.FullName == paramAssembly.GetName().FullName)
                    {
                        assemblies.Add(assembly);
                        break;
                    }
                }
            }

            var parameterTypesList = new List<Type>();
            var samplerTypesList = new List<Type>();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    var isNotAbstract = (type.Attributes & TypeAttributes.Abstract) == 0;
                    if (typeof(Parameter).IsAssignableFrom(type) && isNotAbstract &&
                        ParameterDisplayName.GetDisplayName(type) != null)
                        parameterTypesList.Add(type);
                    else if (typeof(ISampler).IsAssignableFrom(type) &&
                        isNotAbstract && SamplerDisplayName.GetDisplayName(type) != null)
                    {
                        samplerTypesList.Add(type);
                    }
                }
            }

            parameterTypes = parameterTypesList.ToArray();
            samplerTypes = samplerTypesList.ToArray();
        }
    }
}
