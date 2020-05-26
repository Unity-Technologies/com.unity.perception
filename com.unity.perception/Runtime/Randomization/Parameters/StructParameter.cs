using System;
using Unity.Collections;
using Unity.Jobs;

namespace UnityEngine.Perception.Randomization.Parameters
{
    public abstract class StructParameter<T> : TypedParameter<T> where T : struct
    {
        /// <summary>
        /// Schedules a job to generate an array of parameter samples.
        /// Call Complete() on the JobHandle returned by this function to wait on the job generating the parameter samples.
        /// </summary>
        /// <param name="index">Often the current scenario iteration or a scenario's framesSinceInitialization</param>
        /// <param name="sampleCount">Number of parameter samples to generate</param>
        /// <param name="jobHandle">The JobHandle returned from scheduling the sampling job</param>
        public abstract NativeArray<T> Samples(int index, int sampleCount, out JobHandle jobHandle);
    }
}
