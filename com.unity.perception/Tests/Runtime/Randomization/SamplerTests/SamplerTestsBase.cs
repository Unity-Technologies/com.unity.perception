using System;
using NUnit.Framework;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Perception.Randomization.Samplers;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;
using Object = UnityEngine.Object;

namespace RandomizationTests.SamplerTests
{
    public abstract class RangedSamplerTests<T> where T : ISampler
    {
        const int k_TestSampleCount = 30;
        protected T m_BaseSampler;
        T m_Sampler;
        GameObject m_ScenarioObj;

        static ScenarioBase activeScenario => ScenarioBase.activeScenario;

        [SetUp]
        public void Setup()
        {
            m_Sampler = m_BaseSampler;
            m_ScenarioObj = new GameObject("Scenario");
            m_ScenarioObj.AddComponent<FixedLengthScenario>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_ScenarioObj);
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
    }
}
