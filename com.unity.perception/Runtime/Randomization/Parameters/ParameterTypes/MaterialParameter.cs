using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A categorical parameter for generating Material samples
    /// </summary>
    [Serializable]
    [ParameterMetaData("Material")]
    public class MaterialParameter : CategoricalParameter<Material> {}
}
