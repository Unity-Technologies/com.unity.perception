using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;

namespace GroundTruthTests
{
    [TestFixture]
    [Serializable]
    public class DatasetCaptureEditorTests
    {
        [SerializeField]
        string expectedDatasetPath;

        class TestDef : AnnotationDefinition
        {
            public override string modelType => "TestDef";
            public override string description => "description";
            public TestDef() : base("annotation_test") {}
        }

        [Test]
        public void RegisterAnnotationDefinition_InEditMode_Throws()
        {
            var def = new TestDef();
            Assert.Throws<InvalidOperationException>(() => DatasetCapture.RegisterAnnotationDefinition(def));
        }

        [Test]
        public void RegisterMetricDefinition_InEditMode_Throws()
        {
            var def = new MetricDefinition("", "");
            Assert.Throws<InvalidOperationException>(() => DatasetCapture.RegisterMetric(def));
        }

        static RgbSensor CreateMocRgbCapture(RgbSensorDefinition def)
        {
            var position = new float3(.2f, 1.1f, .3f);
            var rotation = new Quaternion(.3f, .2f, .1f, .5f);
            var velocity = new Vector3(.1f, .2f, .3f);
            var matrix = new float3x3(.1f, .2f, .3f, 1f, 2f, 3f, 10f, 20f, 30f);

            return new RgbSensor(def, position, rotation, velocity, Vector3.zero)
            {
                matrix = matrix
            };
        }

        static void AssertAreEqual(Vector3 first, Vector3 second)
        {
            Assert.AreEqual(first.x, second.x);
            Assert.AreEqual(first.y, second.y);
            Assert.AreEqual(first.z, second.z);
        }

        static void AssertAreEqual(Quaternion first, Quaternion second)
        {
            Assert.AreEqual(first.x, second.x);
            Assert.AreEqual(first.y, second.y);
            Assert.AreEqual(first.z, second.z);
            Assert.AreEqual(first.w, second.w);
        }

        static void AssertAreEqual(RgbSensor first, RgbSensor second)
        {
            Assert.NotNull(first);
            Assert.NotNull(second);

            Assert.AreEqual(first.id, second.id);
            Assert.AreEqual(first.modelType, second.modelType);
            Assert.AreEqual(first.description, second.description);
            AssertAreEqual(first.position, second.position);
            AssertAreEqual(first.rotation, second.rotation);
            AssertAreEqual(first.velocity, second.velocity);
            AssertAreEqual(first.acceleration, second.acceleration);
            Assert.AreEqual(first.matrix, second.matrix);
            Assert.AreEqual(first.imageEncodingFormat, second.imageEncodingFormat);
            AssertAreEqual(first.dimension, second.dimension);
            Assert.Null(first.buffer);
            Assert.Null(second.buffer);
        }

        [UnityTest]
        public IEnumerator SimpleData_GeneratesFullDataset_OnExitPlaymode()
        {
            yield return new EnterPlayMode();

            var collector = new TestCollectorEndpoint();
            DatasetCapture.OverrideEndpoint(collector);

            DatasetCapture.ResetSimulation();

            var sensorDefinition = new RgbSensorDefinition("camera", "camera", "")
            {
                firstCaptureFrame = 0,
                captureTriggerMode = CaptureTriggerMode.Scheduled,
                simulationDeltaTime = 0.1f,
                framesBetweenCaptures = 0,
                manualSensorsAffectTiming = false
            };
            var handle = DatasetCapture.RegisterSensor(sensorDefinition);
            var sensorData = CreateMocRgbCapture(sensorDefinition);
            handle.ReportSensor(sensorData);

            yield return new ExitPlayMode();

            Assert.IsFalse(handle.IsValid);

            Assert.NotNull(collector.currentRun);
            Assert.IsTrue(collector.currentRun.frames.Any());
            Assert.NotNull(collector.currentRun.frames.First().sensors);
            Assert.AreEqual(collector.currentRun.frames.First().sensors.Count(), 1);

            var rgb = collector.currentRun.frames.First().sensors.First() as RgbSensor;
            Assert.NotNull(rgb);

            AssertAreEqual(rgb, sensorData);
        }

        [UnityTest]
        public IEnumerator StepFunction_OverridesSimulationDeltaTime_AndRunsSensors()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            yield return new EnterPlayMode();

            var collector = new TestCollectorEndpoint();
            DatasetCapture.OverrideEndpoint(collector);

            DatasetCapture.ResetSimulation();

            var sensorDefinition = new SensorDefinition("camera", "camera", "")
            {
                firstCaptureFrame = 0,
                captureTriggerMode = CaptureTriggerMode.Scheduled,
                simulationDeltaTime = 2f,
                framesBetweenCaptures = 0,
                manualSensorsAffectTiming = false
            };
            var handle = DatasetCapture.RegisterSensor(sensorDefinition);

            yield return null;
            var timeBeforeStep = Time.time;
            EditorApplication.isPaused = true;
            EditorApplication.Step();
            Assert.True(Time.time - timeBeforeStep < .3f);
            Assert.True(handle.ShouldCaptureThisFrame);
            yield return new ExitPlayMode();
        }
    }
}
