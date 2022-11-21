using NUnit.Framework;
using UnityEngine.Perception.Randomization.Samplers;

namespace RandomizationTests.SamplerTests
{
    [TestFixture]
    public class AnimationCurveSamplerTestsBase : SamplerTestsBase<AnimationCurveSampler>
    {
        public AnimationCurveSamplerTestsBase()
        {
            m_BaseSampler = new AnimationCurveSampler();
        }

        [Test]
        public void SamplesInRange()
        {
            var min = m_Sampler.distributionCurve.keys[0].time;
            var max = m_Sampler.distributionCurve.keys[m_Sampler.distributionCurve.length - 1].time;

            var samples = new float[k_TestSampleCount];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = m_Sampler.Sample();
            }
            Assert.AreEqual(samples.Length, k_TestSampleCount);

            for (var i = 0; i < samples.Length; i++)
            {
                Assert.GreaterOrEqual(samples[i], min);
                Assert.LessOrEqual(samples[i], max);
            }
        }
    }
}
