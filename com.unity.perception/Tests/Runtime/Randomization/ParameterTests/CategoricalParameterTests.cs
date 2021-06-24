using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;

namespace RandomizationTests.ParameterTests
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
        public void NegativeProbabilities()
        {
            var parameter = new StringParameter();
            var optionsArray = new [] { ("option1", 1f), ("option2", -1f) };
            Assert.Throws<ParameterValidationException>(() => parameter.SetOptions(optionsArray));
        }

        [Test]
        public void ZeroSumProbability()
        {
            var parameter = new StringParameter();
            var optionsArray = new [] { ("option1", 0f), ("option2", 0f) };
            Assert.Throws<ParameterValidationException>(() => parameter.SetOptions(optionsArray));
        }

        [Test]
        public void DuplicateCategoriesTest()
        {
            var parameter = new StringParameter();
            var optionsArray = new [] { ("option1", 0f), ("option1", 0f) };
            Assert.Throws<ParameterValidationException>(() => parameter.SetOptions(optionsArray));
        }
    }
}
