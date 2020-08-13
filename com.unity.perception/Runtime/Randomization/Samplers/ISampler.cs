using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Generates random values from probability distributions
    /// </summary>
    public interface ISampler
    {
        uint seed { get; set; }
        FloatRange range { get; set; }

        /// <summary>
        /// Returns a duplicate sampler with an iterated seed
        /// </summary>
        /// <param name="index">
        /// Offset value is often a the active scenario's currentIteration or framesSinceInitialization.
        /// </param>
        ISampler CopyAndIterate(int index);

        /// <summary>
        /// Generate one sample
        /// </summary>
        float NextSample();

        /// <summary>
        /// Schedule a job to generate multiple samples
        /// </summary>
        NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle);
    }
}
