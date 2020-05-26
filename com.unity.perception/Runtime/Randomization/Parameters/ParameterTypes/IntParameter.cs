using System;
using UnityEngine.Perception.Randomization.Parameters.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Parameters.ParameterTypes
{
    public class IntParameter : Parameter<int>
    {
        public override string ParameterTypeName => "Int";
    }
}
