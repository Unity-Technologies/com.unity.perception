using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json.Serialization;
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
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        void CreateNewScenario()
        {
            m_Scenario = m_TestObject.AddComponent<FixedFrameLengthScenario>();
            m_Scenario.quitOnComplete = false;
        }

        [UnityTest]
        public IEnumerator SerializationTest()
        {
            CreateNewScenario();
            m_Scenario.serializedConstantsFileName = "perception_serialization_test";

            var constants = new FixedFrameLengthScenario.Constants
            {
                iterationFrameCount = 2,
                startingIteration = 2,
                totalIterations = 2
            };

            var changedConstants = new FixedFrameLengthScenario.Constants
            {
                iterationFrameCount = 0,
                startingIteration = 0,
                totalIterations = 0
            };

            // Serialize some values
            m_Scenario.constants = constants;
            m_Scenario.Serialize();

            // Change the values
            m_Scenario.constants = changedConstants;
            m_Scenario.Deserialize();

            // Check if the values reverted correctly
            Assert.AreEqual(m_Scenario.constants.iterationFrameCount, constants.iterationFrameCount);
            Assert.AreEqual(m_Scenario.constants.startingIteration, constants.startingIteration);
            Assert.AreEqual(m_Scenario.constants.totalIterations, constants.totalIterations);

            // Clean up serialized constants
            File.Delete(m_Scenario.serializedConstantsFilePath);

            yield return null;
        }

        [UnityTest]
        public IEnumerator MultipleFrameIterationTest()
        {
            CreateNewScenario();
            const int testIterationFrameCount = 5;
            m_Scenario.constants.iterationFrameCount = testIterationFrameCount;

            // Update loop starts next frame
            yield return null;

            for (var i = 0; i < testIterationFrameCount; i++)
            {
                Assert.AreEqual(0, m_Scenario.currentIteration);
                yield return null;
            }
            Assert.AreEqual(1, m_Scenario.currentIteration);
        }

        [UnityTest]
        public IEnumerator ScenarioCompletionTest()
        {
            CreateNewScenario();
            const int testIterationTotal = 5;
            m_Scenario.constants.iterationFrameCount = 1;
            m_Scenario.constants.totalIterations = testIterationTotal;

            // Update loop starts next frame
            yield return null;

            for (var i = 0; i < testIterationTotal; i++)
            {
                Assert.False(m_Scenario.isScenarioComplete);
                yield return null;
            }
            Assert.True(m_Scenario.isScenarioComplete);
        }
    }
}
