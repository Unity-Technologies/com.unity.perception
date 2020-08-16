using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A categorical parameter for generating string samples
    /// </summary>
    [Serializable]
    [ParameterMetaData("String")]
    public class StringParameter : CategoricalParameter<string> {}
}
