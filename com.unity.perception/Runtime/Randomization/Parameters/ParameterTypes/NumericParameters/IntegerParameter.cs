using System;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A numeric parameter for generating integer samples
    /// </summary>
    [Serializable]
    public class IntegerParameter : NumericParameter<int>
    {
        /// <summary>
        /// The sampler used as a source of random values for this parameter
        /// </summary>
        [SerializeReference] public ISampler value = new UniformSampler(0f, 1f);

        /// <summary>
        /// Returns an IEnumerable that iterates over each sampler field in this parameter
        /// </summary>
        public override IEnumerable<ISampler> samplers
        {
            get { yield return value; }
        }

        /// <summary>
        /// Generates an integer sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public override int Sample() => (int)value.Sample();
    }
}
