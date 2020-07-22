using System;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public abstract class TypedParameter<T> : Parameter
    {
        public override Type OutputType => typeof(T);

        protected override object UntypedSample(int iteration)
        {
            return Sample(iteration);
        }

        public abstract T Sample(int iteration);
        public abstract T[] Samples(int iteration, int sampleCount);
    }
}
