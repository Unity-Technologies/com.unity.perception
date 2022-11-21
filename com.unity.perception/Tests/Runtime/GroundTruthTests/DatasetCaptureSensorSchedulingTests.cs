using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.Settings;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace GroundTruthTests
{
    // Provides accessors and invocation methods for members of SimulationState that would otherwise be in-accessible
    // due to protection level - use only when testing protected logic is critical
    class SimulationStateTestHelper
    {
        IDictionary<SensorHandle, SimulationState.SensorData> m_SensorsReference;
        MethodInfo m_SequenceTimeOfNextCaptureMethod;
        MethodInfo m_ComputeTotalFramesWithAccumulationMethod;

        SimulationState m_State;

        internal SimulationStateTestHelper()
        {
            m_State = DatasetCapture.currentSimulation;
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            m_SequenceTimeOfNextCaptureMethod = m_State.GetType().GetMethod("GetSequenceTimeOfNextCapture", bindingFlags);
            Debug.Assert(m_SequenceTimeOfNextCaptureMethod != null, "Couldn't find sequence time method.");
#if HDRP_PRESENT
            m_ComputeTotalFramesWithAccumulationMethod = m_State.GetType().GetMethod("ComputeTotalFramesWithAccumulation", bindingFlags);
            Debug.Assert(m_ComputeTotalFramesWithAccumulationMethod != null, "Couldn't find compute total frames method.");
#endif
            var sensorsField = m_State.GetType().GetField("m_Sensors", bindingFlags);
            Debug.Assert(sensorsField != null, "Couldn't find internal sensors field");
            m_SensorsReference = (IDictionary<SensorHandle, SimulationState.SensorData>)(sensorsField.GetValue(m_State));
            Debug.Assert(m_SensorsReference != null, "Couldn't cast sensor field to dictionary");
        }

        internal float CallSequenceTimeOfNextCapture(SimulationState.SensorData sensorData)
        {
            return (float)m_SequenceTimeOfNextCaptureMethod.Invoke(m_State, new object[] { sensorData });
        }

#if HDRP_PRESENT
        internal int CallComputeTotalFramesWithAccumulation(int framesPerIteration)
        {
            return (int)m_ComputeTotalFramesWithAccumulationMethod.Invoke(m_State, new object[] { framesPerIteration });
        }

#endif

        internal SimulationState.SensorData GetSensorData(SensorHandle sensorHandle)
        {
            return m_SensorsReference[sensorHandle];
        }
    }

    [TestFixture]
    public class DatasetCaptureSensorSchedulingTests
    {
        SimulationStateTestHelper m_TestHelper;

        [SetUp]
        public void SetUp()
        {
            m_TestHelper = new SimulationStateTestHelper();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1;
            DatasetCapture.ResetSimulation();
        }

        SensorDefinition CreateSensorDefinition(string id, string modality, string def, int firstFrame, CaptureTriggerMode mode, float deltaTime, int framesBetween, bool manualSensorAffectSimulationTiming = false)
        {
            return new SensorDefinition(id, modality, def)
            {
                firstCaptureFrame = firstFrame,
                captureTriggerMode = mode,
                simulationDeltaTime = deltaTime,
                framesBetweenCaptures = framesBetween,
                manualSensorsAffectTiming = manualSensorAffectSimulationTiming,
            };
        }

        RgbSensorDefinition CreateRgbSensorDefinition(string id, string modality, string def, int firstFrame, CaptureTriggerMode mode, float deltaTime, int framesBetween, bool manualSensorAffectSimulationTiming = false, bool useAccumulation = false)
        {
            return new RgbSensorDefinition(id, modality, def)
            {
                firstCaptureFrame = firstFrame,
                captureTriggerMode = mode,
                simulationDeltaTime = deltaTime,
                framesBetweenCaptures = framesBetween,
                manualSensorsAffectTiming = manualSensorAffectSimulationTiming,
                useAccumulation = useAccumulation
            };
        }

#if HDRP_PRESENT


        [UnityTest]
        [TestCase(5, 1, 1, new int[] {0}, new int[] {0}, 1 + 5, ExpectedResult = (IEnumerator)null)]
        [TestCase(5, 300, 1, new int[] {0}, new int[] {0}, 300 + 300 * 5, ExpectedResult = (IEnumerator)null)]
        [TestCase(5, 60, 1, new int[] {0}, new int[] {15}, 60 + 4 * 5, ExpectedResult = (IEnumerator)null)]
        [TestCase(5, 11, 1, new int[] {10}, new int[] {0}, 11 + 5, ExpectedResult = (IEnumerator)null)]
        [TestCase(5, 11, 1, new int[] {10, 10}, new int[] {10, 10}, 11 + 5, ExpectedResult = (IEnumerator)null)]
        [TestCase(5, 22, 1, new int[] {10}, new int[] {10}, 22 + 2 * 5, ExpectedResult = (IEnumerator)null)]
        [TestCase(5, 20, 1, new int[] {0, 0}, new int[] {5, 10}, 20 + 4 * 5, ExpectedResult = (IEnumerator)null)]
        [TestCase(5, 23, 1, new int[] {0, 10}, new int[] {10, 10}, 23 + 3 * 5, ExpectedResult = (IEnumerator)null)]
        public IEnumerator FixedLengthScenarioChangesToCorrectFramesPerIteration(int accumulationSamples, int framesPerIteration, int numberOfCameras, int[] startAtFrames, int[] framesBetweenCapture, int expectedFramesPerIteration)
        {
            Time.fixedDeltaTime = 0.02f;
            Time.timeScale = 1;

            PerceptionSettings.instance.accumulationSettings = new AccumulationSettings()
            {
                accumulationSamples = accumulationSamples,
                shutterInterval = 1,
                shutterFullyOpen = 0,
                shutterBeginsClosing = 1,
                adaptFixedLengthScenarioFrames = true
            };

            yield return null;
            yield return null;
            yield return null;

            for (int i = 0; i < numberOfCameras; i++)
            {
                DatasetCapture.RegisterSensor(CreateRgbSensorDefinition("cam" + i, "", "", startAtFrames[i], CaptureTriggerMode.Scheduled, 0.0166f, framesBetweenCapture[i], useAccumulation: true));
            }

            int result = m_TestHelper.CallComputeTotalFramesWithAccumulation(framesPerIteration);

            Assert.AreEqual(expectedFramesPerIteration, result);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SequenceTimeOfNextCapture_ReportsCorrectTime_WithAccumulation()
        {
            var firstCaptureFrame = 2;
            var simulationDeltaTime = .4f;

            Time.fixedDeltaTime = 0.02f;
            Time.timeScale = 1;

            PerceptionSettings.instance.accumulationSettings = new AccumulationSettings()
            {
                accumulationSamples = 6,
                shutterInterval = 1,
                shutterFullyOpen = 0,
                shutterBeginsClosing = 1,
                adaptFixedLengthScenarioFrames = true
            };

            var sensorHandle = DatasetCapture.RegisterSensor(CreateRgbSensorDefinition("cam", "", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0, useAccumulation: true));

            var startTime = firstCaptureFrame * simulationDeltaTime;
            float[] sequenceTimesExpected =
            {
                simulationDeltaTime * (6 + 1) + startTime,
                simulationDeltaTime * ((6 + 1) * 2) + startTime,
                simulationDeltaTime * ((6 + 1) * 3) + startTime,
                simulationDeltaTime * ((6 + 1) * 4) + startTime
            };

            for (var i = 0; i < 3; i++)
            {
                yield return null;
            }

            for (var i = 0; i < firstCaptureFrame; i++)
            {
                //render the non-captured frames before firstCaptureFrame
                yield return null;
            }

            for (var i = 0; i < sequenceTimesExpected.Length; i++)
            {
                var sensorData = m_TestHelper.GetSensorData(sensorHandle);
                var sequenceTimeActual = m_TestHelper.CallSequenceTimeOfNextCapture(sensorData);
                Assert.AreEqual(sequenceTimesExpected[i], sequenceTimeActual, 0.0001f);
                for (int j = 0; j < (6 + 1); j++)
                {
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator SequenceTimeOfNextCapture_ReportsCorrectTime_WithAccumulation_WithoutMotionBlur()
        {
            var firstCaptureFrame = 2;
            var simulationDeltaTime = .4f;

            Time.fixedDeltaTime = 0.02f;
            Time.timeScale = 1;

            PerceptionSettings.instance.accumulationSettings = new AccumulationSettings()
            {
                accumulationSamples = 6,
                shutterInterval = 0,
                shutterFullyOpen = 0,
                shutterBeginsClosing = 1,
                adaptFixedLengthScenarioFrames = true
            };

            var sensorHandle = DatasetCapture.RegisterSensor(CreateRgbSensorDefinition("cam", "", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0, useAccumulation: true));

            var startTime = firstCaptureFrame * simulationDeltaTime;
            float[] sequenceTimesExpected =
            {
                simulationDeltaTime * (6 + 1) + startTime,
                simulationDeltaTime * ((6 + 1) * 2) + startTime,
                simulationDeltaTime * ((6 + 1) * 3) + startTime,
                simulationDeltaTime * ((6 + 1) * 4) + startTime
            };

            for (var i = 0; i < 3; i++)
            {
                yield return null;
            }

            for (var i = 0; i < firstCaptureFrame; i++)
            {
                //render the non-captured frames before firstCaptureFrame
                yield return null;
            }

            for (var i = 0; i < sequenceTimesExpected.Length; i++)
            {
                var sensorData = m_TestHelper.GetSensorData(sensorHandle);
                var sequenceTimeActual = m_TestHelper.CallSequenceTimeOfNextCapture(sensorData);
                Assert.AreEqual(sequenceTimesExpected[i], sequenceTimeActual, 0.0001f);
                for (int j = 0; j < (6 + 1); j++)
                {
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator SequenceTimeOfNextCapture_WithInBetweenFrames_WithAccumulation_ReportsCorrectTime()
        {
            var firstCaptureFrame = 2;
            var simulationDeltaTime = .4f;
            var framesBetweenCaptures = 2;

            Time.fixedDeltaTime = 0.02f;
            Time.timeScale = 1;

            PerceptionSettings.instance.accumulationSettings = new AccumulationSettings()
            {
                accumulationSamples = 5,
                shutterInterval = 1,
                shutterFullyOpen = 0,
                shutterBeginsClosing = 1,
                adaptFixedLengthScenarioFrames = true
            };

            var sensorHandle = DatasetCapture.RegisterSensor(
                CreateRgbSensorDefinition("cam", "", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, framesBetweenCaptures, useAccumulation: true));

            var startingFrame = Time.frameCount;

            var startTime = firstCaptureFrame * simulationDeltaTime;
            var interval = (framesBetweenCaptures) * simulationDeltaTime;
            float[] sequenceTimesExpected =
            {
                startTime + (5 + 1) * simulationDeltaTime,
                interval + startTime + (5 + 1) * 2 * simulationDeltaTime,
                interval * 2 + startTime + (5 + 1) * 3 * simulationDeltaTime,
                interval * 3 + startTime + (5 + 1) * 4 * simulationDeltaTime
            };

            int[] simulationFramesToCheck =
            {
                firstCaptureFrame + (5 + 1) - 1,
                firstCaptureFrame + (framesBetweenCaptures) + (5 + 1) * 2 - 1,
                firstCaptureFrame + (framesBetweenCaptures) * 2 + (5 + 1) * 3 - 1,
                firstCaptureFrame + (framesBetweenCaptures) * 3 + (5 + 1) * 4 - 1,
            };

            int checkedFrame = 0;
            var currentSimFrame = Time.frameCount - startingFrame;
            while (currentSimFrame <= simulationFramesToCheck[simulationFramesToCheck.Length - 1] && checkedFrame < simulationFramesToCheck.Length)
            {
                currentSimFrame = Time.frameCount - startingFrame;
                if (currentSimFrame == simulationFramesToCheck[checkedFrame])
                {
                    var sensorData = m_TestHelper.GetSensorData(sensorHandle);
                    var sequenceTimeActual = m_TestHelper.CallSequenceTimeOfNextCapture(sensorData);
                    Assert.AreEqual(sequenceTimesExpected[checkedFrame], sequenceTimeActual, 0.0001f);
                    checkedFrame++;
                }
                else
                {
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator FramesScheduled_WithTimeScale_WithAccumulation_ResultsInProperDeltaTime()
        {
            var firstCaptureFrame = 2;
            var simulationDeltaTime = 1f;
            var timeScale = 2;
            Time.timeScale = timeScale;

            PerceptionSettings.instance.accumulationSettings = new AccumulationSettings()
            {
                accumulationSamples = 5,
                shutterInterval = 1,
                shutterFullyOpen = 0,
                shutterBeginsClosing = 1,
                adaptFixedLengthScenarioFrames = true
            };

            DatasetCapture.RegisterSensor(
                CreateRgbSensorDefinition("cam", "", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0, useAccumulation: true));

            float[] deltaTimeSamplesExpected =
            {
                timeScale * simulationDeltaTime / (5 + 1),
                timeScale * simulationDeltaTime / (5 + 1),
                timeScale * simulationDeltaTime / (5 + 1),
                timeScale * simulationDeltaTime / (5 + 1)
            };
            var deltaTimeSamples = new float[deltaTimeSamplesExpected.Length];
            for (var i = 0; i < deltaTimeSamples.Length; i++)
            {
                for (var j = 0; j < 5 + 1; j++)
                    yield return null;
                deltaTimeSamples[i] = Time.deltaTime;
            }

            CollectionAssert.AreEqual(deltaTimeSamplesExpected, deltaTimeSamples);
        }

        [UnityTest]
        public IEnumerator ShouldCaptureFlagsMultipleSensorsAndRenderTimesAreCorrectWithAccumulation()
        {
            Time.fixedDeltaTime = 0.02f;
            Time.timeScale = 1;

            PerceptionSettings.instance.accumulationSettings = new AccumulationSettings()
            {
                accumulationSamples = 5,
                shutterInterval = 1,
                shutterFullyOpen = 0,
                shutterBeginsClosing = 1,
                adaptFixedLengthScenarioFrames = true
            };

            var simDeltaTime = 4;

            var firstCaptureFrame1 = 2;
            var framesBetweenCaptures1 = 5;
            var sensor1 = DatasetCapture.RegisterSensor(CreateRgbSensorDefinition("cam1", "", "", firstCaptureFrame1, CaptureTriggerMode.Scheduled, simDeltaTime, framesBetweenCaptures1, useAccumulation: true));

            var firstCaptureFrame2 = 2;
            var framesBetweenCaptures2 = 10;
            var sensor2 = DatasetCapture.RegisterSensor(CreateRgbSensorDefinition("cam2", "", "", firstCaptureFrame2, CaptureTriggerMode.Scheduled, simDeltaTime, framesBetweenCaptures2, useAccumulation: true));

            (float deltaTime, bool sensor1ShouldCapture, bool sensor2ShouldCapture)[] samplesExpected =
            {
                (4, false, false), //Simulation time since sensors created: 4
                (4 / 6f, false, false),
                (4 / 6f, false, false),
                (4 / 6f, false, false),
                (4 / 6f, false, false),
                (4 / 6f, true, true),
                (4 / 6f, false, false),
                (4, false, false),
                (4, false, false),
                (4, false, false),
                (4, false, false),
                (4, false, false),
                (4 / 6f, false, false),
                (4 / 6f, false, false),
                (4 / 6f, false, false),
                (4 / 6f, false, false),
                (4 / 6f, true, false),
                (4 / 6f, false, false),
                (4, false, false),
            };

            for (var i = 0; i < 3; i++)
            {
                yield return null;
            }
            var samplesActual = new(float deltaTime, bool sensor1ShouldCapture, bool sensor2ShouldCapture)[samplesExpected.Length];
            for (int i = 0; i < samplesExpected.Length; i++)
            {
                samplesActual[i] = (Time.deltaTime, sensor1.ShouldCaptureThisFrame, sensor2.ShouldCaptureThisFrame);
                yield return null;
            }

            CollectionAssert.AreEqual(samplesExpected, samplesActual);
        }

        [UnityTest]
        [TestCase(5, 1, 6, 12, 18, 24, 30, ExpectedResult = (IEnumerator)null)]
        [TestCase(5, 10, 50, 560, 620, 680, 740, ExpectedResult = (IEnumerator)null)]
        [TestCase(5, 55, 0, 330, 660, 990, 1320, ExpectedResult = (IEnumerator)null)]
        public IEnumerator SequenceTimeOfNextCapture_ReportsCorrectTime_WithAccumulationVariedDeltaTimesAndStartFrames(int accumulationSamples, float simulationDeltaTime, int firstCaptureFrame, float firstCaptureTime, float secondCaptureTime, float thirdCaptureTime, float fourthCaptureTime)
        {
            Time.fixedDeltaTime = 0.02f;
            Time.timeScale = 1;

            PerceptionSettings.instance.accumulationSettings = new AccumulationSettings()
            {
                accumulationSamples = accumulationSamples,
                shutterInterval = 1,
                shutterFullyOpen = 0,
                shutterBeginsClosing = 1,
                adaptFixedLengthScenarioFrames = true
            };

            var sensorHandle = DatasetCapture.RegisterSensor(CreateRgbSensorDefinition("cam", "", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0, useAccumulation: true));

            float[] sequenceTimesExpected =
            {
                firstCaptureTime,
                secondCaptureTime,
                thirdCaptureTime,
                fourthCaptureTime
            };

            for (var i = 0; i < 3; i++)
            {
                yield return null;
            }

            for (var i = 0; i < firstCaptureFrame; i++)
            {
                //render the non-captured frames before firstCaptureFrame
                yield return null;
            }

            for (var i = 0; i < sequenceTimesExpected.Length; i++)
            {
                var sensorData = m_TestHelper.GetSensorData(sensorHandle);
                var sequenceTimeActual = m_TestHelper.CallSequenceTimeOfNextCapture(sensorData);
                Assert.AreEqual(sequenceTimesExpected[i], sequenceTimeActual, 0.0001f);
                for (int j = 0; j < (5 + 1); j++)
                {
                    yield return null;
                }
            }
        }

#endif

        [UnityTest]
        public IEnumerator SequenceTimeOfNextCapture_ReportsCorrectTime()
        {
            var firstCaptureFrame = 2;
            var simulationDeltaTime = .4f;

            var sensorHandle = DatasetCapture.RegisterSensor(
                CreateSensorDefinition("cam", "", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0));

            var startTime = firstCaptureFrame * simulationDeltaTime;
            float[] sequenceTimesExpected =
            {
                startTime,
                simulationDeltaTime + startTime,
                simulationDeltaTime * 2 + startTime,
                simulationDeltaTime * 3 + startTime
            };

            for (var i = 0; i < firstCaptureFrame; i++)
            {
                //render the non-captured frames before firstCaptureFrame
                yield return null;
            }

            for (var i = 0; i < sequenceTimesExpected.Length; i++)
            {
                var sensorData = m_TestHelper.GetSensorData(sensorHandle);
                var sequenceTimeActual = m_TestHelper.CallSequenceTimeOfNextCapture(sensorData);
                Assert.AreEqual(sequenceTimesExpected[i], sequenceTimeActual, 0.0001f);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SequenceTimeOfNextCapture_WithInBetweenFrames_ReportsCorrectTime()
        {
            var firstCaptureFrame = 2;
            var simulationDeltaTime = .4f;
            var framesBetweenCaptures = 2;

            var sensorHandle = DatasetCapture.RegisterSensor(
                CreateSensorDefinition("cam", "", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, framesBetweenCaptures));

            var startingFrame = Time.frameCount;

            var startTime = firstCaptureFrame * simulationDeltaTime;
            var interval = (framesBetweenCaptures + 1) * simulationDeltaTime;
            float[] sequenceTimesExpected =
            {
                startTime,
                interval + startTime,
                interval * 2 + startTime,
                interval * 3 + startTime
            };

            int[] simulationFramesToCheck =
            {
                firstCaptureFrame,
                firstCaptureFrame + (framesBetweenCaptures + 1),
                firstCaptureFrame + (framesBetweenCaptures + 1) * 2,
                firstCaptureFrame + (framesBetweenCaptures + 1) * 3,
            };

            int checkedFrame = 0;
            var currentSimFrame = Time.frameCount - startingFrame;
            while (currentSimFrame <= simulationFramesToCheck[simulationFramesToCheck.Length - 1] && checkedFrame < simulationFramesToCheck.Length)
            {
                currentSimFrame = Time.frameCount - startingFrame;
                if (currentSimFrame == simulationFramesToCheck[checkedFrame])
                {
                    var sensorData = m_TestHelper.GetSensorData(sensorHandle);
                    var sequenceTimeActual = m_TestHelper.CallSequenceTimeOfNextCapture(sensorData);
                    Assert.AreEqual(sequenceTimesExpected[checkedFrame], sequenceTimeActual, 0.0001f);
                    checkedFrame++;
                }
                else
                {
                    yield return null;
                }
            }
        }

        [UnityTest]
        public IEnumerator FramesScheduledBySensorConfig()
        {
            var firstCaptureFrame = 2;
            var simulationDeltaTime = .4f;

            DatasetCapture.RegisterSensor(
                CreateSensorDefinition("cam", "", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0));

            float[] deltaTimeSamplesExpected =
            {
                simulationDeltaTime,
                simulationDeltaTime,
                simulationDeltaTime,
                simulationDeltaTime
            };
            float[] deltaTimeSamples = new float[deltaTimeSamplesExpected.Length];
            for (int i = 0; i < deltaTimeSamples.Length; i++)
            {
                yield return null;
                Assert.AreEqual(deltaTimeSamplesExpected[i], Time.deltaTime, 0.0001f);
            }
        }

        [UnityTest]
        public IEnumerator FramesScheduled_WithTimeScale_ResultsInProperDeltaTime()
        {
            var firstCaptureFrame = 2;
            var simulationDeltaTime = 1f;
            var timeScale = 2;
            Time.timeScale = timeScale;

            DatasetCapture.RegisterSensor(
                CreateSensorDefinition("cam", "", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0));

            float[] deltaTimeSamplesExpected =
            {
                timeScale * simulationDeltaTime,
                timeScale * simulationDeltaTime,
                timeScale * simulationDeltaTime,
                timeScale * simulationDeltaTime
            };
            float[] deltaTimeSamples = new float[deltaTimeSamplesExpected.Length];
            for (int i = 0; i < deltaTimeSamples.Length; i++)
            {
                yield return null;
                Assert.AreEqual(deltaTimeSamplesExpected[i], Time.deltaTime, 0.0001f);
            }
        }

        [UnityTest]
        public IEnumerator ChangingTimeScale_CausesDebugError()
        {
            DatasetCapture.RegisterSensor(CreateSensorDefinition("cam", "", "", 2, CaptureTriggerMode.Scheduled, 1, 0));

            yield return null;
            Time.timeScale = 5;
            yield return null;
            LogAssert.Expect(LogType.Error, new Regex("Time\\.timeScale may not change mid-sequence.*"));
        }

        [UnityTest]
        public IEnumerator ChangingTimeScale_DuringStartNewSequence_Succeeds()
        {
            DatasetCapture.RegisterSensor(CreateSensorDefinition("cam", "", "", 2, CaptureTriggerMode.Scheduled, 1, 0));

            yield return null;
            Time.timeScale = 1;
            DatasetCapture.StartNewSequence();
            yield return null;
        }

        [Ignore("Changing timeScale mid-sequence is not supported")]
        [UnityTest]
        public IEnumerator FramesScheduled_WithChangingTimeScale_ResultsInProperDeltaTime()
        {
            var firstCaptureFrame = 2;
            var simulationDeltaTime = 1f;
            float[] newTimeScalesPerFrame =
            {
                2f,
                10f,
                .01f,
                1f
            };

            DatasetCapture.RegisterSensor(CreateSensorDefinition("cam", "", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, 1, 0));

            float[] deltaTimeSamplesExpected =
            {
                newTimeScalesPerFrame[0] * simulationDeltaTime,
                newTimeScalesPerFrame[1] * simulationDeltaTime,
                newTimeScalesPerFrame[2] * simulationDeltaTime,
                newTimeScalesPerFrame[3] * simulationDeltaTime
            };
            float[] deltaTimeSamples = new float[deltaTimeSamplesExpected.Length];
            for (int i = 0; i < deltaTimeSamples.Length; i++)
            {
                Time.timeScale = newTimeScalesPerFrame[i];
                yield return null;
                Assert.AreEqual(deltaTimeSamplesExpected[i], Time.deltaTime, 0.0001f);
            }
        }

        [UnityTest]
        public IEnumerator ResetSimulation_ResetsCaptureDeltaTime()
        {
            DatasetCapture.RegisterSensor(CreateSensorDefinition("cam", "", "", 0, CaptureTriggerMode.Scheduled, 5, 0));
            yield return null;
            Assert.AreEqual(5, Time.captureDeltaTime);
            DatasetCapture.ResetSimulation();
            Assert.AreEqual(0, Time.captureDeltaTime);
        }

        [UnityTest]
        public IEnumerator ShouldCaptureFlagsAndRenderTimesAreCorrectWithMultipleSensors()
        {
            var firstCaptureFrame1 = 2;
            var simDeltaTime1 = 4;
            var framesBetweenCaptures1 = 2;
            var sensor1 = DatasetCapture.RegisterSensor(CreateSensorDefinition("cam1", "", "", firstCaptureFrame1, CaptureTriggerMode.Scheduled, simDeltaTime1, framesBetweenCaptures1));

            var firstCaptureFrame2 = 1;
            var simDeltaTime2 = 6;
            var framesBetweenCaptures2 = 1;
            var sensor2 = DatasetCapture.RegisterSensor(CreateSensorDefinition("cam2", "", "", firstCaptureFrame2, CaptureTriggerMode.Scheduled, simDeltaTime2, framesBetweenCaptures2));

            //Third sensor is a manually triggered one. All it does in this test is affect delta times.
            var simDeltaTime3 = 5;
            var sensor3 = DatasetCapture.RegisterSensor(CreateSensorDefinition("cam3", "", "", 0, CaptureTriggerMode.Manual, simDeltaTime3, 0, true));

            (float deltaTime, bool sensor1ShouldCapture, bool sensor2ShouldCapture, bool sensor3ShouldCapture)[] samplesExpected =
            {
                (4, false, false, false), //Simulation time since sensors created: 4
                (1, false, false, false), //5
                (1, false, true, false), //6
                (2, true, false, false), //8
                (2, false, false, false), //10
                (2, false, false, false), //12
                (3, false, false, false), //15
                (1, false, false, false), //16
                (2, false, true, false), //18
                (2, true, false, false), //20
                (4, false, false, false), //24
                (1, false, false, false), //25
            };
            var samplesActual = new(float deltaTime, bool sensor1ShouldCapture, bool sensor2ShouldCapture, bool sensor3ShouldCapture)[samplesExpected.Length];
            for (int i = 0; i < samplesActual.Length; i++)
            {
                yield return null;
                samplesActual[i] = (Time.deltaTime, sensor1.ShouldCaptureThisFrame, sensor2.ShouldCaptureThisFrame, sensor3.ShouldCaptureThisFrame);
            }

            CollectionAssert.AreEqual(samplesExpected, samplesActual);
        }

        [UnityTest]
        [TestCase(1, 0, 0, 1, 2, 3, ExpectedResult = (IEnumerator)null)]
        [TestCase(10, 5, 50, 60, 70, 80, ExpectedResult = (IEnumerator)null)]
        [TestCase(55, 0, 0, 55, 110, 165, ExpectedResult = (IEnumerator)null)]
        [TestCase(235, 10, 2350, 2585, 2820, 3055, ExpectedResult = (IEnumerator)null)]
        public IEnumerator SequenceTimeOfNextCapture_ReportsCorrectTime_VariedDeltaTimesAndStartFrames(float simulationDeltaTime, int firstCaptureFrame, float firstCaptureTime, float secondCaptureTime, float thirdCaptureTime, float fourthCaptureTime)
        {
            var sensorHandle = DatasetCapture.RegisterSensor(CreateSensorDefinition("cam", "", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0));

            float[] sequenceTimesExpected =
            {
                firstCaptureTime,
                secondCaptureTime,
                thirdCaptureTime,
                fourthCaptureTime
            };

            for (var i = 0; i < firstCaptureFrame; i++)
            {
                //render the non-captured frames before firstCaptureFrame
                yield return null;
            }

            for (var i = 0; i < sequenceTimesExpected.Length; i++)
            {
                var sensorData = m_TestHelper.GetSensorData(sensorHandle);
                var sequenceTimeActual = m_TestHelper.CallSequenceTimeOfNextCapture(sensorData);
                Assert.AreEqual(sequenceTimesExpected[i], sequenceTimeActual, 0.0001f);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SequenceTimeOfManualCapture_ReportsCorrectTime_ManualSensorDoesNotAffectTimings()
        {
            var sensorHandle = DatasetCapture.RegisterSensor(
                CreateSensorDefinition("cam", "", "", 0, CaptureTriggerMode.Manual, 0, 0, false));

            var framesToCaptureOn = new List<int>();

            var startFrame = Time.frameCount;
            var startTime = Time.time;

            while (framesToCaptureOn.Count < 10)
            {
                var randomFrame = Random.Range(startFrame, startFrame + 100);
                if (!framesToCaptureOn.Contains(randomFrame))
                    framesToCaptureOn.Add(randomFrame);
            }

            framesToCaptureOn.Sort();

            var frameIndex = 0;
            for (var i = 0; i < framesToCaptureOn.Max(); i++)
            {
                if (frameIndex == framesToCaptureOn.Count)
                    break;

                if (Time.frameCount == framesToCaptureOn[frameIndex])
                {
                    frameIndex++;
                    sensorHandle.RequestCapture();
                    var sensorData = m_TestHelper.GetSensorData(sensorHandle);
                    var sequenceTimeActual = m_TestHelper.CallSequenceTimeOfNextCapture(sensorData);
                    if (Time.frameCount == startFrame)
                    {
                        // SequenceTimeOfNextCapture() should return an invalid value (float.MaxValue) if the first
                        // capture has not happened yet.
                        Assert.AreEqual(float.MaxValue, sequenceTimeActual, 0.0001f);
                    }
                    else
                    {
                        var elapsed = Time.time - startTime;
                        Assert.AreEqual(elapsed, sequenceTimeActual, 0.0001f);
                    }
                }

                if (Time.frameCount > 1000)
                {
                    Debug.Log("Pulling the eject handle");
                    yield break;
                }

                yield return null;
            }

            Assert.AreEqual(frameIndex, framesToCaptureOn.Count, 0.0001f);
        }

        [UnityTest]
        public IEnumerator SequenceTimeOfManualCapture_ReportsCorrectTime_ManualSensorAffectsTimings()
        {
            var simulationDeltaTime = 0.05f;
            var sensorHandle = DatasetCapture.RegisterSensor(CreateSensorDefinition("cam", "", "", 0, CaptureTriggerMode.Manual, simulationDeltaTime, 0, true));

            var framesToCaptureOn = new List<int>()
            {
                Time.frameCount + 3,
                Time.frameCount + 5,
                Time.frameCount + 6,
                Time.frameCount + 10,
                Time.frameCount + 15,
                Time.frameCount + 25,
            };

            framesToCaptureOn.Sort();

            float[] sequenceTimesExpected = new float[framesToCaptureOn.Count];

            for (int i = 0; i < sequenceTimesExpected.Length; i++)
            {
                sequenceTimesExpected[i] = (framesToCaptureOn[i] - Time.frameCount) * simulationDeltaTime;
            }

            var frameIndex = 0;
            for (var i = 0; i < framesToCaptureOn.Max(); i++)
            {
                if (frameIndex == framesToCaptureOn.Count)
                    break;

                if (Time.frameCount == framesToCaptureOn[frameIndex])
                {
                    sensorHandle.RequestCapture();
                    var sensorData = m_TestHelper.GetSensorData(sensorHandle);
                    var sequenceTimeActual = m_TestHelper.CallSequenceTimeOfNextCapture(sensorData);
                    Assert.AreEqual(sequenceTimesExpected[frameIndex], sequenceTimeActual, 0.0001f);
                    frameIndex++;
                }

                if (Time.frameCount > 1000)
                {
                    Debug.Log("Pulling the eject handle");
                    yield break;
                }

                yield return null;
            }
            Assert.AreEqual(frameIndex, framesToCaptureOn.Count, 0.0001f);
        }
    }
}
