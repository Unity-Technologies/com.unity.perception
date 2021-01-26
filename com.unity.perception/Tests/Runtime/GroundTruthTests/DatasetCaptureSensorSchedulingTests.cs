using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;

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
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "cam", "", firstCaptureFrame, PerceptionCamera.CaptureTriggerMode.Scheduled, simulationDeltaTime, 0);

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
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "cam", "", firstCaptureFrame, PerceptionCamera.CaptureTriggerMode.Scheduled, simulationDeltaTime, framesBetweenCaptures);
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
            while (currentSimFrame <= simulationFramesToCheck[simulationFramesToCheck.Length-1] && checkedFrame < simulationFramesToCheck.Length)
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
            DatasetCapture.RegisterSensor(ego, "cam", "", firstCaptureFrame, PerceptionCamera.CaptureTriggerMode.Scheduled, simulationDeltaTime, 0);

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
            DatasetCapture.RegisterSensor(ego, "cam", "", firstCaptureFrame, PerceptionCamera.CaptureTriggerMode.Scheduled, simulationDeltaTime, 0);

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
            DatasetCapture.RegisterSensor(ego, "cam", "", 2f, PerceptionCamera.CaptureTriggerMode.Scheduled, 1, 0);

            yield return null;
            Time.timeScale = 5;
            yield return null;
            LogAssert.Expect(LogType.Error, new Regex("Time\\.timeScale may not change mid-sequence.*"));
        }

        [UnityTest]
        public IEnumerator ChangingTimeScale_DuringStartNewSequence_Succeeds()
        {
            var ego = DatasetCapture.RegisterEgo("ego");
            DatasetCapture.RegisterSensor(ego, "cam", "", 2f, PerceptionCamera.CaptureTriggerMode.Scheduled, 1, 0);

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
            DatasetCapture.RegisterSensor(ego, "cam", "", firstCaptureFrame, PerceptionCamera.CaptureTriggerMode.Scheduled, 1, 0);

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
            DatasetCapture.RegisterSensor(ego, "cam", "", 0, PerceptionCamera.CaptureTriggerMode.Scheduled, 5, 0);
            yield return null;
            Assert.AreEqual(5, Time.captureDeltaTime);
            DatasetCapture.ResetSimulation();
            Assert.AreEqual(0, Time.captureDeltaTime);
        }

        [UnityTest]
        public IEnumerator ShouldCaptureThisFrame_ReturnsTrueOnProperFrames()
        {
            var ego = DatasetCapture.RegisterEgo("ego");
            var firstCaptureFrame1 = 2;
            var simDeltaTime1 = 4;
            var framesBetweenCaptures1 = 2;
            var sensor1 = DatasetCapture.RegisterSensor(ego, "cam", "1", firstCaptureFrame1, PerceptionCamera.CaptureTriggerMode.Scheduled, simDeltaTime1, framesBetweenCaptures1);

            var firstCaptureFrame2 = 1;
            var simDeltaTime2 = 6;
            var framesBetweenCaptures2 = 1;
            var sensor2 = DatasetCapture.RegisterSensor(ego, "cam", "2", firstCaptureFrame2, PerceptionCamera.CaptureTriggerMode.Scheduled, simDeltaTime2, framesBetweenCaptures2);


            (float deltaTime, bool sensor1ShouldCapture, bool sensor2ShouldCapture)[] samplesExpected =
            {
                (4, false, false),
                (2, false, true),
                (2, true, false),
                (4, false, false),
                (4, false, false),
                (2, false, true),
                (2, true, false)
            };
            var samplesActual = new(float deltaTime, bool sensor1ShouldCapture, bool sensor2ShouldCapture)[samplesExpected.Length];
            for (int i = 0; i < samplesActual.Length; i++)
            {
                yield return null;
                samplesActual[i] = (Time.deltaTime, sensor1.ShouldCaptureThisFrame, sensor2.ShouldCaptureThisFrame);
            }

            CollectionAssert.AreEqual(samplesExpected, samplesActual);
        }

        [Test]
        public void Enabled_StartsTrue()
        {
            var sensor1 = DatasetCapture.RegisterSensor(DatasetCapture.RegisterEgo(""), "cam", "1", 1, PerceptionCamera.CaptureTriggerMode.Scheduled, 1, 0);
            Assert.IsTrue(sensor1.Enabled);
        }
    }
}
