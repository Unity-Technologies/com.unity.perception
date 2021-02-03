using System;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns a constant value when sampled
    /// </summary>
    [Serializable]
    public class ConstantSampler : ISampler
    {
        /// <summary>
        /// The value from which samples will be generated
        /// </summary>
        public float value;

        /// <summary>
        /// Constructs a ConstantSampler
        /// </summary>
        public ConstantSampler()
        {
            value = 0f;
        }

        /// <summary>
        /// Constructs a new ConstantSampler
        /// </summary>
        /// <param name="value">The value from which samples will be generated</param>
        public ConstantSampler(float value)
        {
            this.value = value;
        }

        /// <summary>
        /// Generates one sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public float Sample()
        {
            return value;
        }

        /// <summary>
        /// Validates that the sampler is configured properly
        /// </summary>
        public void Validate() {}
    }
}
