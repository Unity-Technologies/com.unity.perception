using System;
using Unity.Collections;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers
{
    [Serializable]
    public abstract class Sampler : MonoBehaviour
    {
        public abstract uint GetRandomSeed(int iteration);

        public abstract float Sample(int iteration);

        public abstract float Sample(ref Unity.Mathematics.Random rng);

        public NativeArray<float> NativeSamples(
            int totalSamples,
            int iteration,
            Allocator allocator)
        {
            var random = new Unity.Mathematics.Random(GetRandomSeed(iteration));
            var samples = new NativeArray<float>(totalSamples, allocator, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < totalSamples; i++)
                samples[i] = Sample(ref random);
            return samples;
        }

        public float[] Samples(
            int totalSamples,
            int iteration)
        {
            var random = new Unity.Mathematics.Random(GetRandomSeed(iteration));
            var samples = new float[totalSamples];
            for (var i = 0; i < totalSamples; i++)
                samples[i] = Sample(ref random);
            return samples;
        }
    }
}
