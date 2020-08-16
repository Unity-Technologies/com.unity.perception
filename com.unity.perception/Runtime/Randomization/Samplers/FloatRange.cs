using System;

namespace UnityEngine.Perception.Randomization.Samplers
{
    [Serializable]
    public struct FloatRange
    {
        public float minimum;
        public float maximum;

        public FloatRange(float min, float max)
        {
            minimum = min;
            maximum = max;
        }

        public void Validate()
        {
            if (minimum > maximum)
                throw new ArgumentException("Invalid sampling range");
        }
    }
}
