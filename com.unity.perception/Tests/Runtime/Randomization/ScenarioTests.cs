﻿using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
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
                framesPerIteration = 2,
                startingIteration = 2,
                totalIterations = 2
            };

            var changedConstants = new FixedFrameLengthScenario.Constants
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
        public IEnumerator MultipleFrameIterationTest()
        {
            CreateNewScenario();
            const int testIterationFrameCount = 5;
            m_Scenario.constants.framesPerIteration = testIterationFrameCount;

            // Scenario update loop starts next frame
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
            m_Scenario.constants.framesPerIteration = 1;
            m_Scenario.constants.totalIterations = testIterationTotal;

            // Scenario update loop starts next frame
            yield return null;

            for (var i = 0; i < testIterationTotal; i++)
            {
                Assert.False(m_Scenario.isScenarioComplete);
                yield return null;
            }
            Assert.True(m_Scenario.isScenarioComplete);
        }

        [UnityTest]
        public IEnumerator AppliesParametersEveryFrame()
        {
            CreateNewScenario();
            m_Scenario.constants.framesPerIteration = 5;
            m_Scenario.constants.totalIterations = 1;

            var config = m_TestObject.AddComponent<ParameterConfiguration>();
            var parameter = config.AddParameter<Vector3Parameter>();
            parameter.x = new UniformSampler(1, 2);
            parameter.y = new UniformSampler(1, 2);
            parameter.z = new UniformSampler(1, 2);
            parameter.target.Set(
                m_TestObject, m_TestObject.transform, "position", ParameterApplicationFrequency.EveryFrame);

            var initialPosition = new Vector3();
            m_TestObject.transform.position = initialPosition;

            yield return null;
            Assert.AreNotEqual(initialPosition, m_TestObject.transform.position);
        }

        [UnityTest]
        public IEnumerator AppliesParametersEveryIteration()
        {
            CreateNewScenario();
            m_Scenario.constants.framesPerIteration = 5;
            m_Scenario.constants.totalIterations = 1;

            var config = m_TestObject.AddComponent<ParameterConfiguration>();
            var parameter = config.AddParameter<Vector3Parameter>();
            parameter.x = new UniformSampler(1, 2);
            parameter.y = new UniformSampler(1, 2);
            parameter.z = new UniformSampler(1, 2);

            var transform = m_Scenario.transform;
            parameter.target.Set(
                m_TestObject, transform, "position", ParameterApplicationFrequency.OnIterationSetup);

            var initialPosition = new Vector3();
            transform.position = initialPosition;

            // The position should change when the first iteration starts
            yield return null;
            Assert.AreNotEqual(initialPosition, transform.position);
            // ReSharper disable once Unity.InefficientPropertyAccess
            initialPosition = transform.position;

            // The position should stay the same since the iteration doesn't change for another 4 frames
            yield return null;
            // ReSharper disable once Unity.InefficientPropertyAccess
            Assert.AreEqual(initialPosition, transform.position);
        }
    }
}
