using System;
using UnityEngine.Perception.Randomization.Parameters.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Parameters.ParameterTypes
{
    public class FloatParameter : Parameter<float>
    {
        public override string ParameterTypeName => "Float";
    }
}
