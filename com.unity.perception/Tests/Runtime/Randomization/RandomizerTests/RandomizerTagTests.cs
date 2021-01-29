using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Perception.Randomization.Randomizers;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;
using Assert = Unity.Assertions.Assert;

namespace RandomizationTests.RandomizerTests
{
    [TestFixture]
    public class RandomizerTagTests
    {
        public class ParentTag : RandomizerTag { }

        public class ChildTag : ParentTag { }

        GameObject m_TestObject;
        FixedLengthScenario m_Scenario;

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
            m_Scenario = m_TestObject.AddComponent<FixedLengthScenario>();
            m_Scenario.quitOnComplete = false;
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
            gameObject.AddComponent<ParentTag>();
            for (var i = 0; i < copyCount - 1; i++)
                Object.Instantiate(gameObject);

            var gameObject2 = new GameObject();
            gameObject2.AddComponent<ChildTag>();
            for (var i = 0; i < copyCount - 1; i++)
                Object.Instantiate(gameObject2);

            var tagManager = RandomizerTagManager.singleton;
            var queriedObjects = tagManager.Query<ParentTag>().ToArray();
            Assert.AreEqual(queriedObjects.Length, copyCount);

            queriedObjects = tagManager.Query<ChildTag>().ToArray();
            Assert.AreEqual(queriedObjects.Length, copyCount);

            queriedObjects = tagManager.Query<ParentTag>(true).ToArray();
            Assert.AreEqual(queriedObjects.Length, copyCount * 2);
        }
    }
}
