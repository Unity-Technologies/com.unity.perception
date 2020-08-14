using NUnit.Framework;
using UnityEngine.Perception.Randomization.Samplers;

namespace RandomizationTests.SamplerTests
{
    [TestFixture]
    public class ConstantSamplerTests
    {
        [Test]
        public void ConstantSamplerGeneratesConstantValues()
        {
            var constantSampler = new ConstantSampler();
            var samples = SamplerUtility.GenerateSamples(constantSampler, TestValues.TestSampleCount);
            Assert.AreEqual(samples.Length, TestValues.TestSampleCount);
            foreach (var sample in samples)
                Assert.AreEqual(sample, constantSampler.value);
        }
    }
}
