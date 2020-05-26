using System;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.Abstractions
{
    public abstract class Sampler<T> : SamplerBase
    {
        public abstract T NextSample();

        public override string GetSampleString()
        {
            return $"{NextSample()}";
        }
    }
}
