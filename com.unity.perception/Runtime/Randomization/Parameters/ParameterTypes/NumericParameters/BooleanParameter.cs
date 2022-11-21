using System;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A numeric parameter for generating boolean samples
    /// </summary>
    [Serializable]
    public class BooleanParameter : NumericParameter<bool>
    {
        /// <summary>
        /// The sampler used as a source of random values for this parameter
        /// </summary>
        [HideInInspector, SerializeReference] public ISampler value = new UniformSampler(0f, 1f);

        /// <summary>
        /// A threshold value that transforms random values within the range [0, 1] to boolean values.
        /// Values greater than the threshold are true, and values less than the threshold are false.
        /// </summary>
        [Range(0, 1)] public float threshold = 0.5f;

        /// <summary>
        /// Returns an IEnumerable that iterates over each sampler field in this parameter
        /// </summary>
        public override IEnumerable<ISampler> samplers
        {
            get { yield return value; }
        }

        bool Sample(float t) => t >= threshold;

        /// <summary>
        /// Generates a boolean sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public override bool Sample()
        {
            return Sample(value.Sample());
        }
    }
}
