using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    [ParameterMetaData("Material")]
    public class MaterialParameter : CategoricalParameter<Material> {}
}
