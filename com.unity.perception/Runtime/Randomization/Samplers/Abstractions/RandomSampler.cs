using UnityEngine.Perception.Randomization.Samplers.Enums;
using UnityEngine.Perception.Randomization.Utilities;
using Unity.Collections;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.Abstractions
{
    public abstract class RandomSampler<T> : Sampler<T> where T : struct
    {
        public IterationOrigin iterationOrigin;

        public uint baseRandomSeed = RandomUtility.defaultBaseSeed;

        protected uint GetRandomSeed()
        {
            var data = parameter.iterationData;
            var iteration = iterationOrigin == IterationOrigin.Local
                ? data.localSampleIndex
                : data.globalSampleIndex;
            return RandomUtility.SeedFromIndex((uint)iteration, baseRandomSeed);
        }

        public override T NextSample()
        {
            var random = new Unity.Mathematics.Random(GetRandomSeed());
            return NextRandomSample(ref random);
        }

        public abstract T NextRandomSample(ref Unity.Mathematics.Random random);

        public NativeArray<T> RandomSamples(int sampleCount, Allocator allocator)
        {
            var random = new Unity.Mathematics.Random(GetRandomSeed());
            var samples = new NativeArray<T>(sampleCount, allocator, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < sampleCount; i++)
            {
                samples[i] = NextRandomSample(ref random);
            }
            return samples;
        }
    }
}
