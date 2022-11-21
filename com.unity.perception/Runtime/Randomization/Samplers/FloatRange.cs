using System;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// A struct representing a continuous range of values
    /// </summary>
    [Serializable]
    public struct FloatRange
    {
        /// <summary>
        /// The smallest value contained within the range
        /// </summary>
        public float minimum;

        /// <summary>
        /// The largest value contained within the range
        /// </summary>
        public float maximum;

        /// <summary>
        /// Constructs a float range
        /// </summary>
        /// <param name="min">The smallest value contained within the range</param>
        /// <param name="max">The largest value contained within the range</param>
        public FloatRange(float min, float max)
        {
            minimum = min;
            maximum = max;
        }

        /// <summary>
        /// Assert whether this range is valid
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void Validate()
        {
            Assert.IsTrue(minimum <= maximum);
        }
    }
}
