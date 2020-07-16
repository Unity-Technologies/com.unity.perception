using System;

namespace UnityEngine.Perception.Randomization.Parameters.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ParameterMetaData : Attribute
    {
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
