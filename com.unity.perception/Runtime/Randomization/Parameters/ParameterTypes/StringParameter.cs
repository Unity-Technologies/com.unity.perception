using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    [ParameterMetaData("String")]
    public class StringParameter : CategoricalParameter<string> {}
}
