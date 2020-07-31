using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Derived classes of the Sampler base class generate random values from probability distributions.
    /// </summary>
    [Serializable]
    public abstract class Sampler
    {
        /// <summary>
        /// Returns meta information regarding this type of sampler
        /// </summary>
        public SamplerMetaData MetaData =>
            (SamplerMetaData)Attribute.GetCustomAttribute(GetType(), typeof(SamplerMetaData));

        /// <summary>
        /// Generate one sample for the given scenario iteration
        /// </summary>
        public abstract float Sample(int iteration);

        /// <summary>
        /// Generate multiple samples for the given scenario iteration
        /// </summary>
        public abstract float[] Samples(int iteration, int totalSamples);

        /// <summary>
        /// Generate multiple samples in a native array for the given scenario iteration
        /// </summary>
        public abstract NativeArray<float> Samples(int iteration, int totalSamples, out JobHandle jobHandle);

        public virtual void Validate() { }
    }
}
