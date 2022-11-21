using System;
using System.Collections;
using System.Collections.Generic;
using GroundTruthTests;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Analytics;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
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

        #region Helpers
        static string RemoveWhitespace(string str) =>
            string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

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

        // TODO: update this function once the perception camera doesn't skip the first frame
        IEnumerator CreateNewScenario(int iterationCount, int framesPerIteration, Randomizer[] randomizers = null)
        {
            m_TestObject.SetActive(false);
            m_Scenario = m_TestObject.AddComponent<TestFixedLengthScenario>();
            m_Scenario.constants.iterationCount = iterationCount;
            m_Scenario.framesPerIteration = framesPerIteration;
            m_TestObject.SetActive(true);

            if (randomizers != null)
            {
                foreach (var rnd in randomizers)
                {
                    m_Scenario.AddRandomizer(rnd);
                }
            }

            if (PerceptionCamera.captureFrameCount < 0)
            {
                yield return null;
            }
        }

        #endregion

        #region Setup & Teardown
        [SetUp, UnitySetUp]
        public void Setup()
        {
            if (m_TestObject)
                Object.DestroyImmediate(m_TestObject);

            m_TestObject = new GameObject();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        #endregion

        [Test]
        public void ScenarioConfigurationSerializesProperly()
        {
            m_TestObject = new GameObject();
            m_Scenario = m_TestObject.AddComponent<TestFixedLengthScenario>();
            m_Scenario.AddRandomizer(new RotationRandomizer());

            var expectedConfigAsset = new TextAsset(@"{""constants"":{""startIteration"":0,""iterationCount"":100,""randomSeed"":539662031},""randomizers"":{""randomizerGroups"":[{""randomizerId"":""RotationRandomizer"",""metadata"":{""name"":"""",""description"":"""",""imageLink"":""""},""state"":{""enabled"":true,""canBeSwitchedByUser"":true},""items"":{""rotation"":{""param"":{""metadata"":{""name"":"""",""description"":"""",""imageLink"":""""},""items"":{""x"":{""samplerOptions"":{""metadata"":{""name"":"""",""description"":"""",""imageLink"":""""},""uniform"":{""min"":0.0,""max"":360.0}}},""y"":{""samplerOptions"":{""metadata"":{""name"":"""",""description"":"""",""imageLink"":""""},""uniform"":{""min"":0.0,""max"":360.0}}},""z"":{""samplerOptions"":{""metadata"":{""name"":"""",""description"":"""",""imageLink"":""""},""uniform"":{""min"":0.0,""max"":360.0}}}}}}}}]}}");

            var expectedText = expectedConfigAsset.text;
            var scenarioJson = RemoveWhitespace(m_Scenario.SerializeToJson());
            Assert.AreEqual(expectedText, scenarioJson);
        }

        [Test]
        public void ScenarioDeserializesExtraFixedFrameConstants()
        {
            m_TestObject = new GameObject();
            m_Scenario = m_TestObject.AddComponent<TestFixedLengthScenario>();
            m_Scenario.AddRandomizer(new RotationRandomizer());

            m_Scenario.configuration = new TextAsset(@"{
  ""constants"": {
    ""startIteration"": 1,
    ""iterationCount"": 2,
    ""totalIterations"":  4,
    ""instanceCount"" :  5,
    ""instanceIndex"" : 6,
    ""randomSeed"": 7
  }
}");
            m_Scenario.DeserializeConfigurationInternal();

            Assert.AreEqual(1, m_Scenario.constants.startIteration);
            Assert.AreEqual(2, m_Scenario.constants.iterationCount);
            Assert.AreEqual(4, m_Scenario.constants.totalIterations);
            Assert.AreEqual(5, m_Scenario.constants.instanceCount);
            Assert.AreEqual(6, m_Scenario.constants.instanceIndex);
            Assert.AreEqual(7, m_Scenario.constants.randomSeed);
        }

        [Test]
        public void ScenarioConfigurationOverwrittenDuringDeserialization()
        {
            m_TestObject = new GameObject();
            m_Scenario = m_TestObject.AddComponent<TestFixedLengthScenario>();

            var constants = new FixedLengthScenario.Constants
            {
                iterationCount = 2
            };

            var changedConstants = new FixedLengthScenario.Constants
            {
                iterationCount = 0
            };

            // Serialize some values
            m_Scenario.constants = constants;
            m_Scenario.configuration = new TextAsset(m_Scenario.SerializeToJson());

            // Change the values
            m_Scenario.constants = changedConstants;
            m_Scenario.DeserializeConfigurationInternal();

            // Check if the values reverted correctly
            Assert.AreEqual(m_Scenario.constants.iterationCount, constants.iterationCount);
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

#if UNITY_SIMULATION_CORE_PRESENT
        [UnityTest]
        public IEnumerator CloudConstantsProcessedCorrectly()
        {
            const int expectedIterationCount = 40;

            // Deactivate and reactivate so that Awake() is not called until constants are filled out
            m_TestObject.SetActive(false);
            m_Scenario = m_TestObject.AddComponent<TestFixedLengthScenario>();
            m_Scenario.m_SimulationRunningInCloudOverride = true;
            m_Scenario.constants.startIteration = 10;
            m_Scenario.constants.iterationCount = 100;
            m_Scenario.constants.instanceCount = 5;
            m_Scenario.constants.totalIterations = 200;
            m_TestObject.SetActive(true);

            yield return null;
            for (var i = 0; i < expectedIterationCount; i++)
            {
                Assert.AreEqual(ScenarioBase.State.Playing, m_Scenario.state);
                Assert.AreEqual(i * m_Scenario.constants.instanceCount, m_Scenario.currentIteration);
                yield return null;
            }

            Assert.AreEqual(ScenarioBase.State.Idle, m_Scenario.state);
        }

#endif

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
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);

            var perceptionCamera = SetupPerceptionCamera();

            yield return CreateNewScenario(2, 2);
            Assert.AreEqual(DatasetCapture.currentSimulation.SequenceTime, 0);

            // Second frame, first iteration
            yield return null;
            Assert.AreEqual(DatasetCapture.currentSimulation.SequenceTime, perceptionCamera.simulationDeltaTime);

            // Third frame, second iteration, SequenceTime has been reset
            yield return null;
            Assert.AreEqual(DatasetCapture.currentSimulation.SequenceTime, 0);
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
            var sensorWidth = perceptionCamera.cameraSensor.pixelWidth;
            var sensorHeight = perceptionCamera.cameraSensor.pixelHeight;

            // Labeler serialization
            var idLabelConfig = ScriptableObject.CreateInstance<IdLabelConfig>();
            idLabelConfig.startingLabelId = StartingLabelId.One;
            idLabelConfig.Init(new List<IdLabelEntry>
            {
                new() { id = 1, label = "Test1", hierarchyRelation = HierarchyRelation.Independent},
                new() { id = 2, label = "Test2", hierarchyRelation = HierarchyRelation.Independent },
                new() { id = 3, label = "Test3", hierarchyRelation = HierarchyRelation.Independent }
            });


            perceptionCamera.AddLabeler(new BoundingBox2DLabeler(idLabelConfig));
            perceptionCamera.AddLabeler(new RenderedObjectInfoLabeler(idLabelConfig));

            // Randomizer Serialization
            var testRandomizer = new AllMembersAndParametersTestRandomizer();
            testRandomizer.colorRgbCategoricalParam.SetOptions(new(Color, float)[]
            {
                (Color.black, 0.4f),
                (Color.blue, 0.93f),
                (Color.red, 0.23f)
            });

            // Scenario serialization
            yield return CreateNewScenario(20, 1, new Randomizer[] { testRandomizer });

            yield return null;
            yield return null;

            var cameras = Object.FindObjectsOfType<PerceptionCamera>();
            var scenarioCompletedData = ScenarioCompletedData.FromCamerasAndRandomizers(cameras, m_Scenario);
            var actualRandomizerJson = RemoveWhitespace(JsonConvert.SerializeObject(scenarioCompletedData));
            actualRandomizerJson = actualRandomizerJson.Replace(".00", ".0");

            var expectedRandomizerJson = @"{""platform"":""$Platform$"",""perceptionCameras"":[{""captureTriggerMode"":""Scheduled"",""startAtFrame"":2,""framesBetweenCaptures"":10,""perceptionCameraIndex"":0}],""sensors"":[{""type"":""UnityCamera"",""width"":""$SensorWidth$"",""height"":""$SensorHeight$"",""perceptionCameraIndex"":0}],""labelers"":[{""name"":""BoundingBox2DLabeler"",""enabled"":true,""perceptionCameraIndex"":0,""labelConfigCount"":3,""objectFilter"":"""",""animationPoseCount"":-1},{""name"":""RenderedObjectInfoLabeler"",""enabled"":true,""perceptionCameraIndex"":0,""labelConfigCount"":3,""objectFilter"":"""",""animationPoseCount"":-1}],""randomizers"":[{""name"":""AllMembersAndParametersTestRandomizer"",""members"":[{""name"":""booleanMember"",""value"":""False"",""type"":""System.Boolean""},{""name"":""intMember"",""value"":""4"",""type"":""System.Int32""},{""name"":""uintMember"",""value"":""2"",""type"":""System.UInt32""},{""name"":""floatMember"",""value"":""5"",""type"":""System.Single""},{""name"":""vector2Member"",""value"":""(4.0,7.0)"",""type"":""UnityEngine.Vector2""},{""name"":""unsupportedMember"",""value"":""UnityEngine.Perception.Randomization.Samplers.UniformSampler"",""type"":""UnityEngine.Perception.Randomization.Samplers.UniformSampler""}],""parameters"":[{""name"":""booleanParam"",""type"":""BooleanParameter"",""fields"":[{""name"":""value"",""distribution"":""Constant"",""value"":1.0,""rangeMinimum"":0.0,""rangeMaximum"":0.0,""mean"":0.0,""stdDev"":0.0,""categoricalParameterCount"":0}]},{""name"":""floatParam"",""type"":""FloatParameter"",""fields"":[{""name"":""value"",""distribution"":""AnimationCurve"",""value"":0.0,""rangeMinimum"":0.0,""rangeMaximum"":0.0,""mean"":0.0,""stdDev"":0.0,""categoricalParameterCount"":0}]},{""name"":""integerParam"",""type"":""IntegerParameter"",""fields"":[{""name"":""value"",""distribution"":""Uniform"",""value"":0.0,""rangeMinimum"":-3.0,""rangeMaximum"":7.0,""mean"":0.0,""stdDev"":0.0,""categoricalParameterCount"":0}]},{""name"":""vector2Param"",""type"":""Vector2Parameter"",""fields"":[{""name"":""x"",""distribution"":""Constant"",""value"":2.0,""rangeMinimum"":0.0,""rangeMaximum"":0.0,""mean"":0.0,""stdDev"":0.0,""categoricalParameterCount"":0},{""name"":""y"",""distribution"":""Uniform"",""value"":0.0,""rangeMinimum"":-4.0,""rangeMaximum"":8.0,""mean"":0.0,""stdDev"":0.0,""categoricalParameterCount"":0}]},{""name"":""vector3Param"",""type"":""Vector3Parameter"",""fields"":[{""name"":""x"",""distribution"":""Normal"",""value"":0.0,""rangeMinimum"":-5.0,""rangeMaximum"":9.0,""mean"":4.0,""stdDev"":2.0,""categoricalParameterCount"":0},{""name"":""y"",""distribution"":""Constant"",""value"":3.0,""rangeMinimum"":0.0,""rangeMaximum"":0.0,""mean"":0.0,""stdDev"":0.0,""categoricalParameterCount"":0},{""name"":""z"",""distribution"":""AnimationCurve"",""value"":0.0,""rangeMinimum"":0.0,""rangeMaximum"":0.0,""mean"":0.0,""stdDev"":0.0,""categoricalParameterCount"":0}]},{""name"":""vector4Param"",""type"":""Vector4Parameter"",""fields"":[{""name"":""x"",""distribution"":""Normal"",""value"":0.0,""rangeMinimum"":-5.0,""rangeMaximum"":9.0,""mean"":4.0,""stdDev"":2.0,""categoricalParameterCount"":0},{""name"":""y"",""distribution"":""Constant"",""value"":3.0,""rangeMinimum"":0.0,""rangeMaximum"":0.0,""mean"":0.0,""stdDev"":0.0,""categoricalParameterCount"":0},{""name"":""z"",""distribution"":""AnimationCurve"",""value"":0.0,""rangeMinimum"":0.0,""rangeMaximum"":0.0,""mean"":0.0,""stdDev"":0.0,""categoricalParameterCount"":0},{""name"":""w"",""distribution"":""Uniform"",""value"":0.0,""rangeMinimum"":-12.0,""rangeMaximum"":42.0,""mean"":0.0,""stdDev"":0.0,""categoricalParameterCount"":0}]},{""name"":""colorRgbCategoricalParam"",""type"":""CategoricalParameter"",""fields"":[{""name"":""values"",""distribution"":""Categorical"",""value"":0.0,""rangeMinimum"":0.0,""rangeMaximum"":0.0,""mean"":0.0,""stdDev"":0.0,""categoricalParameterCount"":3}]}]}],""framesPerIteration"":1,""iterationsRun"":2,""iterationsPlanned"":20,""totalFrameCount"":3,""startIteration"":0}";

            // some things need to be dynamically updated
            expectedRandomizerJson = expectedRandomizerJson
                .Replace("$Platform$", Application.platform.ToString())
                .Replace("\"$SensorWidth$\"", $"{sensorWidth}")
                .Replace("\"$SensorHeight$\"", $"{sensorHeight}");
            expectedRandomizerJson = RemoveWhitespace(expectedRandomizerJson);

            Assert.AreEqual(expectedRandomizerJson, actualRandomizerJson);
        }
    }
}
