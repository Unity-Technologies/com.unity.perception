using System;
using UnityEngine.Perception.Randomization.Parameters.Attributes;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A categorical parameter for generating GameObject samples
    /// </summary>
    [Serializable]
    [ParameterMetaData("GameObject")]
    public class GameObjectParameter : CategoricalParameter<GameObject> { }
}
