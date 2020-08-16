using System;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// Defines the label used to identify a parameter types in a parameter configuration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ParameterDisplayName : Attribute
    {
        /// <summary>
        /// Returns the ParameterDisplayName attribute annotating a particular parameter type
        /// </summary>
        /// <param name="type">The type of parameter</param>
        /// <returns>A ParameterDisplayName attribute</returns>
        public static ParameterDisplayName GetDisplayName(Type type) =>
            (ParameterDisplayName)GetCustomAttribute(type, typeof(ParameterDisplayName));

        /// <summary>
        /// The parameter label string
        /// </summary>
        public string displayName;

        /// <summary>
        /// Constructs a new ParameterDisplayName attribute
        /// </summary>
        /// <param name="displayName">The parameter label string</param>
        public ParameterDisplayName(string displayName)
        {
            this.displayName = displayName;
        }
    }
}
