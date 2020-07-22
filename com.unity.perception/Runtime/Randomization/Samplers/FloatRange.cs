using System;
using UnityEngine.Perception.Randomization.Utilities;

namespace UnityEngine.Perception.Randomization.Samplers
{
    [Serializable]
    public class FloatRange
    {
        public float minimum;
        public float maximum = 1f;
        public float defaultValue = 0.5f;
    }
}
