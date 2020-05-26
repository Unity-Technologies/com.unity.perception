using System;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public abstract class TypedParameter<T> : Parameter
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
