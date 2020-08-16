using System;

namespace UnityEngine.Perception.Randomization.Parameters
{
    class ParameterValidationException : Exception
    {
        public ParameterValidationException(string msg) : base(msg) {}
    }
}
