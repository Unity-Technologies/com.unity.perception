using UnityEngine.Perception.Randomization.Parameters.Abstractions;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Parameters.ParameterTypes
{
    public class ColorParameter : Parameter<Color>
    {
        public override string ParameterTypeName => "Color";
    }
}
