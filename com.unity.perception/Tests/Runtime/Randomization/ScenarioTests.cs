using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RandomizationTests
{
    [TestFixture]
    public class ScenarioTests
    {
        GameObject m_TestObject;
        FixedFrameLengthScenario m_Scenario;

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
            m_Scenario = m_TestObject.AddComponent<FixedFrameLengthScenario>();
            m_Scenario.quitOnComplete = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        [UnityTest]
        public IEnumerator SerializationTest()
        {
            m_Scenario.serializedConstantsFileName = "perception_serialization_test";

            const int iterationFrameCount = 2;
            const int startingIteration = 2;
            const int totalIterations = 2;

            // Serialize some values
            m_Scenario.constants.iterationFrameCount = iterationFrameCount;
            m_Scenario.constants.startingIteration = startingIteration;
            m_Scenario.constants.totalIterations = totalIterations;

            m_Scenario.Serialize();

            // Change the values
            m_Scenario.constants.iterationFrameCount = 0;
            m_Scenario.constants.startingIteration = 0;
            m_Scenario.constants.totalIterations = 0;

            m_Scenario.Deserialize();

            // Check if the values reverted correctly
            Assert.AreEqual(m_Scenario.constants.iterationFrameCount, iterationFrameCount);
            Assert.AreEqual(m_Scenario.constants.startingIteration, startingIteration);
            Assert.AreEqual(m_Scenario.constants.totalIterations, totalIterations);

            File.Delete(m_Scenario.serializedConstantsFilePath);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ScenarioCompletionTest()
        {
            // Dependent on test script execution order
            var startingIteration = m_Scenario.currentIteration;

            const int testIterationTotal = 5;
            m_Scenario.constants.totalIterations = testIterationTotal;
            for (var i = 0; i < testIterationTotal - startingIteration; i++)
            {
                Assert.False(m_Scenario.isScenarioComplete);
                yield return null;
            }
            Assert.True(m_Scenario.isScenarioComplete);
        }
    }
}
