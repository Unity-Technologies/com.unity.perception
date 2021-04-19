using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace GroundTruthTests
{
    // Provides accessors and invocation methods for members of SimulationState that would otherwise be in-accessible
    // due to protection level - use only when testing protected logic is critical
    class SimulationStateTestHelper
    {
        SimulationState m_State => DatasetCapture.SimulationState;
        Dictionary<SensorHandle, SimulationState.SensorData> m_SensorsReference;
        MethodInfo m_SequenceTimeOfNextCaptureMethod;

        internal SimulationStateTestHelper()
        {
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
            m_SequenceTimeOfNextCaptureMethod = m_State.GetType().GetMethod("GetSequenceTimeOfNextCapture", bindingFlags);
            Debug.Assert(m_SequenceTimeOfNextCaptureMethod != null, "Couldn't find sequence time method.");
            var sensorsField = m_State.GetType().GetField("m_Sensors", bindingFlags);
            Debug.Assert(sensorsField != null, "Couldn't find internal sensors field");
            m_SensorsReference = (Dictionary<SensorHandle, SimulationState.SensorData>)(sensorsField.GetValue(m_State));
            Debug.Assert(m_SensorsReference != null, "Couldn't cast sensor field to dictionary");
        }

        internal float CallSequenceTimeOfNextCapture(SimulationState.SensorData sensorData)
        {
            return (float)m_SequenceTimeOfNextCaptureMethod.Invoke(m_State, new object[] { sensorData });
        }

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

        [UnityTest]
        public IEnumerator SequenceTimeOfNextCapture_ReportsCorrectTime()
        {
            var ego = DatasetCapture.RegisterEgo("ego");
            var firstCaptureFrame = 2f;
            var simulationDeltaTime = .4f;
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "cam", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0);

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
            var ego = DatasetCapture.RegisterEgo("ego");
            var firstCaptureFrame = 2;
            var simulationDeltaTime = .4f;
            var framesBetweenCaptures = 2;
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "cam", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, framesBetweenCaptures);
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
            var ego = DatasetCapture.RegisterEgo("ego");
            var firstCaptureFrame = 2f;
            var simulationDeltaTime = .4f;
            DatasetCapture.RegisterSensor(ego, "cam", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0);

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
            var ego = DatasetCapture.RegisterEgo("ego");
            var firstCaptureFrame = 2f;
            var simulationDeltaTime = 1f;

            var timeScale = 2;
            Time.timeScale = timeScale;
            DatasetCapture.RegisterSensor(ego, "cam", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0);

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
            var ego = DatasetCapture.RegisterEgo("ego");
            DatasetCapture.RegisterSensor(ego, "cam", "", 2f, CaptureTriggerMode.Scheduled, 1, 0);

            yield return null;
            Time.timeScale = 5;
            yield return null;
            LogAssert.Expect(LogType.Error, new Regex("Time\\.timeScale may not change mid-sequence.*"));
        }

        [UnityTest]
        public IEnumerator ChangingTimeScale_DuringStartNewSequence_Succeeds()
        {
            var ego = DatasetCapture.RegisterEgo("ego");
            DatasetCapture.RegisterSensor(ego, "cam", "", 2f, CaptureTriggerMode.Scheduled, 1, 0);

            yield return null;
            Time.timeScale = 1;
            DatasetCapture.StartNewSequence();
            yield return null;
        }

        [Ignore("Changing timeScale mid-sequence is not supported")]
        [UnityTest]
        public IEnumerator FramesScheduled_WithChangingTimeScale_ResultsInProperDeltaTime()
        {
            var ego = DatasetCapture.RegisterEgo("ego");
            var firstCaptureFrame = 2f;
            var simulationDeltaTime = 1f;
            float[] newTimeScalesPerFrame =
            {
                2f,
                10f,
                .01f,
                1f
            };
            DatasetCapture.RegisterSensor(ego, "cam", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, 1, 0);

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
            var ego = DatasetCapture.RegisterEgo("ego");
            DatasetCapture.RegisterSensor(ego, "cam", "", 0, CaptureTriggerMode.Scheduled, 5, 0);
            yield return null;
            Assert.AreEqual(5, Time.captureDeltaTime);
            DatasetCapture.ResetSimulation();
            Assert.AreEqual(0, Time.captureDeltaTime);
        }

        [UnityTest]
        public IEnumerator ShouldCaptureFlagsAndRenderTimesAreCorrectWithMultipleSensors()
        {
            var ego = DatasetCapture.RegisterEgo("ego");
            var firstCaptureFrame1 = 2;
            var simDeltaTime1 = 4;
            var framesBetweenCaptures1 = 2;
            var sensor1 = DatasetCapture.RegisterSensor(ego, "cam", "1", firstCaptureFrame1, CaptureTriggerMode.Scheduled, simDeltaTime1, framesBetweenCaptures1);

            var firstCaptureFrame2 = 1;
            var simDeltaTime2 = 6;
            var framesBetweenCaptures2 = 1;
            var sensor2 = DatasetCapture.RegisterSensor(ego, "cam", "2", firstCaptureFrame2, CaptureTriggerMode.Scheduled, simDeltaTime2, framesBetweenCaptures2);

            //Third sensor is a manually triggered one. All it does in this test is affect delta times.
            var simDeltaTime3 = 5;
            var sensor3 = DatasetCapture.RegisterSensor(ego, "cam", "3", 0, CaptureTriggerMode.Manual, simDeltaTime3, 0, true);

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
            var samplesActual = new (float deltaTime, bool sensor1ShouldCapture, bool sensor2ShouldCapture, bool sensor3ShouldCapture)[samplesExpected.Length];
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
            var ego = DatasetCapture.RegisterEgo("ego");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "cam", "", firstCaptureFrame, CaptureTriggerMode.Scheduled, simulationDeltaTime, 0);

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
            var ego = DatasetCapture.RegisterEgo("ego");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "cam", "", 0, CaptureTriggerMode.Manual, 0, 0, false);

            var framesToCaptureOn = new List<int>();

            var startFrame = Time.frameCount;
            var startTime = Time.time;

            while (framesToCaptureOn.Count < 10)
            {
                var randomFrame = Random.Range(startFrame, startFrame + 100);
                if(!framesToCaptureOn.Contains(randomFrame))
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
                    var elapsed = Time.time - startTime;
                    Assert.AreEqual(elapsed, sequenceTimeActual, 0.0001f);
                }
                yield return null;
            }

            Assert.AreEqual(frameIndex, framesToCaptureOn.Count, 0.0001f);
        }

        [UnityTest]
        public IEnumerator SequenceTimeOfManualCapture_ReportsCorrectTime_ManualSensorAffectsTimings()
        {
            var ego = DatasetCapture.RegisterEgo("ego");
            var simulationDeltaTime = 0.05f;
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "cam", "", 0, CaptureTriggerMode.Manual, simulationDeltaTime, 0, true);

            var framesToCaptureOn = new List<int>();

            var startFrame = Time.frameCount;
            var startTime = Time.time;

            while (framesToCaptureOn.Count < 10)
            {
                var randomFrame = Random.Range(startFrame, startFrame + 100);
                if(!framesToCaptureOn.Contains(randomFrame))
                    framesToCaptureOn.Add(randomFrame);
            }

            framesToCaptureOn.Sort();

            float[] sequenceTimesExpected = new float[framesToCaptureOn.Count];

            for (int i = 0; i < sequenceTimesExpected.Length; i++)
            {
                sequenceTimesExpected[i] = (framesToCaptureOn[i] - startFrame) * simulationDeltaTime;
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
                yield return null;
            }
            Assert.AreEqual(frameIndex, framesToCaptureOn.Count, 0.0001f);
        }
    }
}
