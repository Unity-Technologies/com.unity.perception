using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;
using Assert = Unity.Assertions.Assert;

namespace RandomizationTests.RandomizerTests
{
    [TestFixture]
    public class RandomizerTagTests
    {
        GameObject m_TestObject;
        FixedLengthScenario m_Scenario;

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
            m_Scenario = m_TestObject.AddComponent<FixedLengthScenario>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        [Test]
        public void TagQueryFindsCorrectNumberOfGameObjects()
        {
            const int copyCount = 5;
            var gameObject = new GameObject();
            gameObject.AddComponent<ExampleTag>();
            for (var i = 0; i < copyCount - 1; i++)
                Object.Instantiate(gameObject);

            var queriedObjects = m_Scenario.tagManager.Query<ExampleTag>();
            Assert.AreEqual(queriedObjects.Length, copyCount);
        }
    }
}
