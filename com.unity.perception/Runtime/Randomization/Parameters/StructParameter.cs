using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public abstract class StructParameter<T> : TypedParameter<T> where T : struct
    {
        public abstract NativeArray<T> Samples(int iteration, int totalSamples, out JobHandle jobHandle);
    }
}
