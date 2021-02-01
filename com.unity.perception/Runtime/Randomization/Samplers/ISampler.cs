using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Generates random values from probability distributions
    /// </summary>
    [MovedFrom("UnityEngine.Experimental.Perception.Randomization.Samplers")]
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
    }
}
