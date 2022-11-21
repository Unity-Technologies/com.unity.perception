using System;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A numeric parameter for generating RGBA color samples
    /// </summary>
    [Serializable]
    public class ColorRgbParameter : NumericParameter<Color>
    {
        /// <summary>
        /// The sampler used for randomizing the red component of generated samples
        /// </summary>
        [SerializeReference] public ISampler red = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the green component of generated samples
        /// </summary>
        [SerializeReference] public ISampler green = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the blue component of generated samples
        /// </summary>
        [SerializeReference] public ISampler blue = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the alpha component of generated samples
        /// </summary>
        [SerializeReference] public ISampler alpha = new ConstantSampler(1f);

        /// <summary>
        /// Returns an IEnumerable that iterates over each sampler field in this parameter
        /// </summary>
        public override IEnumerable<ISampler> samplers
        {
            get
            {
                yield return red;
                yield return green;
                yield return blue;
                yield return alpha;
            }
        }

        /// <summary>
        /// Generates an RGBA color sample
        /// </summary>
        /// <returns>The generated RGBA sample</returns>
        public override Color Sample()
        {
            return new Color(red.Sample(), green.Sample(), blue.Sample(), alpha.Sample());
        }
    }
}
