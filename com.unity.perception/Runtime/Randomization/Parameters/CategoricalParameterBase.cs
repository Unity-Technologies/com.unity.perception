using System;
using System.Collections.Generic;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    public abstract class CategoricalParameterBase : Parameter
    {
        [SerializeField] internal List<float> probabilities = new List<float>();

        internal abstract void AddOption();
        public abstract void RemoveOption(int index);
        public abstract void ClearOptions();
    }
}
