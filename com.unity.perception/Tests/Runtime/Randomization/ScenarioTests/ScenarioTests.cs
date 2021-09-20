using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception;
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

            string RemoveWhitespace(string str) =>
                string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

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
        [Test]

        public void ScenarioCompletedAnalyticsSerializesCorrectly()
        {
            // Setup test randomizer
            var testRandomizer = new AllMembersAndParametersTestRandomizer();
            testRandomizer.colorRgbCategoricalParam.SetOptions(new (Color, float)[]
            {
                (Color.black, 0.4f),
                (Color.blue, 0.93f),
                (Color.red, 0.23f)
            });
            var randomizerData =
                PerceptionEngineAnalytics.RandomizerData.FromRandomizer(testRandomizer);

            Assert.IsTrue(randomizerData != null);

            // Parameters
            var expectedSerializedValue =
                new PerceptionEngineAnalytics.RandomizerData()
                {
                    name = nameof(AllMembersAndParametersTestRandomizer),
                    members = new[]
                    {
                        new PerceptionEngineAnalytics.MemberData()
                        {
                            name = "booleanMember",
                            type = "System.Boolean",
                            value = "False"
                        },
                        new PerceptionEngineAnalytics.MemberData()
                        {
                            name = "intMember",
                            type = "System.Int32",
                            value = "4"
                        },
                        new PerceptionEngineAnalytics.MemberData()
                        {
                            name = "uintMember",
                            type = "System.UInt32",
                            value = "2"
                        },
                        new PerceptionEngineAnalytics.MemberData()
                        {
                            name = "floatMember",
                            type = "System.Single",
                            value = "5"
                        },
                        new PerceptionEngineAnalytics.MemberData()
                        {
                            name = "vector2Member",
                            type = "UnityEngine.Vector2",
                            value = "(4.0, 7.0)"
                        },
                        new PerceptionEngineAnalytics.MemberData()
                        {
                            name = "unsupportedMember",
                            type = "UnityEngine.Perception.PerceptionEngineAnalytics+MemberData",
                            value = "UnityEngine.Perception.PerceptionEngineAnalytics+MemberData"
                        }
                    },
                    parameters = new[]
                    {
                        new PerceptionEngineAnalytics.ParameterData()
                        {
                            name = "booleanParam",
                            type = "BooleanParameter",
                            fields = new List<PerceptionEngineAnalytics.ParameterField>()
                            {
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "Constant",
                                    name = "value",
                                    value = 1
                                },
                            }
                        },
                        new PerceptionEngineAnalytics.ParameterData()
                        {
                            name = "floatParam",
                            type = "FloatParameter",
                            fields = new List<PerceptionEngineAnalytics.ParameterField>()
                            {
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "AnimationCurve",
                                    name = "value",
                                }
                            }
                        },
                        new PerceptionEngineAnalytics.ParameterData()
                        {
                            name = "integerParam",
                            type = "IntegerParameter",
                            fields = new List<PerceptionEngineAnalytics.ParameterField>()
                            {
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "Uniform",
                                    name = "value",
                                    rangeMinimum = -3, rangeMaximum = 7
                                }
                            }
                        },
                        new PerceptionEngineAnalytics.ParameterData()
                        {
                            name = "vector2Param",
                            type = "Vector2Parameter",
                            fields = new List<PerceptionEngineAnalytics.ParameterField>()
                            {
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "Constant",
                                    name = "x",
                                    value = 2
                                },
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "Uniform",
                                    name = "y",
                                    rangeMinimum = -4, rangeMaximum = 8
                                }
                            }
                        },
                        new PerceptionEngineAnalytics.ParameterData()
                        {
                            name = "vector3Param",
                            type = "Vector3Parameter",
                            fields = new List<PerceptionEngineAnalytics.ParameterField>()
                            {
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "Normal",
                                    name = "x",
                                    rangeMinimum = -5, rangeMaximum = 9,
                                    mean = 4, stdDev = 2
                                },
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "Constant",
                                    name = "y",
                                    value = 3
                                },
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "AnimationCurve",
                                    name = "z",
                                }
                            }
                        },
                        new PerceptionEngineAnalytics.ParameterData()
                        {
                            name = "vector4Param",
                            type = "Vector4Parameter",
                            fields = new List<PerceptionEngineAnalytics.ParameterField>()
                            {
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "Normal",
                                    name = "x",
                                    rangeMinimum = -5, rangeMaximum = 9,
                                    mean = 4, stdDev = 2
                                },
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "Constant",
                                    name = "y",
                                    value = 3
                                },
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "AnimationCurve",
                                    name = "z",
                                },
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "Uniform",
                                    name = "w",
                                    rangeMinimum = -12, rangeMaximum = 42
                                }
                            }
                        },
                        new PerceptionEngineAnalytics.ParameterData()
                        {
                            name = "colorRgbCategoricalParam",
                            type = "ColorRgbCategoricalParameter",
                            fields = new List<PerceptionEngineAnalytics.ParameterField>()
                            {
                                new PerceptionEngineAnalytics.ParameterField()
                                {
                                    distribution = "Categorical",
                                    name = "values",
                                    categoricalParameterCount = 3
                                }
                            }
                        }
                    }
                };

            var expectedSerializedValueJson = JsonConvert.SerializeObject(expectedSerializedValue);
            var serializedValueJson = JsonConvert.SerializeObject(randomizerData);

            Assert.AreEqual(expectedSerializedValueJson, serializedValueJson);
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
