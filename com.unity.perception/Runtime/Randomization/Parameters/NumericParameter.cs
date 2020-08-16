using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// Numeric parameters use samplers to generate randomized structs
    /// </summary>
    /// <typeparam name="T">The sample type of the parameter</typeparam>
    [Serializable]
    public abstract class NumericParameter<T> : Parameter where T : struct
    {
        /// <summary>
        /// The sample type of parameter
        /// </summary>
        public sealed override Type sampleType => typeof(T);

        /// <summary>
        /// Generates one parameter sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public abstract T Sample();

        /// <summary>
        /// Schedules a job to generate an array of parameter samples.
        /// Call Complete() on the JobHandle returned by this function to wait on the job generating the parameter samples.
        /// </summary>
        /// <param name="sampleCount">Number of parameter samples to generate</param>
        /// <param name="jobHandle">The JobHandle returned from scheduling the sampling job</param>
        /// <returns>A NativeArray containing generated samples</returns>
        public abstract NativeArray<T> Samples(int sampleCount, out JobHandle jobHandle);

        internal sealed override void ApplyToTarget(int seedOffset)
        {
            if (!hasTarget)
                return;
            target.ApplyValueToTarget(Sample());
        }

        internal override void Validate()
        {
            base.Validate();
            foreach (var sampler in samplers)
                if (sampler is IRandomRangedSampler rangedSampler)
                    SamplerUtility.ValidateRange(rangedSampler);
        }
    }
}
