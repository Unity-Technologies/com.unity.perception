using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Parameters
{
    /// <summary>
    /// A numeric parameter for generating float samples
    /// </summary>
    [Serializable]
    public class FloatParameter : NumericParameter<float>
    {
        /// <summary>
        /// The sampler used to generate random float values
        /// </summary>
        [SerializeReference] public ISampler value = new UniformSampler(0f, 1f);

        /// <summary>
        /// Returns an IEnumerable that iterates over each sampler field in this parameter
        /// </summary>
        internal override IEnumerable<ISampler> samplers
        {
            get { yield return value; }
        }

        /// <summary>
        /// Generates a float sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public override float Sample()
        {
            return value.Sample();
        }

        /// <summary>
        /// Schedules a job to generate an array of samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of samples</returns>
        public override NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle)
        {
            return value.Samples(sampleCount, out jobHandle);
        }
    }
}
