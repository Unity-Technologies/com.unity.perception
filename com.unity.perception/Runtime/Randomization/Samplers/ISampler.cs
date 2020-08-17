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
        /// <param name="index">Often a the active scenario's currentIteration</param>
        void ResetState(int index);

        /// <summary>
        /// Set the base seed value of this sampler
        /// </summary>
        /// <param name="seed">The seed that will replace the sampler's current seed</param>
        void Rebase(uint seed);

        /// <summary>
        /// Deterministically offsets a sampler's state when generating values within a batched job
        /// </summary>
        /// <param name="offsetIndex">
        /// The index used to offset the sampler's state.
        /// Typically set to either the current scenario iteration or a job's batch index.
        /// </param>
        void IterateState(int offsetIndex);

        /// <summary>
        /// Generates one sample
        /// </summary>
        /// <returns>The generated sample</returns>
        float Sample();

        /// <summary>
        /// Schedules a job to generate an array of samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of generated samples</returns>
        NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle);
    }
}
