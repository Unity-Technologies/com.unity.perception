using System;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A categorical parameter for generating Material samples
    /// </summary>
    [Serializable]
    [ParameterDisplayName("Material")]
    public class MaterialParameter : CategoricalParameter<Material> {}
}
