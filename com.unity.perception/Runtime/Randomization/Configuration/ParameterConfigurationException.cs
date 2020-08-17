using System;

namespace UnityEngine.Perception.Randomization.Configuration
{
    [Serializable]
    class ParameterConfigurationException : Exception
    {
        public ParameterConfigurationException(string message) : base(message) { }
        public ParameterConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
