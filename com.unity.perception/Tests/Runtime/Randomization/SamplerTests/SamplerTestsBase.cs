using System;
using NUnit.Framework;
using Unity.Jobs;
using UnityEngine.Experimental.Perception.Randomization.Samplers;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;

namespace RandomizationTests.SamplerTests
{
    public abstract class RangedSamplerTests<T> where T : ISampler
    {
        const int k_TestSampleCount = 30;
        protected T m_BaseSampler;
        T m_Sampler;

        static ScenarioBase activeScenario => ScenarioBase.activeScenario;

        [SetUp]
        public void Setup()
        {
            m_Sampler = m_BaseSampler;
        }

        [Test]
        public void SamplesInRange()
        {
            var samples = m_Sampler.Samples(k_TestSampleCount, out var handle);
            handle.Complete();
            Assert.AreEqual(samples.Length, k_TestSampleCount);
            foreach (var sample in samples)
            {
                Assert.GreaterOrEqual(sample, m_Sampler.range.minimum);
                Assert.LessOrEqual(sample, m_Sampler.range.maximum);
            }
            samples.Dispose();
        }

        [Test]
        public void NativeSamplesInRange()
        {
            var samples = m_Sampler.Samples(k_TestSampleCount, out var handle);
            handle.Complete();
            Assert.AreEqual(samples.Length, k_TestSampleCount);
            foreach (var sample in samples)
            {
                Assert.GreaterOrEqual(sample, m_Sampler.range.minimum);
                Assert.LessOrEqual(sample, m_Sampler.range.maximum);
            }
            samples.Dispose();
        }

        [Test]
        public void ConsecutiveSamplesChangesState()
        {
            var state0 = activeScenario.randomState;
            m_Sampler.Sample();
            var state1 = activeScenario.randomState;
            m_Sampler.Sample();
            var state2 = activeScenario.randomState;;

            Assert.AreNotEqual(state0, state1);
            Assert.AreNotEqual(state1, state2);
        }

        [Test]
        public void ConsecutiveSampleBatchesChangesState()
        {
            var state0 = activeScenario.randomState;
            var samples1 = m_Sampler.Samples(k_TestSampleCount, out var handle1);
            var state1 = activeScenario.randomState;
            var samples2 = m_Sampler.Samples(k_TestSampleCount, out var handle2);
            var state2 = activeScenario.randomState;

            JobHandle.CombineDependencies(handle1, handle2).Complete();

            Assert.AreEqual(samples1.Length, samples2.Length);
            Assert.AreNotEqual(state0, state1);
            Assert.AreNotEqual(state1, state2);
            Assert.AreNotEqual(samples1[0], samples2[0]);

            samples1.Dispose();
            samples2.Dispose();
        }
    }
}
