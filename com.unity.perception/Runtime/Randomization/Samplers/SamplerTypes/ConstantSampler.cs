using Unity.Collections;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns a constant value when sampled
    /// </summary>
    [AddComponentMenu("")]
    [SamplerMetaData("Constant")]
    public class ConstantSampler : Sampler
    {
        public float value;

        public override float Sample(int iteration)
        {
            return value;
        }

        public override float[] Samples(int iteration, int totalSamples)
        {
            var floats = new float[totalSamples];
            for (var i = 0; i < totalSamples; i++)
                floats[i] = value;
            return floats;
        }

        public override NativeArray<float> Samples(int iteration, int totalSamples, Allocator allocator)
        {
            var floats = new NativeArray<float>(totalSamples, allocator, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < totalSamples; i++)
                floats[i] = value;
            return floats;
        }
    }
}
