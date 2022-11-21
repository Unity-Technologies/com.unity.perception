using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.Settings;
using UnityEngine.TestTools;
// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedField.Local

namespace GroundTruthTests
{
    public static class AssertUtils
    {
        public static void AreEqual(Vector2 first, Vector2 second)
        {
            Assert.AreEqual(first.x, second.x);
            Assert.AreEqual(first.y, second.y);
        }

        public static void AreEqual(Vector3 first, Vector3 second)
        {
            Assert.AreEqual(first.x, second.x);
            Assert.AreEqual(first.y, second.y);
            Assert.AreEqual(first.z, second.z);
        }

        public static void AreEqual(Quaternion first, Quaternion second)
        {
            Assert.AreEqual(first.x, second.x);
            Assert.AreEqual(first.y, second.y);
            Assert.AreEqual(first.z, second.z);
            Assert.AreEqual(first.w, second.w);
        }

        public static void AreEqual(float3x3 first, float3x3 second)
        {
            for (var i = 0; i < 9; i++)
            {
                Assert.AreEqual(first[i], second[i]);
            }
        }

        public static void AreEqual(RgbSensor first, RgbSensor second)
        {
            Assert.NotNull(first);
            Assert.NotNull(second);

            Assert.AreEqual(first.id, second.id);
            Assert.AreEqual(first.modelType, second.modelType);
            Assert.AreEqual(first.description, second.description);
            AreEqual(first.position, second.position);
            AreEqual(first.rotation, second.rotation);
            AreEqual(first.velocity, second.velocity);
            AreEqual(first.acceleration, second.acceleration);
            Assert.AreEqual(first.matrix, second.matrix);
            Assert.AreEqual(first.imageEncodingFormat, second.imageEncodingFormat);
            AreEqual(first.dimension, second.dimension);
            Assert.Null(first.buffer);
            Assert.Null(second.buffer);
        }
    }

    [TestFixture]
    public class DatasetCaptureTests
    {
        [UnityTest]
        public IEnumerator RegisterSensor_ReportsProperJson()
        {
            const string id = "camera";
            const string modality = "camera";
            const string def = "Cam (FL2-14S3M-C)";
            const int firstFrame = 1;
            const CaptureTriggerMode mode = CaptureTriggerMode.Scheduled;
            const int delta = 1;
            const int framesBetween = 0;

            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor(id, modality, def, firstFrame, mode, delta, framesBetween);
            Assert.IsTrue(sensorHandle.IsValid);

            yield return null;
            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            // Check metadata
            var meta = collector.currentRun.metadata as SimulationMetadata;
            Assert.NotNull(meta);
            Assert.AreEqual(meta.perceptionVersion, DatasetCapture.perceptionVersion);
            Assert.AreEqual(meta.unityVersion, Application.unityVersion);

            // Check sensor data
            Assert.AreEqual(collector.sensors.Count, 1);
            var sensor = collector.sensors.First();
            Assert.NotNull(sensor);
            Assert.AreEqual(sensor.id, id);
            Assert.AreEqual(sensor.modality, modality);
            Assert.AreEqual(sensor.description, def);
            Assert.AreEqual(sensor.firstCaptureFrame, firstFrame);
            Assert.AreEqual(sensor.captureTriggerMode, mode);
            Assert.AreEqual(sensor.simulationDeltaTime, delta);
            Assert.AreEqual(sensor.framesBetweenCaptures, framesBetween);
        }

        static RgbSensor CreateMocRgbCapture(RgbSensorDefinition sensorDef)
        {
            var position = new float3(.2f, 1.1f, .3f);
            var rotation = new Quaternion(.3f, .2f, .1f, .5f);
            var velocity = new Vector3(.1f, .2f, .3f);
            var matrix = new float3x3(.1f, .2f, .3f, 1f, 2f, 3f, 10f, 20f, 30f);

            return new RgbSensor(sensorDef, position, rotation, velocity, Vector3.zero)
            {
                matrix = matrix
            };
        }

        [Test]
        public void ResetSimulation_WithUnreportedCaptureAsync_LogsError()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var f = sensorHandle.ReportSensorAsync();

            DatasetCapture.ResetSimulation();

            LogAssert.Expect(LogType.Error, new Regex("Simulation ended with pending frame .*"));
        }

