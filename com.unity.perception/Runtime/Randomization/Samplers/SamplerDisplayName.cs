using System;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Defines the label used to identify a sampler types in the sampler drop down menu
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class SamplerDisplayName : Attribute
    {
        /// <summary>
        /// Returns the SamplerDisplayName attribute annotating a particular sampler type
        /// </summary>
        /// <param name="type">The type of sampler</param>
        /// <returns>A SamplerDisplayName attribute</returns>
        public static SamplerDisplayName GetDisplayName(Type type) =>
            (SamplerDisplayName)GetCustomAttribute(type, typeof(SamplerDisplayName));

        /// <summary>
        /// The sampler label string
        /// </summary>
        public string displayName;

        /// <summary>
        /// Constructs a new SamplerDisplayName attribute
        /// </summary>
        /// <param name="displayName">The sampler label string</param>
        public SamplerDisplayName(string displayName)
        {
            this.displayName = displayName;
        }
    }
}
