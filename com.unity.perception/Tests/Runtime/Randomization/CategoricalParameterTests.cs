using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.TestTools;

namespace RandomizationTests
{
    [TestFixture]
    public class CategoricalParameterTests
    {
        GameObject m_TestObject;

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        [UnityTest]
        public IEnumerator NegativeProbabilitiesTest()
        {
            var parameter = m_TestObject.AddComponent<StringParameter>();
            parameter.options = new List<string> { "option1", "option2" };
            parameter.probabilities = new List<float> { 1f, -1f };
            Assert.Throws<ParameterValidationException>(() => parameter.Validate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator DifferentOptionAndProbabilityCounts()
        {
            var parameter = m_TestObject.AddComponent<StringParameter>();
            parameter.options = new List<string> { "option1" };
            parameter.probabilities = new List<float> { 1f, 1f };
            Assert.Throws<ParameterValidationException>(() => parameter.Validate());
            yield return null;
        }

        [UnityTest]
        public IEnumerator ZeroSumProbability()
        {
            var parameter = m_TestObject.AddComponent<StringParameter>();
            parameter.options = new List<string> { "option1", "option2" };
            parameter.probabilities = new List<float> { 0f, 0f };
            Assert.Throws<ParameterValidationException>(() => parameter.Validate());
            yield return null;
        }
    }
}
