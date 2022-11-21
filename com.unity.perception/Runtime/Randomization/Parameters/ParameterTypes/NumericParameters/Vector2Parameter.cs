using System;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A numeric parameter for generating Vector2 samples
    /// </summary>
    [Serializable]
    public class Vector2Parameter : NumericParameter<Vector2>
    {
        /// <summary>
        /// The sampler used for randomizing the x component of generated samples
        /// </summary>
        [SerializeReference] public ISampler x = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the y component of generated samples
        /// </summary>
        [SerializeReference] public ISampler y = new UniformSampler(0f, 1f);

        /// <summary>
        /// Returns an IEnumerable that iterates over each sampler field in this parameter
        /// </summary>
        public override IEnumerable<ISampler> samplers
        {
            get
            {
                yield return x;
                yield return y;
            }
        }

        /// <summary>
        /// Generates a Vector2 sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public override Vector2 Sample()
        {
            return new Vector2(x.Sample(), y.Sample());
        }
    }
}
