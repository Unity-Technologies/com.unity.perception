using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A categorical parameter for generating Material samples
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Experimental.Perception.Randomization.Parameters")]
    public class MaterialParameter : CategoricalParameter<Material> {}
}
