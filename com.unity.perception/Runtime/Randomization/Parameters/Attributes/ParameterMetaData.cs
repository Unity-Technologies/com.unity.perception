using System;

namespace UnityEngine.Perception.Randomization.Parameters.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ParameterMetaData : Attribute
    {
        public static ParameterMetaData GetMetaData(Type type) =>
            (ParameterMetaData)GetCustomAttribute(type, typeof(ParameterMetaData));

        public string typeDisplayName;

        public ParameterMetaData(string typeDisplayName)
        {
            this.typeDisplayName = typeDisplayName;
        }
    }

    public enum ParameterSpace
    {
        Categorical,
        Numeric
    }
}
