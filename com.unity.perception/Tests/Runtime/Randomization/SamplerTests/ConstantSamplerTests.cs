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
            var constantSampler = new ConstantSampler(0f);
            var sample1 = constantSampler.Sample();
            var sample2 = constantSampler.Sample();
            Assert.AreEqual(sample1, sample2);
        }
    }
}
