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

        [Test]
        public void NegativeProbabilitiesTest()
        {
            var parameter = new StringParameter();
            parameter.AddOption("option1", 1f);
            parameter.AddOption("option2", -1f);
            Assert.Throws<ParameterValidationException>(() => parameter.Validate());
        }

        [Test]
        public void ZeroSumProbability()
        {
            var parameter = new StringParameter();
            parameter.AddOption("option1", 0f);
            parameter.AddOption("option2", 0f);
            Assert.Throws<ParameterValidationException>(() => parameter.Validate());
        }
    }
}
