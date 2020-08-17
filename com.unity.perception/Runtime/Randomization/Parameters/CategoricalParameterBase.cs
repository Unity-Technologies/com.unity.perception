using System;
using System.Collections.Generic;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// The base class of CategoricalParameters.
    /// </summary>
    [Serializable]
    public abstract class CategoricalParameterBase : Parameter
    {
        [SerializeField] internal List<float> probabilities = new List<float>();

        internal abstract void AddOption();
        internal abstract void RemoveOption(int index);
        internal abstract void ClearOptions();
    }
}
