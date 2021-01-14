using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace UnityEngine.Experimental.Perception.Randomization.Samplers
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
    }
}
