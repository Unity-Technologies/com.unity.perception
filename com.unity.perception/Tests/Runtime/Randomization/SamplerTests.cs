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

        static void SamplesInRange(ISampler sampler)
        {
            var offsetSampler = sampler.CopyAndIterate(k_ScenarioIteration);
            var samples = SamplerUtility.GenerateSamples(offsetSampler, k_TestSampleCount);

            Assert.AreEqual(samples.Length, k_TestSampleCount);

            foreach (var sample in samples)
            {
                Assert.GreaterOrEqual(sample, sampler.range.minimum);
                Assert.LessOrEqual(sample, sampler.range.maximum);
            }
        }

        static void NativeSamplesInRange(ISampler sampler)
        {
            var samples = sampler.CopyAndIterate(k_ScenarioIteration).NativeSamples(k_TestSampleCount, out var handle);
            handle.Complete();
            Assert.AreEqual(samples.Length, k_TestSampleCount);
            foreach (var sample in samples)
            {
                Assert.GreaterOrEqual(sample, sampler.range.minimum);
                Assert.LessOrEqual(sample, sampler.range.maximum);
            }
            samples.Dispose();
        }

        static void TestSamples(ISampler sampler)
        {
            SamplesInRange(sampler);
            NativeSamplesInRange(sampler);
        }

        [UnityTest]
        public IEnumerator UniformSamplesInRangeTest()
        {
            TestSamples(new UniformSampler(0, 1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator NormalSamplesInRangeTest()
        {
            TestSamples(new NormalSampler(-1, 1, 0, 1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConstantSamplerTest()
        {
            var constantSampler = new ConstantSampler();
            var samples = SamplerUtility.GenerateSamples(constantSampler, k_TestSampleCount);
            Assert.AreEqual(samples.Length, k_TestSampleCount);
            foreach (var sample in samples)
                Assert.AreEqual(sample, constantSampler.value);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlaceholderRangeThrowsExceptionsWhenSamplingTest()
        {
            var phSampler = new PlaceholderRangeSampler();
            Assert.Throws<SamplerException>(() => phSampler.NextSample());
            yield return null;
        }

        [UnityTest]
        public IEnumerator CatchInvalidSamplerRangeTest()
        {
            Assert.Throws<SamplerException>(() => SamplerUtility.ValidateRange(new FloatRange(1, -1)));
            yield return null;
        }
    }
}
