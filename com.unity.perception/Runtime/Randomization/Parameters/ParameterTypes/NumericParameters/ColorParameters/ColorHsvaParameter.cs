using System;
using System.Collections.Generic;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A numeric parameter for generating color samples using HSVA samplers
    /// </summary>
    [Serializable]
    public class ColorHsvaParameter : NumericParameter<Color>
    {
        /// <summary>
        /// The sampler used for randomizing the hue component of generated samples
        /// </summary>
        [SerializeReference] public ISampler hue = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the saturation component of generated samples
        /// </summary>
        [SerializeReference] public ISampler saturation = new UniformSampler(0f, 1f);

        /// <summary>
        /// The sampler used for randomizing the value component of generated samples
        /// </summary>
        [SerializeReference] public ISampler value = new UniformSampler(0f, 1f);

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
                yield return hue;
                yield return saturation;
                yield return value;
                yield return alpha;
            }
        }

        /// <summary>
        /// Generates an RGBA color sample
        /// </summary>
        /// <returns>The generated RGBA sample</returns>
        public override Color Sample()
        {
            var color = Color.HSVToRGB(hue.Sample(), saturation.Sample(), value.Sample());
            color.a = alpha.Sample();
            return color;
        }

        /// <summary>
        /// Generates an HSVA color sample
        /// </summary>
        /// <returns>The generated HSVA sample</returns>
        public ColorHsva SampleHsva()
        {
            return new ColorHsva(hue.Sample(), saturation.Sample(), value.Sample(), alpha.Sample());
        }
    }
}
