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
            var parameter = new CategoricalParameter<string>();
            var optionsArray = new[] { ("option1", 1f), ("option2", -1f) };
            Assert.Throws<ParameterValidationException>(() => parameter.SetOptions(optionsArray));
        }

        [Test]
        public void ZeroSumProbability()
        {
            var parameter = new CategoricalParameter<string>();
            var optionsArray = new[] { ("option1", 0f), ("option2", 0f) };
            Assert.Throws<ParameterValidationException>(() => parameter.SetOptions(optionsArray));
        }

        [Test]
        public void DuplicateCategoriesTest()
        {
            var parameter = new CategoricalParameter<string>();
            var optionsArray = new[] { ("option1", 0f), ("option1", 0f) };
            Assert.Throws<ParameterValidationException>(() => parameter.SetOptions(optionsArray));
        }

        [Test]
        public void BinarySearchTest()
        {
            var array = new float[] {0f, 0.1f, 0.3f, 0.6f, 0.8f, 1.0f};
            Assert.AreEqual(0, CategoricalParameter<string>.BinarySearch(array, 0f));
            Assert.AreEqual(2, CategoricalParameter<string>.BinarySearch(array, 0.3f));
            Assert.AreEqual(5, CategoricalParameter<string>.BinarySearch(array, 1.0f));
            //search for value that does not exists, return next closest index
            Assert.AreEqual(2, CategoricalParameter<string>.BinarySearch(array, 0.2f));
            //search for value that does not exists and greater than the biggest value in the array, return last index
            Assert.AreEqual(5, CategoricalParameter<string>.BinarySearch(array, 2f));
        }
    }
}
