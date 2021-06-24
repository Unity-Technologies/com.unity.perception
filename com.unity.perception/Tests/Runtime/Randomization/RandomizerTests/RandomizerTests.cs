using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.TestTools;

namespace RandomizationTests.RandomizerTests
{
    [TestFixture]
    public class RandomizerTests
    {
        GameObject m_TestObject;
        FixedLengthScenario m_Scenario;

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

        // TODO: update this function once the perception camera doesn't skip the first frame
        IEnumerator CreateNewScenario(int totalIterations, int framesPerIteration)
        {
            m_Scenario = m_TestObject.AddComponent<FixedLengthScenario>();
            m_Scenario.constants.totalIterations = totalIterations;
            m_Scenario.constants.framesPerIteration = framesPerIteration;
            yield return null; // Skip first frame
        }

        [Test]
        public void OneRandomizerInstancePerTypeTest()
        {
            m_Scenario = m_TestObject.AddComponent<FixedLengthScenario>();
            m_Scenario.AddRandomizer(new ExampleTransformRandomizer());
            Assert.Throws<ScenarioException>(() => m_Scenario.AddRandomizer(new ExampleTransformRandomizer()));
        }

        [UnityTest]
        public IEnumerator OnUpdateExecutesEveryFrame()
        {
            yield return CreateNewScenario(10, 1);
            m_Scenario.AddRandomizer(new ExampleTransformRandomizer());
            var transform = m_TestObject.transform;
            var initialPosition = Vector3.zero;
            transform.position = initialPosition;

            yield return null;
            Assert.AreNotEqual(initialPosition, transform.position);
            // ReSharper disable once Unity.InefficientPropertyAccess
            initialPosition = transform.position;

            yield return null;
            // ReSharper disable once Unity.InefficientPropertyAccess
            Assert.AreNotEqual(initialPosition, transform.position);
        }

        [UnityTest]
        public IEnumerator OnIterationStartExecutesEveryIteration()
        {
            yield return CreateNewScenario(10, 2);
            m_Scenario.AddRandomizer(new ExampleTransformRandomizer());
            var transform = m_TestObject.transform;
            var initialRotation = Quaternion.identity;
            transform.rotation = initialRotation;

            yield return null;
            Assert.AreNotEqual(initialRotation, transform.rotation);
            // ReSharper disable once Unity.InefficientPropertyAccess
            initialRotation = transform.rotation;

            yield return null;
            // ReSharper disable once Unity.InefficientPropertyAccess
            Assert.AreEqual(initialRotation, transform.rotation);
            // ReSharper disable once Unity.InefficientPropertyAccess
            initialRotation = transform.rotation;

            yield return null;
            // ReSharper disable once Unity.InefficientPropertyAccess
            Assert.AreNotEqual(initialRotation, transform.rotation);
        }
    }
}