        [UnityTest]
        public IEnumerator ReportCaptureAsync_DoesNotError()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var f = sensorHandle.ReportSensorAsync();

            yield return null;
            yield return null;

            f.Report(CreateMocRgbCapture(sensorDef));

            DatasetCapture.ResetSimulation();
            Assert.AreEqual(1, collector.currentRun.frames.Count);
        }

        [UnityTest]
        public IEnumerator ReportCapture_ReportsProperJson()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor(
                "camera", "camera", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var sensor = CreateMocRgbCapture(sensorDef);
            sensorHandle.ReportSensor(sensor);

            yield return null;
            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            Assert.NotNull(collector.currentRun);
            Assert.AreEqual(collector.currentRun.frames.Count, 1);
            Assert.NotNull(collector.currentRun.frames.First().sensors);
            Assert.AreEqual(collector.currentRun.frames.First().sensors.Count(), 1);

            var rgb = collector.currentRun.frames.First().sensors.First() as RgbSensor;
            Assert.NotNull(rgb);

            AssertUtils.AreEqual(rgb, sensor);
        }

        [UnityTest]
        public IEnumerator StartNewSequence_ProperlyIncrementsSequence()
        {
            var timingsExpected = new(int seq, int step, int timestamp)[]
            {
                (0, 0, 0),
                (0, 1, 2),
                (1, 0, 0),
                (1, 1, 2)
            };


            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            //DatasetCapture.StartNewSequence();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 2, 0);
            var sensor = new RgbSensor(sensorDef, Vector3.zero, Quaternion.identity);

            Assert.IsTrue(sensorHandle.ShouldCaptureThisFrame);
            sensorHandle.ReportSensor(sensor);
            yield return null;

            Assert.IsTrue(sensorHandle.ShouldCaptureThisFrame);
            sensorHandle.ReportSensor(sensor);
            yield return null;

            DatasetCapture.StartNewSequence();
            Assert.IsTrue(sensorHandle.ShouldCaptureThisFrame);
            sensorHandle.ReportSensor(sensor);
            yield return null;
            Assert.IsTrue(sensorHandle.ShouldCaptureThisFrame);
            sensorHandle.ReportSensor(sensor);
            yield return null;

            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            Debug.Log("after watcher");

            Assert.NotNull(collector.currentRun);
            Assert.AreEqual(timingsExpected.Length, collector.currentRun.TotalFrames);

            var i = 0;
            foreach (var(seq, step, timestamp) in timingsExpected)
            {
                var collected = collector.currentRun.frames[i++];
                Assert.AreEqual(seq, collected.sequence);
                Assert.AreEqual(step, collected.step);
                Assert.AreEqual(timestamp, collected.timestamp);
            }
        }

        [UnityTest]
        public IEnumerator ReportAnnotation_AddsProperJsonToCapture()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var sensor = new RgbSensor(sensorDef, Vector3.zero, Quaternion.identity);
            sensorHandle.ReportSensor(sensor);

            var def = new SemanticSegmentationDefinition("labeler", Array.Empty<SemanticSegmentationDefinitionEntry>());
            var annotation = new SemanticSegmentationAnnotation(
                def, sensorHandle.Id, ImageEncodingFormat.Png, Vector2.zero, new List<SemanticSegmentationDefinitionEntry>(), Array.Empty<byte>());

            DatasetCapture.RegisterAnnotationDefinition(def);
            sensorHandle.ReportAnnotation(def, annotation);

            yield return null;
            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            Assert.NotNull(collector.currentRun);
            Assert.AreEqual(collector.currentRun.frames.Count, 1);
            Assert.NotNull(collector.currentRun.frames.First().sensors);
            Assert.AreEqual(collector.currentRun.frames.First().sensors.Count(), 1);

            var rgb = collector.currentRun.frames.First().sensors.First() as RgbSensor;
            Assert.NotNull(rgb);

            AssertUtils.AreEqual(rgb, sensor);

            Assert.NotNull(rgb.annotations);
            Assert.AreEqual(1, rgb.annotations.Count());
            var seg = rgb.annotations.First() as SemanticSegmentationAnnotation;
            Assert.NotNull(seg);
        }

        class TestDef : AnnotationDefinition
        {
            public override string modelType => "TestDef";
            public override string description => "description";
            public TestDef() : base("annotation_test") {}
        }

        class TestDef2 : AnnotationDefinition
        {
            public override string modelType => "TestDef2";
            public override string description => "description";
            public TestDef2() : base("test2") {}
        }

        class TestAnnotation : Annotation
        {
            public override string modelType => "Test";
            public struct Entry
            {
                public string a;
                public int b;
            }

            public List<Entry> entries = new List<Entry>();

            public TestAnnotation(TestDef def, string sensorId, string annotationType)
                : base(def, sensorId) {}

            public TestAnnotation()
                : base(new TestDef(), "") {}
        }

        [UnityTest]
        public IEnumerator ReportAnnotationValues_ReportsProperJson()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var sensor = new RgbSensor(sensorDef, Vector3.zero, Quaternion.identity);
            sensorHandle.ReportSensor(sensor);

            var def = new TestDef();
            var ann = new TestAnnotation()
            {
                entries = new List<TestAnnotation.Entry>()
                {
                    new TestAnnotation.Entry { a = "a string", b = 10 },
                    new TestAnnotation.Entry { a = "a second string", b = 20 }
                }
            };

            DatasetCapture.RegisterAnnotationDefinition(def);
            sensorHandle.ReportAnnotation(def, ann);

            yield return null;
            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            var rgb = collector.currentRun.frames.First().sensors.First() as RgbSensor;
            Assert.NotNull(rgb);

            Assert.NotNull(rgb.annotations);
            Assert.AreEqual(1, rgb.annotations.Count());
            var tAnn = rgb.annotations.First() as TestAnnotation;
            Assert.NotNull(tAnn);

            Assert.AreEqual(2, tAnn.entries.Count);

            Assert.AreEqual("a string", tAnn.entries[0].a);
            Assert.AreEqual(10, tAnn.entries[0].b);
            Assert.AreEqual("a second string", tAnn.entries[1].a);
            Assert.AreEqual(20, tAnn.entries[1].b);
        }

        [Test]
        public void ReportAnnotationFile_WhenCaptureNotExpected_Throws()
        {
            var def = new TestDef();
            DatasetCapture.RegisterAnnotationDefinition(def);
            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 100, CaptureTriggerMode.Scheduled, 1, 0);
            Assert.Throws<InvalidOperationException>(() => sensorHandle.ReportAnnotation(def, null));

            DatasetCapture.ResetSimulation();
        }

        [Test]
        public void ReportAnnotationValues_WhenCaptureNotExpected_Throws()
        {
            var def = new TestDef();
            DatasetCapture.RegisterAnnotationDefinition(def);
            var ann = new TestAnnotation()
            {
                entries = new List<TestAnnotation.Entry>()
                {
                    new TestAnnotation.Entry { a = "a string", b = 10 },
                    new TestAnnotation.Entry { a = "a second string", b = 20 }
                }
            };
            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 100, CaptureTriggerMode.Scheduled, 1, 0);
            Assert.Throws<InvalidOperationException>(() => sensorHandle.ReportAnnotation(def, ann));
            DatasetCapture.ResetSimulation();
        }

        [Test]
        public void ReportAnnotationAsync_WhenCaptureNotExpected_Throws()
        {
            var def = new TestDef();
            DatasetCapture.RegisterAnnotationDefinition(def);
            var ann = new TestAnnotation()
            {
                entries = new List<TestAnnotation.Entry>()
                {
                    new TestAnnotation.Entry { a = "a string", b = 10 },
                    new TestAnnotation.Entry { a = "a second string", b = 20 }
                }
            };
            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 100, CaptureTriggerMode.Scheduled, 1, 0);
            Assert.Throws<InvalidOperationException>(() => sensorHandle.ReportAnnotationAsync(def));
        }

        [Test]
        public void ResetSimulation_CallsSimulationEnding()
        {
            var timesCalled = 0;
            DatasetCapture.SimulationEnding += () => timesCalled++;
            DatasetCapture.ResetSimulation();
            DatasetCapture.ResetSimulation();
            Assert.AreEqual(2, timesCalled);
        }

        [Test]
        public void ResetSimulation_WithUnreportedAnnotationAsync_LogsError()
        {
            var def = new TestDef();
            DatasetCapture.RegisterAnnotationDefinition(def);
            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var asyncAnnotation = sensorHandle.ReportAnnotationAsync(def);
            Assert.IsTrue(asyncAnnotation.IsValid());

            DatasetCapture.ResetSimulation();
            LogAssert.Expect(LogType.Error, new Regex("Simulation ended with pending .*"));
        }

        [Test]
        public void ResetSimulation_WithUnreportedMetricAsync_LogsError()
        {
            var def = new TestMetricDef();
            DatasetCapture.RegisterMetric(def);
            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            sensorHandle.ReportMetricAsync(def);
            DatasetCapture.ResetSimulation();
            LogAssert.Expect(LogType.Error, new Regex("Simulation ended with pending .*"));
        }

        [Test]
        public void AnnotationAsyncIsValid_ReturnsProperValue()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            LogAssert.ignoreFailingMessages = true; //we are not worried about timing out

            var def = new TestDef();
            DatasetCapture.RegisterAnnotationDefinition(def);
            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var asyncAnnotation = sensorHandle.ReportAnnotationAsync(def);
            Assert.IsTrue(asyncAnnotation.IsValid());

            DatasetCapture.ResetSimulation();
            Assert.IsFalse(asyncAnnotation.IsValid());
        }

        [UnityTest]
        public IEnumerator AnnotationAsyncReportValue_ReportsProperJson()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var sensor = new RgbSensor(sensorDef, Vector3.zero, Quaternion.identity);
            sensorHandle.ReportSensor(sensor);

            var def = new TestDef();
            var ann = new TestAnnotation()
            {
                entries = new List<TestAnnotation.Entry>()
                {
                    new TestAnnotation.Entry { a = "a string", b = 10 },
                    new TestAnnotation.Entry { a = "a second string", b = 20 }
                }
            };

            DatasetCapture.RegisterAnnotationDefinition(def);
            var asyncFuture = sensorHandle.ReportAnnotationAsync(def);

            Assert.IsTrue(asyncFuture.IsPending());
            asyncFuture.Report(ann);
            Assert.IsFalse(asyncFuture.IsPending());
            yield return null;                            // TODO why does removing this cause us to spiral out for eternity
            DatasetCapture.ResetSimulation();

            Assert.IsFalse(sensorHandle.IsValid);

            var rgb = collector.currentRun.frames.First().sensors.First() as RgbSensor;
            Assert.NotNull(rgb);

            Assert.NotNull(rgb.annotations);
            Assert.AreEqual(1, rgb.annotations.Count());
            var tAnn = rgb.annotations.First() as TestAnnotation;
            Assert.NotNull(tAnn);

            Assert.AreEqual(2, tAnn.entries.Count);

            Assert.AreEqual("a string", tAnn.entries[0].a);
            Assert.AreEqual(10, tAnn.entries[0].b);
            Assert.AreEqual("a second string", tAnn.entries[1].a);
            Assert.AreEqual(20, tAnn.entries[1].b);
        }

        [UnityTest]
        public IEnumerator AnnotationAsyncReportResult_FindsCorrectPendingCaptureAfterStartingNewSequence()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var def = new TestDef();
            DatasetCapture.RegisterAnnotationDefinition(def);

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);

            // Record one capture for this frame
            var sensor = new RgbSensor(sensorDef, Vector3.zero, Quaternion.identity);
            sensorHandle.ReportSensor(sensor);

            // Wait one frame
            yield return null;

            // Reset the capture step
            DatasetCapture.StartNewSequence();

            // Record a new capture on different frame that has the same step (0) as the first capture
            sensorHandle.ReportSensor(sensor);

            var ann = new TestAnnotation()
            {
                entries = new List<TestAnnotation.Entry>()
                {
                    new TestAnnotation.Entry { a = "a string", b = 10 },
                    new TestAnnotation.Entry { a = "a second string", b = 20 }
                }
            };

            // Confirm that the annotation correctly skips the first pending capture to write to the second
            var asyncAnnotation = sensorHandle.ReportAnnotationAsync(def);
            Assert.DoesNotThrow(() => asyncAnnotation.Report(ann));
            sensorHandle.ReportSensor(sensor);
            DatasetCapture.ResetSimulation();
        }

        [Test]
        public void CreateAnnotation_MultipleTimes_WritesProperTypeOnce()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var def1 = new TestDef();
            var def2 = new TestDef();
            DatasetCapture.RegisterAnnotationDefinition(def1);
            DatasetCapture.RegisterAnnotationDefinition(def2);

            DatasetCapture.ResetSimulation();

            Assert.AreNotEqual(def1.id, def2.id);
            Assert.AreEqual("annotation_test", def1.id);
            Assert.AreEqual("annotation_test_0", def2.id);

            Assert.AreEqual(2, collector.annotationDefinitions.Count);
            Assert.AreEqual(def1.id, collector.annotationDefinitions[0].id);
            Assert.AreEqual(def2.id, collector.annotationDefinitions[1].id);
        }

        [Test]
        public void CreateAnnotation_MultipleTimesWithDifferentParameters_WritesProperTypes()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var def1 = new TestDef();
            var def2 = new TestDef2();

            DatasetCapture.RegisterAnnotationDefinition(def1);
            DatasetCapture.RegisterAnnotationDefinition(def2);

            DatasetCapture.ResetSimulation();
            Assert.AreNotEqual(def1.id, def2.id);
            Assert.AreEqual("annotation_test", def1.id);
            Assert.AreEqual("test2", def2.id);

            Assert.AreEqual(2, collector.annotationDefinitions.Count);
            Assert.AreEqual(def1.id, collector.annotationDefinitions[0].id);
            Assert.AreEqual(def2.id, collector.annotationDefinitions[1].id);
        }

        class TestMetricDef : MetricDefinition
        {
            public TestMetricDef() : base("test", "counting things") {}
        }

        [Test]
        public void ReportMetricValues_WhenCaptureNotExpected_Throws()
        {
            var def = new TestMetricDef();
            DatasetCapture.RegisterMetric(def);
            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 100, CaptureTriggerMode.Scheduled, 1, 0);
            var metric = new GenericMetric(1, def);
            Assert.Throws<InvalidOperationException>(() => sensorHandle.ReportMetric(def, metric));
        }

        [Test]
        public void ReportMetricAsync_WhenCaptureNotExpected_Throws()
        {
            var def = new TestMetricDef();
            DatasetCapture.RegisterMetric(def);
            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 100, CaptureTriggerMode.Scheduled, 1, 0);
            Assert.Throws<InvalidOperationException>(() => sensorHandle.ReportMetricAsync(def));
        }

        [Test]
        public void MetricAsyncIsValid_ReturnsProperValue()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            LogAssert.ignoreFailingMessages = true; //we are not worried about timing out

            var def = new TestMetricDef();
            DatasetCapture.RegisterMetric(def);
            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var asyncMetric = sensorHandle.ReportMetricAsync(def);
            Assert.IsTrue(asyncMetric.IsValid());

            DatasetCapture.ResetSimulation();
            Assert.IsFalse(asyncMetric.IsValid());
        }

        public enum MetricTarget
        {
            Global,
            Capture,
            Annotation
        }

        [UnityTest]
        public IEnumerator MetricReportValues_WithNoReportsInFrames_DoesNotIncrementStep()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            DatasetCapture.ResetSimulation();

            var def = new TestMetricDef();
            DatasetCapture.RegisterMetric(def);

            var boolDef = new MetricDefinition("bool_def", "bool_def");
            var boolDef2 = new MetricDefinition("bool_def2", "bool_def2");
            var boolArrayDef = new MetricDefinition("bool_array_def", "bool_array_def");
            var vec3Def = new MetricDefinition("vec3_def", "vec3_def");

            var sensorHandle = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);

            yield return null;
            yield return null;
            yield return null;

            var metric = new GenericMetric(1, def);
            DatasetCapture.ReportMetric(def, metric);

            var m2 = new GenericMetric(true, boolDef);
            DatasetCapture.ReportMetric(boolDef, m2);

            var m3 = new GenericMetric(new[] {true, true, false}, boolArrayDef);
            DatasetCapture.ReportMetric(boolArrayDef, m3);

            var m4 = new GenericMetric(new Vector3(20f, 80f, 111f), vec3Def);
            DatasetCapture.ReportMetric(vec3Def, m4);

            var m5 = new GenericMetric(false, boolDef2);
            DatasetCapture.ReportMetric(boolDef2, m5);

            DatasetCapture.ResetSimulation();

            Assert.AreEqual(1, collector.currentRun.frames.Count);
            var f = collector.currentRun.frames.First();
            Assert.AreEqual(5, f.metrics.Count);

            foreach (var m in f.metrics)
            {
                switch (m.id)
                {
                    case "test":
                        Assert.AreEqual(1, m.GetValues<int>().Length);
                        break;
                    case "bool_def":
                        Assert.AreEqual(1, m.GetValues<bool>().Length);
                        Assert.IsTrue(m.GetValues<bool>().First());
                        break;
                    case "bool_def2":
                        Assert.AreEqual(1, m.GetValues<bool>().Length);
                        Assert.IsFalse(m.GetValues<bool>().First());
                        break;
                    case "bool_array_def":
                        Assert.AreEqual(3, m.GetValues<bool>().Length);
                        var vals = m.GetValues<bool>();
                        Assert.AreEqual(3, vals.Length);
                        Assert.IsTrue(vals[0]);
                        Assert.IsTrue(vals[1]);
                        Assert.IsFalse(vals[2]);
                        break;
                    case "vec3_def":
                        var vals2 = m.GetValues<float>();
                        Assert.AreEqual(3, vals2.Length);
                        Assert.AreEqual(20f, vals2[0]);
                        Assert.AreEqual(80f, vals2[1]);
                        Assert.AreEqual(111f, vals2[2]);
                        break;
                }
            }
        }

        [UnityTest]
        public IEnumerator SensorHandleReportMetric_BeforeReportCapture_ReportsProperJson()
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            DatasetCapture.ResetSimulation();

            // DatasetCapture.automaticShutdown = false;

            var def = new TestMetricDef();
            DatasetCapture.RegisterMetric(def);

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);


            // var values = new[] { 1 };
            //
            // var expectedLine = @"""step"": 0";

            // var metricDefinition = DatasetCapture.RegisterMetricDefinition("");
            // var sensor = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);

            yield return null;
            var metric = new GenericMetric(1, def);
            sensorHandle.ReportMetric(def, metric);
            var sensor = new RgbSensor(sensorDef, Vector3.zero, Quaternion.identity);
            sensorHandle.ReportSensor(sensor);


            //sensorHandle.ReportCapture("file", new SensorSpatialData(Pose.identity, Pose.identity, null, null));
            DatasetCapture.ResetSimulation();

            var first = collector.currentRun.frames.First();
            Assert.NotNull(first);
            Assert.NotNull(first.metrics);
            Assert.NotZero(first.metrics.Count());
        }

        class TestMetric2Entry : IMessageProducer
        {
            public string a;
            public int b;
            public void ToMessage(IMessageBuilder builder)
            {
                builder.AddString("a", a);
                builder.AddInt("b", b);
            }
        }

        [UnityTest]
        public IEnumerator MetricAsyncReportValues_ReportsProperJson(
            [Values(MetricTarget.Global, MetricTarget.Capture, MetricTarget.Annotation)] MetricTarget metricTarget,
            [Values(true, false)] bool async)
        {
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            DatasetCapture.ResetSimulation();

            var metDef = new TestMetricDef();

            var values = new IMessageProducer[]
            {
                new TestMetric2Entry
                {
                    a = "a string",
                    b = 10
                },
                new TestMetric2Entry
                {
                    a = "a second string",
                    b = 20
                },
            };

            var metric = new GenericMetric(values, metDef);

            DatasetCapture.RegisterMetric(metDef);
            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var annDef = new TestDef();
            DatasetCapture.RegisterAnnotationDefinition(annDef);

            var ann = new TestAnnotation()
            {
                entries = new List<TestAnnotation.Entry>()
                {
                    new TestAnnotation.Entry { a = "a string", b = 10 },
                    new TestAnnotation.Entry { a = "a second string", b = 20 }
                }
            };

            var annotationHandle = sensorHandle.ReportAnnotation(annDef, ann);
            var sensor = new RgbSensor(sensorDef, Vector3.zero, Quaternion.identity);
            sensorHandle.ReportSensor(sensor);

            if (async)
            {
                AsyncFuture<Metric> asyncMetric;
                switch (metricTarget)
                {
                    case MetricTarget.Global:
                        asyncMetric = DatasetCapture.ReportMetric(metDef);
                        break;
                    case MetricTarget.Capture:
                        asyncMetric = sensorHandle.ReportMetricAsync(metDef);
                        break;
                    case MetricTarget.Annotation:
                        asyncMetric = annotationHandle.ReportMetricAsync(metDef);
                        break;
                    default:
                        throw new Exception("unsupported");
                }

                Assert.IsTrue(asyncMetric.IsPending());
                asyncMetric.Report(metric);
                Assert.IsFalse(asyncMetric.IsPending());
            }
            else
            {
                switch (metricTarget)
                {
                    case MetricTarget.Global:
                        DatasetCapture.ReportMetric(metDef, metric);
                        break;
                    case MetricTarget.Capture:
                        sensorHandle.ReportMetric(metDef, metric);
                        break;
                    case MetricTarget.Annotation:
                        annotationHandle.ReportMetric(metDef, metric);
                        break;
                    default:
                        throw new Exception("unsupported");
                }
            }

            yield return null; // Need to advance one frame to allow for the simulation state to start
            DatasetCapture.ResetSimulation();

            var first = collector.currentRun.frames.First();
            Assert.NotNull(first);
            var foundSensor = first.sensors.First();
            Assert.NotNull(first.metrics);
            Assert.NotZero(first.metrics.Count);

            var m = first.metrics.First();
            Assert.NotNull(m);

            switch (metricTarget)
            {
                case MetricTarget.Global:
                    Assert.IsTrue(string.IsNullOrEmpty(m.sensorId));
                    Assert.IsTrue(string.IsNullOrEmpty(m.annotationId));
                    break;
                case MetricTarget.Capture:
                    Assert.IsTrue(string.IsNullOrEmpty(m.annotationId));
                    Assert.AreEqual("camera", m.sensorId);
                    break;
                case MetricTarget.Annotation:
                    Assert.AreEqual("camera", m.sensorId);
                    Assert.AreEqual("annotation_test", m.annotationId);
                    break;
                default:
                    throw new Exception("unsupported");
            }

            var metricValues = m.GetValues<TestMetric2Entry>();

            Assert.AreEqual(2, metricValues.Length);

            var v0 = metricValues[0];
            var v1 = metricValues[1];

            Assert.AreEqual("a string", v0.a);
            Assert.AreEqual(10, v0.b);
            Assert.AreEqual("a second string", v1.a);
            Assert.AreEqual(20, v1.b);
        }

        class MetDef1 : MetricDefinition
        {
            public MetDef1()
                : base("name", "name") {}
        }

        class MetDef2 : MetricDefinition
        {
            public MetDef2()
                : base("name2", "name2") {}
        }


        [Test]
        public void CreateMetric_MultipleTimesWithDifferentParameters_WritesProperTypes()
        {
            var md1 = new MetDef1();
            var md2 = new MetDef2();
            var md3 = new MetDef1();

            DatasetCapture.RegisterMetric(md1);
            DatasetCapture.RegisterMetric(md3);
            DatasetCapture.RegisterMetric(md2);

            DatasetCapture.ResetSimulation();

            Assert.AreEqual("name", md1.id);
            Assert.AreEqual("name2", md2.id);
            Assert.AreNotEqual("name", md3.id);
            Assert.AreEqual("name_0", md3.id);
        }

        struct TestSpec
        {
            public int label_id;
            public string label_name;
            public int[] pixel_value;
        }

        public enum AdditionalInfoKind
        {
            Annotation,
            Metric
        }

        class A1 : AnnotationDefinition
        {
            public override string modelType => "A1";
            public override string description => "description";
            public TestSpec[] specValues;

            public A1() : base("id") {}
        }

        class M1 : MetricDefinition
        {
            public TestSpec[] specValues;

            public M1()
                : base("id", "description") {}
        }

        [Test]
        public void CreateAnnotationOrMetric_WithSpecValues_WritesProperTypes(
            [Values(AdditionalInfoKind.Annotation, AdditionalInfoKind.Metric)] AdditionalInfoKind additionalInfoKind)
        {
            var specValues = new[]
            {
                new TestSpec
                {
                    label_id = 1,
                    label_name = "sky",
                    pixel_value = new[] { 1, 2, 3}
                },
                new TestSpec
                {
                    label_id = 2,
                    label_name = "sidewalk",
                    pixel_value = new[] { 4, 5, 6}
                }
            };

            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);
            DatasetCapture.ResetSimulation();

            if (additionalInfoKind == AdditionalInfoKind.Annotation)
            {
                var ad = new A1
                {
                    specValues = specValues
                };
                DatasetCapture.RegisterAnnotationDefinition(ad);
            }
            else
            {
                var md = new M1
                {
                    specValues = specValues
                };

                DatasetCapture.RegisterMetric(md);
            }

            DatasetCapture.ResetSimulation();

            if (additionalInfoKind == AdditionalInfoKind.Annotation)
            {
                Assert.AreEqual(1, collector.annotationDefinitions.Count);
                var a = collector.annotationDefinitions.First() as A1;
                Assert.NotNull(a);
                Assert.AreEqual(2, a.specValues.Length);

                Assert.AreEqual(1, a.specValues[0].label_id);
                Assert.AreEqual("sky", a.specValues[0].label_name);
                Assert.AreEqual(3, a.specValues[0].pixel_value.Count());
                Assert.AreEqual(1, a.specValues[0].pixel_value[0]);
                Assert.AreEqual(2, a.specValues[0].pixel_value[1]);
                Assert.AreEqual(3, a.specValues[0].pixel_value[2]);

                Assert.AreEqual(2, a.specValues[1].label_id);
                Assert.AreEqual("sidewalk", a.specValues[1].label_name);
                Assert.AreEqual(3, a.specValues[1].pixel_value.Count());
                Assert.AreEqual(4, a.specValues[1].pixel_value[0]);
                Assert.AreEqual(5, a.specValues[1].pixel_value[1]);
                Assert.AreEqual(6, a.specValues[1].pixel_value[2]);
            }
            else
            {
                Assert.AreEqual(1, collector.metricDefinitions.Count);
                var a = collector.metricDefinitions.First() as M1;
                Assert.NotNull(a);
                Assert.AreEqual(2, a.specValues.Length);

                Assert.AreEqual(1, a.specValues[0].label_id);
                Assert.AreEqual("sky", a.specValues[0].label_name);
                Assert.AreEqual(3, a.specValues[0].pixel_value.Count());
                Assert.AreEqual(1, a.specValues[0].pixel_value[0]);
                Assert.AreEqual(2, a.specValues[0].pixel_value[1]);
                Assert.AreEqual(3, a.specValues[0].pixel_value[2]);

                Assert.AreEqual(2, a.specValues[1].label_id);
                Assert.AreEqual("sidewalk", a.specValues[1].label_name);
                Assert.AreEqual(3, a.specValues[1].pixel_value.Count());
                Assert.AreEqual(4, a.specValues[1].pixel_value[0]);
                Assert.AreEqual(5, a.specValues[1].pixel_value[1]);
                Assert.AreEqual(6, a.specValues[1].pixel_value[2]);
            }
        }

