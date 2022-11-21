using NUnit.Framework;
using UnityEngine.Perception.Randomization.Samplers;

namespace RandomizationTests.SamplerTests
{
    [TestFixture]
    public class NormalSamplerTestsBase : SamplerTestsBase<NormalSampler>
    {
        public NormalSamplerTestsBase()
        {
            m_BaseSampler = new NormalSampler(-10f, 10f, 0f, 1f);
        }

        [Test]
        public void SamplesInRange()
        {
            var samples = new float[k_TestSampleCount];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = m_Sampler.Sample();
            }
            Assert.AreEqual(samples.Length, k_TestSampleCount);

            for (var i = 0; i < samples.Length; i++)
            {
                Assert.GreaterOrEqual(samples[i], m_Sampler.range.minimum);
                Assert.LessOrEqual(samples[i], m_Sampler.range.maximum);
            }
        }
    }
}
