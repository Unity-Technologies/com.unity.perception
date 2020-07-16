using System;

namespace UnityEngine.Perception.Randomization.Samplers
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SamplerMetaData : Attribute
    {
        public string displayName;

        public SamplerMetaData(string displayName)
        {
            this.displayName = displayName;
        }
    }
}
