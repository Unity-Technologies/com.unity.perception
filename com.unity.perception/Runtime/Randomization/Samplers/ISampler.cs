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
        /// <summary>
        /// Resets a sampler's state to its base random seed
        /// </summary>
        void ResetState();

        /// <summary>
        /// Resets a sampler's state to its base random seed and then offsets said seed using an index value
        /// </summary>
        /// <param name="index">
        /// Often a the active scenario's currentIteration
        /// </param>
        void ResetState(int index);

        /// <summary>
        /// Deterministically offsets a sampler's state when generating values within a batched job
        /// </summary>
        /// <param name="batchIndex">
        /// The current job index
        /// </param>
        void IterateState(int batchIndex);

        /// <summary>
        /// Generate one sample
        /// </summary>
        float Sample();

        /// <summary>
        /// Schedule a job to generate multiple samples
        /// </summary>
        NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle);
    }
}