#if UNITY_EDITOR
        // Perception only supports setting the output directory from the command line in non editor based simulations
        [UnityTest]
        public IEnumerator TestSetOutputDirectory()
        {
            const string id = "camera";
            const string modality = "camera";
            const string def = "Cam (FL2-14S3M-C)";
            const int firstFrame = 1;
            const CaptureTriggerMode mode = CaptureTriggerMode.Scheduled;
            const int delta = 1;
            const int framesBetween = 0;

            // Clear dataset override from previous tests
            DatasetCapture.OverrideEndpoint(null);

            PerceptionSettings.endpoint = new PerceptionEndpoint();

            var savePath = PerceptionSettings.GetOutputBasePath();

            var outputPath = Path.Combine(PerceptionSettings.defaultOutputPath, $"test_{Guid.NewGuid()}");
            Directory.CreateDirectory(outputPath);
            DatasetCapture.SetOutputPath(outputPath);

            DatasetCapture.ResetSimulation();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor(id, modality, def, firstFrame, mode, delta, framesBetween);
            Assert.IsTrue(sensorHandle.IsValid);

            yield return null;

            // grab a handle for the endpoint of the current run
            var endpoint = (PerceptionEndpoint)DatasetCapture.activateEndpoint;

            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            var path = endpoint.datasetPath;

            // Verify that the output directory exists and a captures and a metrics file is written to it
            Assert.IsTrue(path.Contains(outputPath));
            DirectoryAssert.Exists(path);
            FileAssert.Exists(Path.Combine(path, "captures_000.json"));
            FileAssert.Exists(Path.Combine(path, "metrics_000.json"));

            Directory.Delete(outputPath, true);

            DirectoryAssert.DoesNotExist(outputPath);

            DatasetCapture.SetOutputPath(savePath);
        }

#endif
    }
}
