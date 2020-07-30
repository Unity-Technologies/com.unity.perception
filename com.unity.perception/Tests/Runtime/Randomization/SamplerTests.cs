using System.Collections;
using NUnit.Framework;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.TestTools;

namespace RandomizationTests
{
    [TestFixture]
    public class SamplerTests
    {
        const int k_ScenarioIteration = 0;
        const int k_TestSampleCount = 30;

        static void SamplesInRange(RangedSampler sampler)
        {
            var samples = sampler.Samples(k_ScenarioIteration, k_TestSampleCount);
            Assert.AreEqual(samples.Length, k_TestSampleCount);
            foreach (var sample in samples)
            {
                Assert.GreaterOrEqual(sample, sampler.range.minimum);
                Assert.LessOrEqual(sample, sampler.range.maximum);
            }
        }

        static void NativeSamplesInRange(RangedSampler sampler)
        {
            var samples = sampler.Samples(k_ScenarioIteration, k_TestSampleCount, out var handle);
            handle.Complete();
            Assert.AreEqual(samples.Length, k_TestSampleCount);
            foreach (var sample in samples)
            {
                Assert.GreaterOrEqual(sample, sampler.range.minimum);
                Assert.LessOrEqual(sample, sampler.range.maximum);
            }
            samples.Dispose();
        }

        static void TestSamples(RangedSampler sampler)
        {
            SamplesInRange(sampler);
            NativeSamplesInRange(sampler);
        }

        [UnityTest]
        public IEnumerator UniformSamplesInRangeTest()
        {
            TestSamples(new UniformSampler());
            yield return null;
        }

        [UnityTest]
        public IEnumerator NormalSamplesInRangeTest()
        {
            TestSamples(new NormalSampler());
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConstantSamplerTest()
        {
            var constantSampler = new ConstantSampler();
            var samples = constantSampler.Samples(k_ScenarioIteration, k_TestSampleCount);
            Assert.AreEqual(samples.Length, k_TestSampleCount);
            foreach (var sample in samples)
                Assert.AreEqual(sample, constantSampler.value);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlaceholderRangeThrowsExceptionsWhenSamplingTest()
        {
            var phSampler = new PlaceholderRangeSampler();
            Assert.Throws<SamplerException>(() => phSampler.Sample(k_ScenarioIteration));
            Assert.Throws<SamplerException>(() => phSampler.Samples(k_ScenarioIteration, k_TestSampleCount));
            Assert.Throws<SamplerException>(() => phSampler.Samples(k_ScenarioIteration, k_TestSampleCount, out var handle));
            yield return null;
        }
    }
}
