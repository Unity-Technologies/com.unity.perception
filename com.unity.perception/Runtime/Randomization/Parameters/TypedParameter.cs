using System;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public abstract class TypedParameter<T> : Parameter
    {
        public override Type OutputType => typeof(T);

        public abstract T Sample(int iteration);

        public abstract T[] Samples(int iteration, int sampleCount);
    }
}
