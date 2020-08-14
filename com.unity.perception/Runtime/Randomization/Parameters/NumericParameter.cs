using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    [Serializable]
    public abstract class NumericParameter<T> : Parameter where T : struct
    {
        public sealed override Type OutputType => typeof(T);

        /// <summary>
        /// Generates one parameter sample
        /// </summary>
        /// <param name="index">Often the current scenario iteration or a scenario's framesSinceInitialization</param>
        public abstract T Sample(int index);

        /// <summary>
        /// Generates an array of parameter samples
        /// </summary>
        /// <param name="index">Often the current scenario iteration or a scenario's framesSinceInitialization</param>
        /// <param name="sampleCount">Number of parameter samples to generate</param>
        public abstract T[] Samples(int index, int sampleCount);

        /// <summary>
        /// Schedules a job to generate an array of parameter samples.
        /// Call Complete() on the JobHandle returned by this function to wait on the job generating the parameter samples.
        /// </summary>
        /// <param name="index">Often the current scenario iteration or a scenario's framesSinceInitialization</param>
        /// <param name="sampleCount">Number of parameter samples to generate</param>
        /// <param name="jobHandle">The JobHandle returned from scheduling the sampling job</param>
        public abstract NativeArray<T> Samples(int index, int sampleCount, out JobHandle jobHandle);

        public sealed override void ApplyToTarget(int seedOffset)
        {
            if (!hasTarget)
                return;
            target.ApplyValueToTarget(Sample(seedOffset));
        }

        public override void Validate()
        {
            base.Validate();
            foreach (var sampler in Samplers)
                SamplerUtility.ValidateRange(sampler.range);
        }
    }
}
