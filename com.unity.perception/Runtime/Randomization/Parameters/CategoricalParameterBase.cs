using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// The base class of CategoricalParameters.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Experimental.Perception.Randomization.Parameters")]
    public abstract class CategoricalParameterBase : Parameter
    {
        [SerializeField] internal List<float> probabilities = new List<float>();
    }
}
