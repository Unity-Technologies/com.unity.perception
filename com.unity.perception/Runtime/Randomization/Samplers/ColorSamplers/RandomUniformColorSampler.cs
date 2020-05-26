using UnityEngine.Perception.Randomization.Samplers.Abstractions;
using UnityEngine.Perception.Randomization.Utilities;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Samplers.Vector3Samplers
{
    public class RandomUniformColorSampler : RandomSampler<Color>
    {
        [Min(1)] public int sampleCount = 1;
        public override int SampleCount => sampleCount;
        public Color minSample;
        public Color maxSample;

        public override Color NextRandomSample(ref Unity.Mathematics.Random random)
        {
            var colorFloat = random.NextFloat4(
                minSample.ToFloat4(),
                maxSample.ToFloat4());
            return colorFloat.ToColor();
        }
    }
}
