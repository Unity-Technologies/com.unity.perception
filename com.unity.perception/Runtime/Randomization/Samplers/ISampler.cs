using System;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Generates random values from probability distributions
    /// </summary>
    public interface ISampler
    {
        /// <summary>
        /// Generates one sample
        /// </summary>
        /// <returns>The generated sample</returns>
        float Sample();

        /// <summary>
        /// Validates that the sampler is configured properly
        /// </summary>
        void Validate();

        /// <summary>
        /// Check that the provided values adhere to the <see cref="minAllowed"/> and <see cref="maxAllowed"/> outputs for this sampler.
        /// </summary>
        public void CheckAgainstValidRange();

        /// <summary>
        /// Whether the provided <see cref="minAllowed"/> and <see cref="maxAllowed"/> values should be used to validate this sampler.
        /// </summary>
        public bool shouldCheckValidRange { get; set; }

        /// <summary>
        /// The smallest value this sampler should output
        /// </summary>
        public float minAllowed { get; set; }

        /// <summary>
        /// The largest value this sampler should output
        /// </summary>
        public float maxAllowed { get; set; }
    }
}
