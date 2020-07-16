using System;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public class CategoricalParameter<T> : Parameter
    {
        public bool uniform;
        public T[] options;
        public float[] probability;
        public override Sampler[] Samplers => new Sampler[0];
        public override Type OutputType => typeof(T);
    }
}
