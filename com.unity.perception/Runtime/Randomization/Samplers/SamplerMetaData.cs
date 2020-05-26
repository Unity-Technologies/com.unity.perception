using System;

namespace UnityEngine.Perception.Randomization.Samplers
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class SamplerMetaData : Attribute
    {
        public static SamplerMetaData GetMetaData(Type type) =>
            (SamplerMetaData)GetCustomAttribute(type, typeof(SamplerMetaData));

        public string displayName;

        public SamplerMetaData(string displayName)
        {
            this.displayName = displayName;
        }
    }
}
