using System;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public class ParameterValidationException : Exception
    {
        public ParameterValidationException(string msg) : base(msg) {}
    }
}
