using System;

namespace UnityEngine.Experimental.Perception.Randomization.Samplers
{
    public class SamplerValidationException : Exception
    {
        public SamplerValidationException(string msg) : base(msg) {}
        public SamplerValidationException(string msg, Exception innerException) : base(msg, innerException) {}
    }
}
