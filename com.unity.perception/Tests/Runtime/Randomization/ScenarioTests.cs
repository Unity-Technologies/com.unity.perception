using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios;
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
                startingIteration = 2,
                totalIterations = 2
            };

            var changedConstants = new FixedLengthScenario.Constants
            {
                framesPerIteration = 0,
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
            Assert.AreEqual(m_Scenario.constants.framesPerIteration, constants.framesPerIteration);
            Assert.AreEqual(m_Scenario.constants.startingIteration, constants.startingIteration);
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
        public IEnumerator AppliesParametersEveryFrame()
        {
            var config = m_TestObject.AddComponent<ParameterConfiguration>();
            var parameter = config.AddParameter<Vector3Parameter>();
            parameter.x = new UniformSampler(1, 2);
            parameter.y = new UniformSampler(1, 2);
            parameter.z = new UniformSampler(1, 2);
            parameter.target.AssignNewTarget(
                m_TestObject, m_TestObject.transform, "position", ParameterApplicationFrequency.EveryFrame);

            var initialPosition = Vector3.zero;
            yield return CreateNewScenario(1, 5);

            // ReSharper disable once Unity.InefficientPropertyAccess
            Assert.AreNotEqual(initialPosition, m_TestObject.transform.position);
            // ReSharper disable once Unity.InefficientPropertyAccess
            initialPosition = m_TestObject.transform.position;

            yield return null;
            // ReSharper disable once Unity.InefficientPropertyAccess
            Assert.AreNotEqual(initialPosition, m_TestObject.transform.position);
        }

        [UnityTest]
        public IEnumerator AppliesParametersEveryIteration()
        {
            var config = m_TestObject.AddComponent<ParameterConfiguration>();
            var parameter = config.AddParameter<Vector3Parameter>();
            parameter.x = new UniformSampler(1, 2);
            parameter.y = new UniformSampler(1, 2);
            parameter.z = new UniformSampler(1, 2);

            var transform = m_TestObject.transform;
            var prevPosition = new Vector3();
            transform.position = prevPosition;
            parameter.target.AssignNewTarget(
                m_TestObject, transform, "position", ParameterApplicationFrequency.OnIterationSetup);


            yield return CreateNewScenario(2, 2);

            Assert.AreNotEqual(prevPosition, transform.position);
            // ReSharper disable once Unity.InefficientPropertyAccess
            prevPosition = transform.position;

            yield return null;
            // ReSharper disable once Unity.InefficientPropertyAccess
            Assert.AreEqual(prevPosition, transform.position);
            // ReSharper disable once Unity.InefficientPropertyAccess
            prevPosition = transform.position;

            yield return null;
            // ReSharper disable once Unity.InefficientPropertyAccess
            Assert.AreNotEqual(prevPosition, transform.position);
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
