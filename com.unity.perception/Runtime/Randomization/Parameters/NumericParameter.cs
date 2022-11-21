using System;

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
        /// Generates a generic sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public override object GenericSample()
        {
            return Sample();
        }

        /// <summary>
        /// Validate the settings of this parameter
        /// </summary>
        public override void Validate()
        {
            base.Validate();
            foreach (var sampler in samplers)
                sampler.Validate();
        }
    }
}
