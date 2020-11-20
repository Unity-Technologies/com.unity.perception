using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RandomizationTests
{
    [TestFixture]
    public class ScenarioTests
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
            m_Scenario.quitOnComplete = false;
            m_Scenario.constants.totalIterations = totalIterations;
            m_Scenario.constants.framesPerIteration = framesPerIteration;
            yield return null; // Skip Start() frame
            yield return null; // Skip first Update() frame
        }

        [UnityTest]
        public IEnumerator OverwritesConstantsOnSerialization()
        {
            yield return CreateNewScenario(10, 10);
            m_Scenario.serializedConstantsFileName = "perception_serialization_test";

            var constants = new FixedLengthScenario.Constants
            {
                framesPerIteration = 2,
                totalIterations = 2
            };

            var changedConstants = new FixedLengthScenario.Constants
            {
                framesPerIteration = 0,
                totalIterations = 0
            };

            // Serialize some values
            m_Scenario.constants = constants;
            m_Scenario.Serialize();

            // Change the values
            m_Scenario.constants = changedConstants;
            m_Scenario.Deserialize();

            // Check if the values reverted correctly
            Assert.AreEqual(m_Scenario.constants.framesPerIteration, constants.framesPerIteration);
            Assert.AreEqual(m_Scenario.constants.totalIterations, constants.totalIterations);

            // Clean up serialized constants
            File.Delete(m_Scenario.serializedConstantsFilePath);

            yield return null;
        }

        [UnityTest]
        public IEnumerator IterationsCanLastMultipleFrames()
        {
            const int frameCount = 5;
            yield return CreateNewScenario(1, frameCount);
            for (var i = 0; i < frameCount; i++)
            {
                Assert.AreEqual(0, m_Scenario.currentIteration);
                yield return null;
            }
            Assert.AreEqual(1, m_Scenario.currentIteration);
        }

        [UnityTest]
        public IEnumerator FinishesWhenIsScenarioCompleteIsTrue()
        {
            const int iterationCount = 5;
            yield return CreateNewScenario(iterationCount, 1);
            for (var i = 0; i < iterationCount; i++)
            {
                Assert.False(m_Scenario.isScenarioComplete);
                yield return null;
            }
            Assert.True(m_Scenario.isScenarioComplete);
        }

        [UnityTest]
        public IEnumerator StartNewDatasetSequenceEveryIteration()
        {
            var perceptionCamera = SetupPerceptionCamera();

            yield return CreateNewScenario(2, 2);
            Assert.AreEqual(DatasetCapture.SimulationState.SequenceTime, 0);

            // Second frame, first iteration
            yield return null;
            Assert.AreEqual(DatasetCapture.SimulationState.SequenceTime, perceptionCamera.period);

            // Third frame, second iteration, SequenceTime has been reset
            yield return null;
            Assert.AreEqual(DatasetCapture.SimulationState.SequenceTime, 0);
        }

        [UnityTest]
        public IEnumerator GeneratedRandomSeedsChangeWithScenarioIteration()
        {
            yield return CreateNewScenario(3, 1);
            var seed = m_Scenario.GenerateRandomSeed();
            var seeds = new uint[3];
            for (var i = 0; i < 3; i++)
                seeds[i] = m_Scenario.GenerateRandomSeedFromIndex(i);

            yield return null;
            Assert.AreNotEqual(seed, m_Scenario.GenerateRandomSeed());
            for (var i = 0; i < 3; i++)
                Assert.AreNotEqual(seeds[i], m_Scenario.GenerateRandomSeedFromIndex(i));
        }

        PerceptionCamera SetupPerceptionCamera()
        {
            m_TestObject.SetActive(false);
            var camera = m_TestObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;

            var perceptionCamera = m_TestObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;

            m_TestObject.SetActive(true);
            return perceptionCamera;
        }
    }
}
