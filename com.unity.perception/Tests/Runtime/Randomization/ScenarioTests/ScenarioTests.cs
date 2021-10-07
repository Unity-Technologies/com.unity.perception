using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Perception.Analytics;
using UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Perception.RandomizationTests.ScenarioTests;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RandomizationTests.ScenarioTests
{
    [TestFixture]
    public class ScenarioTests
    {
        GameObject m_TestObject;
        TestFixedLengthScenario m_Scenario;

        static string RemoveWhitespace(string str) =>
            string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

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
        IEnumerator CreateNewScenario(int totalIterations, int framesPerIteration, Randomizer[] randomizers = null)
        {
            m_Scenario = m_TestObject.AddComponent<TestFixedLengthScenario>();
            m_Scenario.constants.totalIterations = totalIterations;
            m_Scenario.constants.framesPerIteration = framesPerIteration;

            if (randomizers != null)
            {
                foreach (var rnd in randomizers)
                {
                    m_Scenario.AddRandomizer(rnd);
                }
            }

            yield return null; // Skip first frame
            yield return null; // Skip first Update() frame
        }

        [Test]
        public void ScenarioConfigurationSerializesProperly()
        {
            m_TestObject = new GameObject();
            m_Scenario = m_TestObject.AddComponent<TestFixedLengthScenario>();
            m_Scenario.AddRandomizer(new RotationRandomizer());

            var expectedConfigAsset = (TextAsset)Resources.Load("SampleScenarioConfiguration");
            var expectedText = RemoveWhitespace(expectedConfigAsset.text);
            var scenarioJson = RemoveWhitespace(m_Scenario.SerializeToJson());
            Assert.AreEqual(expectedText, scenarioJson);
        }

        [Test]
        public void ScenarioConfigurationOverwrittenDuringDeserialization()
        {
            m_TestObject = new GameObject();
            m_Scenario = m_TestObject.AddComponent<TestFixedLengthScenario>();

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
            m_Scenario.configuration = new TextAsset(m_Scenario.SerializeToJson());

            // Change the values
            m_Scenario.constants = changedConstants;
            m_Scenario.DeserializeConfigurationInternal();

            // Check if the values reverted correctly
            Assert.AreEqual(m_Scenario.constants.framesPerIteration, constants.framesPerIteration);
            Assert.AreEqual(m_Scenario.constants.totalIterations, constants.totalIterations);
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
                Assert.True(m_Scenario.state == ScenarioBase.State.Playing);
                yield return null;
            }

            Assert.True(m_Scenario.state == ScenarioBase.State.Idle);
        }

        [UnityTest]
        public IEnumerator StartNewDatasetSequenceEveryIteration()
        {
            var perceptionCamera = SetupPerceptionCamera();

            yield return CreateNewScenario(2, 2);
            Assert.AreEqual(DatasetCapture.SimulationState.SequenceTime, 0);

            // Second frame, first iteration
            yield return null;
            Assert.AreEqual(DatasetCapture.SimulationState.SequenceTime, perceptionCamera.simulationDeltaTime);

            // Third frame, second iteration, SequenceTime has been reset
            yield return null;
            Assert.AreEqual(DatasetCapture.SimulationState.SequenceTime, 0);
        }

        [UnityTest]
        public IEnumerator GeneratedRandomSeedsChangeWithScenarioIteration()
        {
            yield return CreateNewScenario(3, 1);
            var seeds = new uint[3];
            for (var i = 0; i < 3; i++)
                seeds[i] = SamplerState.NextRandomState();

            yield return null;
            for (var i = 0; i < 3; i++)
                Assert.AreNotEqual(seeds[i], SamplerState.NextRandomState());
        }

        [UnityTest]
        public IEnumerator IterationCorrectlyDelays()
        {
            yield return CreateNewScenario(5, 1, new Randomizer[]
            {
                // Delays every other iteration
                new ExampleDelayRandomizer(2)
            });

            // State: currentIteration = 0
            Assert.AreEqual(0, m_Scenario.currentIteration);
            yield return null;

            // State: currentIteration = 1
            Assert.AreEqual(1, m_Scenario.currentIteration);
            yield return null;

            // State: currentIteration = 2
            // Action: ExampleDelayRandomizer will delay the iteration
            Assert.AreEqual(2, m_Scenario.currentIteration);
            yield return null;

            // State: currentIteration = 2
            Assert.AreEqual(2, m_Scenario.currentIteration);
            yield return null;

            // State: currentIteration = 3;
            Assert.AreEqual(3, m_Scenario.currentIteration);
            yield return null;

            // State: currentIteration = 4
            // Action: ExampleDelayRandomizer will delay the iteration
            Assert.AreEqual(4, m_Scenario.currentIteration);
            yield return null;

            // State: currentIteration = 4
            Assert.AreEqual(4, m_Scenario.currentIteration);
            yield return null;

            // State: currentIteration = 5
            Assert.AreEqual(5, m_Scenario.currentIteration);
        }

        [UnityTest]
        public IEnumerator ScenarioCompletedAnalyticsSerializesCorrectly()
        {
            // Perception Camera Serialization
            var perceptionCamera = m_TestObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureTriggerMode = CaptureTriggerMode.Scheduled;
            perceptionCamera.firstCaptureFrame = 2;
            perceptionCamera.framesBetweenCaptures = 10;

            // Labeler serialization
            var sampleIdLabelConfig = Resources.Load<IdLabelConfig>("sampleIdLabelConfig");
            var sampleAnimationPoseConfig = Resources.Load<AnimationPoseConfig>("sampleAnimationPoseConfig");
            var emptyKeypointTemplate = ScriptableObject.CreateInstance<KeypointTemplate>();
            emptyKeypointTemplate.keypoints = new KeypointDefinition[] { };
            emptyKeypointTemplate.skeleton = new SkeletonDefinition[] { };

            perceptionCamera.AddLabeler(new BoundingBox2DLabeler(sampleIdLabelConfig));
            perceptionCamera.AddLabeler(new RenderedObjectInfoLabeler(sampleIdLabelConfig));
            perceptionCamera.AddLabeler(new KeypointLabeler()
            {
                idLabelConfig = sampleIdLabelConfig,
                objectFilter = KeypointObjectFilter.Visible,
                activeTemplate = emptyKeypointTemplate,
                animationPoseConfigs = new List<AnimationPoseConfig>()
                {
                    sampleAnimationPoseConfig, sampleAnimationPoseConfig
                }
            });

            // Randomizer Serialization
            var randomizers = new List<Randomizer>();
            var testRandomizer = new AllMembersAndParametersTestRandomizer();
            testRandomizer.colorRgbCategoricalParam.SetOptions(new (Color, float)[]
            {
                (Color.black, 0.4f),
                (Color.blue, 0.93f),
                (Color.red, 0.23f)
            });
            randomizers.Add(testRandomizer);

            yield return null;

            // Scenario Completed Serialization
            var scenarioCompletedData = ScenarioCompletedData.FromCameraAndRandomizers(perceptionCamera, randomizers);
            var expectedRandomizerJson = RemoveWhitespace(((TextAsset)Resources.Load("analyticsSerializationExample")).text);
            var actualRandomizerJson = RemoveWhitespace(JsonConvert.SerializeObject(scenarioCompletedData));

            Assert.AreEqual(expectedRandomizerJson, actualRandomizerJson);

            Object.DestroyImmediate(emptyKeypointTemplate);
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
