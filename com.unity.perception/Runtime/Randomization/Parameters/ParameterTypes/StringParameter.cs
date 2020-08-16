using System;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A categorical parameter for generating string samples
    /// </summary>
    [Serializable]
    [ParameterDisplayName("String")]
    public class StringParameter : CategoricalParameter<string> {}
}
