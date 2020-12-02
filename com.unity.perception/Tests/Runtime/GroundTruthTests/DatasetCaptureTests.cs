using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;
// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedField.Local

namespace GroundTruthTests
{
    [TestFixture]
    public class DatasetCaptureTests
    {
        [Test]
        public void RegisterSensor_ReportsProperJson()
        {
            var egoDescription = @"the main car driving in simulation";
            var sensorDescription = "Cam (FL2-14S3M-C)";
            var modality = "camera";

            var egoJsonExpected =
                $@"{{
  ""version"": ""{DatasetCapture.SchemaVersion}"",
  ""egos"": [
    {{
      ""id"": <guid>,
      ""description"": ""{egoDescription}""
    }}
  ]
}}";
            var sensorJsonExpected =
                $@"{{
  ""version"": ""{DatasetCapture.SchemaVersion}"",
  ""sensors"": [
    {{
      ""id"": <guid>,
      ""ego_id"": <guid>,
      ""modality"": ""{modality}"",
      ""description"": ""{sensorDescription}""
    }}
  ]
}}";

            var ego = DatasetCapture.RegisterEgo(egoDescription);
            var sensorHandle = DatasetCapture.RegisterSensor(ego, modality, sensorDescription, 1, 1);
            Assert.IsTrue(sensorHandle.IsValid);
            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            var sensorsPath = Path.Combine(DatasetCapture.OutputDirectory, "sensors.json");
            var egosPath = Path.Combine(DatasetCapture.OutputDirectory, "egos.json");

            FileAssert.Exists(egosPath);
            FileAssert.Exists(sensorsPath);

            AssertJsonFileEquals(egoJsonExpected, egosPath);
            AssertJsonFileEquals(sensorJsonExpected, sensorsPath);
        }

        [Test]
        public void ReportCapture_ReportsProperJson()
        {
            var filename = "my/file.png";

            var egoPosition = new float3(.02f, .03f, .04f);
            var egoRotation = new quaternion(.1f, .2f, .3f, .4f);

            var egoVelocity = new Vector3(.1f, .2f, .3f);

            var position = new float3(.2f, 1.1f, .3f);
            var rotation = new quaternion(.3f, .2f, .1f, .5f);
            var intrinsics = new float3x3(.1f, .2f, .3f, 1f, 2f, 3f, 10f, 20f, 30f);


            var capturesJsonExpected =
                $@"{{
  ""version"": ""{DatasetCapture.SchemaVersion}"",
  ""captures"": [
    {{
      ""id"": <guid>,
      ""sequence_id"": <guid>,
      ""step"": 0,
      ""timestamp"": 0.0,
      ""sensor"": {{
        ""sensor_id"": <guid>,
        ""ego_id"": <guid>,
        ""modality"": ""camera"",
        ""translation"": [
          {Format(position.x)},
          {Format(position.y)},
          {Format(position.z)}
        ],
        ""rotation"": [
          {Format(rotation.value.x)},
          {Format(rotation.value.y)},
          {Format(rotation.value.z)},
          {Format(rotation.value.w)}
        ],
        ""camera_intrinsic"": [
          [
            {Format(intrinsics.c0.x)},
            {Format(intrinsics.c0.y)},
            {Format(intrinsics.c0.z)}
          ],
          [
            {Format(intrinsics.c1.x)},
            {Format(intrinsics.c1.y)},
            {Format(intrinsics.c1.z)}
          ],
          [
            {Format(intrinsics.c2.x)},
            {Format(intrinsics.c2.y)},
            {Format(intrinsics.c2.z)}
          ]
        ]
      }},
      ""ego"": {{
        ""ego_id"": <guid>,
        ""translation"": [
          {Format(egoPosition.x)},
          {Format(egoPosition.y)},
          {Format(egoPosition.z)}
        ],
        ""rotation"": [
          {Format(egoRotation.value.x)},
          {Format(egoRotation.value.y)},
          {Format(egoRotation.value.z)},
          {Format(egoRotation.value.w)}
        ],
        ""velocity"": [
          {Format(egoVelocity.x)},
          {Format(egoVelocity.y)},
          {Format(egoVelocity.z)}
        ],
        ""acceleration"": null
      }},
      ""filename"": ""{filename}"",
      ""format"": ""PNG""
    }}
  ]
}}";

            var ego = DatasetCapture.RegisterEgo("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "camera", "", 1, 0);
            var sensorSpatialData = new SensorSpatialData(new Pose(egoPosition, egoRotation), new Pose(position, rotation), egoVelocity, null);
            sensorHandle.ReportCapture(filename, sensorSpatialData, ("camera_intrinsic", intrinsics));

            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            var capturesPath = Path.Combine(DatasetCapture.OutputDirectory, "captures_000.json");

            FileAssert.Exists(capturesPath);

            AssertJsonFileEquals(capturesJsonExpected, capturesPath);
        }

        [UnityTest]
        public IEnumerator StartNewSequence_ProperlyIncrementsSequence()
        {
            var timingsExpected = new(int step, int timestamp, bool expectNewSequence)[]
            {
                (0, 0, true),
                (1, 2, false),
                (0, 0, true),
                (1, 2, false)
            };

            var ego = DatasetCapture.RegisterEgo("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 2, 0);
            var sensorSpatialData = new SensorSpatialData(default, default, null, null);
            Assert.IsTrue(sensorHandle.ShouldCaptureThisFrame);
            sensorHandle.ReportCapture("f", sensorSpatialData);
            yield return null;
            Assert.IsTrue(sensorHandle.ShouldCaptureThisFrame);
            sensorHandle.ReportCapture("f", sensorSpatialData);
            yield return null;
            DatasetCapture.StartNewSequence();
            Assert.IsTrue(sensorHandle.ShouldCaptureThisFrame);
            sensorHandle.ReportCapture("f", sensorSpatialData);
            yield return null;
            Assert.IsTrue(sensorHandle.ShouldCaptureThisFrame);
            sensorHandle.ReportCapture("f", sensorSpatialData);

            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            //read all captures from the output directory
            List<JObject> captures = new List<JObject>();
            foreach (var capturesPath in Directory.EnumerateFiles(DatasetCapture.OutputDirectory, "captures_*.json"))
            {
                var capturesText = File.ReadAllText(capturesPath);
                var jObject = JToken.ReadFrom(new JsonTextReader(new StringReader(capturesText)));
                var captureJArray = (JArray)jObject["captures"];
                captures.AddRange(captureJArray.Cast<JObject>());
            }

            Assert.AreEqual(timingsExpected.Length, captures.Count);

            var currentSequenceId = "00";
            for (int i = 0; i < timingsExpected.Length; i++)
            {
                var timingExpected = timingsExpected[i];
                var text = captures[i];
                Assert.AreEqual(timingExpected.step, text["step"].Value<int>());
                Assert.AreEqual(timingExpected.timestamp, text["timestamp"].Value<int>());
                var newSequenceId = text["sequence_id"].ToString();

                if (timingExpected.expectNewSequence)
                    Assert.AreNotEqual(newSequenceId, currentSequenceId, $"Expected new sequence in frame {i}, but was same");
                else
                    Assert.AreEqual(newSequenceId, currentSequenceId, $"Expected same sequence in frame {i}, but was new");

                currentSequenceId = newSequenceId;
            }
        }

        //Format a float to match Newtonsoft.Json formatting
        string Format(float value)
        {
            var result = value.ToString("R", CultureInfo.InvariantCulture);
            if (!result.Contains("."))
                return result + ".0";

            return result;
        }

        [Test]
        public void ReportAnnotation_AddsProperJsonToCapture()
        {
            var filename = "my/file.png";
            var annotationDefinitionGuid = Guid.NewGuid();

            var annotationDefinitionsJsonExpected =
                $@"{{
  ""version"": ""{DatasetCapture.SchemaVersion}"",
  ""annotation_definitions"": [
    {{
      ""id"": <guid>,
      ""name"": ""semantic segmentation"",
      ""description"": ""pixel-wise semantic segmentation label"",
      ""format"": ""PNG""
    }}
  ]
}}";
            var annotationsJsonExpected =
                $@"      ""annotations"": [
        {{
          ""id"": <guid>,
          ""annotation_definition"": <guid>,
          ""filename"": ""annotations/semantic_segmentation_000.png""
        }}
      ]";

            var ego = DatasetCapture.RegisterEgo("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 0);
            sensorHandle.ReportCapture(filename, default);
            var annotationDefinition = DatasetCapture.RegisterAnnotationDefinition("semantic segmentation", "pixel-wise semantic segmentation label", "PNG", annotationDefinitionGuid);
            sensorHandle.ReportAnnotationFile(annotationDefinition, "annotations/semantic_segmentation_000.png");

            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            var annotationDefinitionsPath = Path.Combine(DatasetCapture.OutputDirectory, "annotation_definitions.json");
            var capturesPath = Path.Combine(DatasetCapture.OutputDirectory, "captures_000.json");


            AssertJsonFileEquals(annotationDefinitionsJsonExpected, annotationDefinitionsPath);

            FileAssert.Exists(capturesPath);
            StringAssert.Contains(TestHelper.NormalizeJson(annotationsJsonExpected), EscapeGuids(File.ReadAllText(capturesPath)));
        }

        [Test]
        public void ReportAnnotationValues_ReportsProperJson()
        {
            var values = new[]
            {
                new TestValues()
                {
                    a = "a string",
                    b = 10
                },
                new TestValues()
                {
                    a = "a second string",
                    b = 20
                },
            };

            var expectedAnnotation = $@"      ""annotations"": [
        {{
          ""id"": <guid>,
          ""annotation_definition"": <guid>,
          ""values"": [
            {{
              ""a"": ""a string"",
              ""b"": 10
            }},
            {{
              ""a"": ""a second string"",
              ""b"": 20
            }}
          ]
        }}
      ]";

            var ego = DatasetCapture.RegisterEgo("");
            var annotationDefinition = DatasetCapture.RegisterAnnotationDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 0);

            sensorHandle.ReportAnnotationValues(annotationDefinition, values);
            DatasetCapture.ResetSimulation();

            var capturesPath = Path.Combine(DatasetCapture.OutputDirectory, "captures_000.json");

            FileAssert.Exists(capturesPath);
            StringAssert.Contains(TestHelper.NormalizeJson(expectedAnnotation), EscapeGuids(File.ReadAllText(capturesPath)));
        }

        [Test]
        public void ReportAnnotationFile_WhenCaptureNotExpected_Throws()
        {
            var ego = DatasetCapture.RegisterEgo("");
            var annotationDefinition = DatasetCapture.RegisterAnnotationDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 100);
            Assert.Throws<InvalidOperationException>(() => sensorHandle.ReportAnnotationFile(annotationDefinition, ""));
        }

        [Test]
        public void ReportAnnotationValues_WhenCaptureNotExpected_Throws()
        {
            var ego = DatasetCapture.RegisterEgo("");
            var annotationDefinition = DatasetCapture.RegisterAnnotationDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 100);
            Assert.Throws<InvalidOperationException>(() => sensorHandle.ReportAnnotationValues(annotationDefinition, new int[0]));
        }

        [Test]
        public void ReportAnnotationAsync_WhenCaptureNotExpected_Throws()
        {
            var ego = DatasetCapture.RegisterEgo("");
            var annotationDefinition = DatasetCapture.RegisterAnnotationDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 100);
            Assert.Throws<InvalidOperationException>(() => sensorHandle.ReportAnnotationAsync(annotationDefinition));
        }

        [Test]
        public void ResetSimulation_WithUnreportedAnnotationAsync_LogsError()
        {
            var ego = DatasetCapture.RegisterEgo("");
            var annotationDefinition = DatasetCapture.RegisterAnnotationDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 0);
            sensorHandle.ReportAnnotationAsync(annotationDefinition);
            DatasetCapture.ResetSimulation();
            LogAssert.Expect(LogType.Error, new Regex("Simulation ended with pending .*"));
        }

        [Test]
        public void ResetSimulation_CallsSimulationEnding()
        {
            int timesCalled = 0;
            DatasetCapture.SimulationEnding += () => timesCalled++;
            DatasetCapture.ResetSimulation();
            DatasetCapture.ResetSimulation();
            Assert.AreEqual(2, timesCalled);
        }

        [Test]
        public void AnnotationAsyncIsValid_ReturnsProperValue()
        {
            LogAssert.ignoreFailingMessages = true; //we aren't worried about "Simulation ended with pending..."

            var ego = DatasetCapture.RegisterEgo("");
            var annotationDefinition = DatasetCapture.RegisterAnnotationDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 0);
            var asyncAnnotation = sensorHandle.ReportAnnotationAsync(annotationDefinition);

            Assert.IsTrue(asyncAnnotation.IsValid);
            DatasetCapture.ResetSimulation();
            Assert.IsFalse(asyncAnnotation.IsValid);
        }

        [Test]
        public void AnnotationAsyncReportFile_ReportsProperJson()
        {
            var expectedAnnotation = $@"      ""annotations"": [
        {{
          ""id"": <guid>,
          ""annotation_definition"": <guid>,
          ""filename"": ""annotations/output.png""
        }}
      ]";

            var ego = DatasetCapture.RegisterEgo("");
            var annotationDefinition = DatasetCapture.RegisterAnnotationDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 0);
            var asyncAnnotation = sensorHandle.ReportAnnotationAsync(annotationDefinition);

            Assert.IsTrue(asyncAnnotation.IsPending);
            asyncAnnotation.ReportFile("annotations/output.png");
            Assert.IsFalse(asyncAnnotation.IsPending);
            DatasetCapture.ResetSimulation();

            var capturesPath = Path.Combine(DatasetCapture.OutputDirectory, "captures_000.json");

            FileAssert.Exists(capturesPath);
            StringAssert.Contains(TestHelper.NormalizeJson(expectedAnnotation), EscapeGuids(File.ReadAllText(capturesPath)));
        }

        public struct TestValues
        {
            public string a;
            public int b;
        }

        [Test]
        public void AnnotationAsyncReportValues_ReportsProperJson()
        {
            var values = new[]
            {
                new TestValues()
                {
                    a = "a string",
                    b = 10
                },
                new TestValues()
                {
                    a = "a second string",
                    b = 20
                },
            };

            var expectedAnnotation = $@"      ""annotations"": [
        {{
          ""id"": <guid>,
          ""annotation_definition"": <guid>,
          ""values"": [
            {{
  ""a"": ""a string"",
  ""b"": 10
}},
            {{
  ""a"": ""a second string"",
  ""b"": 20
}}
          ]
        }}
      ]";

            var ego = DatasetCapture.RegisterEgo("");
            var annotationDefinition = DatasetCapture.RegisterAnnotationDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 0);
            var asyncAnnotation = sensorHandle.ReportAnnotationAsync(annotationDefinition);

            Assert.IsTrue(asyncAnnotation.IsPending);
            asyncAnnotation.ReportValues(values);
            Assert.IsFalse(asyncAnnotation.IsPending);
            DatasetCapture.ResetSimulation();

            var capturesPath = Path.Combine(DatasetCapture.OutputDirectory, "captures_000.json");

            FileAssert.Exists(capturesPath);
            StringAssert.Contains(TestHelper.NormalizeJson(expectedAnnotation), EscapeGuids(File.ReadAllText(capturesPath)));
        }

        [UnityTest]
        public IEnumerator AnnotationAsyncReportResult_FindsCorrectPendingCaptureAfterStartingNewSequence()
        {
            const string fileName = "my/file.png";

            var value = new[]
            {
                new TestValues()
                {
                    a = "a string",
                    b = 10
                }
            };

            var ego = DatasetCapture.RegisterEgo("");
            var annotationDefinition = DatasetCapture.RegisterAnnotationDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 0);

            // Record one capture for this frame
            sensorHandle.ReportCapture(fileName, default);

            // Wait one frame
            yield return null;

            // Reset the capture step
            DatasetCapture.StartNewSequence();

            // Record a new capture on different frame that has the same step (0) as the first capture
            sensorHandle.ReportCapture(fileName, default);

            // Confirm that the annotation correctly skips the first pending capture to write to the second
            var asyncAnnotation = sensorHandle.ReportAnnotationAsync(annotationDefinition);
            Assert.DoesNotThrow(() => asyncAnnotation.ReportValues(value));
            DatasetCapture.ResetSimulation();
        }

        [Test]
        public void CreateAnnotation_MultipleTimes_WritesProperTypeOnce()
        {
            var annotationDefinitionGuid = new Guid(10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            var annotationDefinitionsJsonExpected =
                $@"{{
  ""version"": ""{DatasetCapture.SchemaVersion}"",
  ""annotation_definitions"": [
    {{
      ""id"": ""{annotationDefinitionGuid}"",
      ""name"": ""name"",
      ""format"": ""json""
    }}
  ]
}}";
            var annotationDefinition1 = DatasetCapture.RegisterAnnotationDefinition("name", id: annotationDefinitionGuid);
            var annotationDefinition2 = DatasetCapture.RegisterAnnotationDefinition("name", id: annotationDefinitionGuid);

            DatasetCapture.ResetSimulation();

            var annotationDefinitionsPath = Path.Combine(DatasetCapture.OutputDirectory, "annotation_definitions.json");

            Assert.AreEqual(annotationDefinition1, annotationDefinition2);
            Assert.AreEqual(annotationDefinitionGuid, annotationDefinition1.Id);
            Assert.AreEqual(annotationDefinitionGuid, annotationDefinition2.Id);
            AssertJsonFileEquals(annotationDefinitionsJsonExpected, annotationDefinitionsPath, false);
        }

        [Test]
        public void CreateAnnotation_MultipleTimesWithDifferentParameters_WritesProperTypes()
        {
            var annotationDefinitionGuid = new Guid(10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            var annotationDefinitionsJsonExpected =
                $@"{{
  ""version"": ""{DatasetCapture.SchemaVersion}"",
  ""annotation_definitions"": [
    {{
      ""id"": <guid>,
      ""name"": ""name"",
      ""format"": ""json""
    }},
    {{
      ""id"": <guid>,
      ""name"": ""name2"",
      ""description"": ""description"",
      ""format"": ""json""
    }}
  ]
}}";
            var annotationDefinition1 = DatasetCapture.RegisterAnnotationDefinition("name", id: annotationDefinitionGuid);
            var annotationDefinition2 = DatasetCapture.RegisterAnnotationDefinition("name2", description: "description");

            DatasetCapture.ResetSimulation();

            var annotationDefinitionsPath = Path.Combine(DatasetCapture.OutputDirectory, "annotation_definitions.json");

            Assert.AreEqual(annotationDefinitionGuid, annotationDefinition1.Id);
            Assert.AreNotEqual(default(Guid), annotationDefinition2.Id);

            AssertJsonFileEquals(annotationDefinitionsJsonExpected, annotationDefinitionsPath);
        }

        [Test]
        public void ReportMetricValues_WhenCaptureNotExpected_Throws()
        {
            var ego = DatasetCapture.RegisterEgo("");
            var metricDefinition = DatasetCapture.RegisterMetricDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 100);
            Assert.Throws<InvalidOperationException>(() => sensorHandle.ReportMetric(metricDefinition, new int[0]));
        }

        [Test]
        public void ReportMetricAsync_WhenCaptureNotExpected_Throws()
        {
            var ego = DatasetCapture.RegisterEgo("");
            var metricDefinition = DatasetCapture.RegisterMetricDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 100);
            Assert.Throws<InvalidOperationException>(() => sensorHandle.ReportMetricAsync(metricDefinition));
        }

        [Test]
        public void ResetSimulation_WithUnreportedMetricAsync_LogsError()
        {
            var ego = DatasetCapture.RegisterEgo("");
            var metricDefinition = DatasetCapture.RegisterMetricDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 0);
            sensorHandle.ReportMetricAsync(metricDefinition);
            DatasetCapture.ResetSimulation();
            LogAssert.Expect(LogType.Error, new Regex("Simulation ended with pending .*"));
        }

        [Test]
        public void MetricAsyncIsValid_ReturnsProperValue()
        {
            LogAssert.ignoreFailingMessages = true; //we aren't worried about "Simulation ended with pending..."

            var ego = DatasetCapture.RegisterEgo("");
            var metricDefinition = DatasetCapture.RegisterMetricDefinition("");
            var sensorHandle = DatasetCapture.RegisterSensor(ego, "", "", 1, 0);
            var asyncMetric = sensorHandle.ReportMetricAsync(metricDefinition);

            Assert.IsTrue(asyncMetric.IsValid);
            DatasetCapture.ResetSimulation();
            Assert.IsFalse(asyncMetric.IsValid);
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
            var values = new[] { 1 };

            var expectedLine = @"""step"": 0";

            var metricDefinition = DatasetCapture.RegisterMetricDefinition("");
            DatasetCapture.RegisterSensor(DatasetCapture.RegisterEgo(""), "", "", 1, 0);

            yield return null;
            yield return null;
            yield return null;
            DatasetCapture.ReportMetric(metricDefinition, values);
            DatasetCapture.ResetSimulation();

            var text = File.ReadAllText(Path.Combine(DatasetCapture.OutputDirectory, "metrics_000.json"));
            StringAssert.Contains(expectedLine, text);
        }

        [UnityTest]
        public IEnumerator SensorHandleReportMetric_BeforeReportCapture_ReportsProperJson()
        {
            var values = new[] { 1 };

            var expectedLine = @"""step"": 0";

            var metricDefinition = DatasetCapture.RegisterMetricDefinition("");
            var sensor = DatasetCapture.RegisterSensor(DatasetCapture.RegisterEgo(""), "", "", 1, 0);

            yield return null;
            sensor.ReportMetric(metricDefinition, values);
            sensor.ReportCapture("file", new SensorSpatialData(Pose.identity, Pose.identity, null, null));
            DatasetCapture.ResetSimulation();

            var metricsTest = File.ReadAllText(Path.Combine(DatasetCapture.OutputDirectory, "metrics_000.json"));
            var captures = File.ReadAllText(Path.Combine(DatasetCapture.OutputDirectory, "captures_000.json"));
            StringAssert.Contains(expectedLine, metricsTest);
            StringAssert.Contains(expectedLine, captures);
        }

        [Test]
        public void MetricAsyncReportValues_ReportsProperJson(
            [Values(MetricTarget.Global, MetricTarget.Capture, MetricTarget.Annotation)] MetricTarget metricTarget,
            [Values(true, false)] bool async,
            [Values(true, false)] bool asStringJsonArray)
        {
            var values = new[]
            {
                new TestValues()
                {
                    a = "a string",
                    b = 10
                },
                new TestValues()
                {
                    a = "a second string",
                    b = 20
                },
            };

            var expectedMetric = $@"{{
  ""version"": ""0.0.1"",
  ""metrics"": [
    {{
      ""capture_id"": {(metricTarget == MetricTarget.Annotation || metricTarget == MetricTarget.Capture ? "<guid>" : "null")},
      ""annotation_id"": {(metricTarget == MetricTarget.Annotation ? "<guid>" : "null")},
      ""sequence_id"": <guid>,
      ""step"": 0,
      ""metric_definition"": <guid>,
      ""values"": [
        {{
          ""a"": ""a string"",
          ""b"": 10
        }},
        {{
          ""a"": ""a second string"",
          ""b"": 20
        }}
      ]
    }}
  ]
}}";

            var metricDefinition = DatasetCapture.RegisterMetricDefinition("");
            var sensor = DatasetCapture.RegisterSensor(DatasetCapture.RegisterEgo(""), "", "", 1, 0);
            var annotation = sensor.ReportAnnotationFile(DatasetCapture.RegisterAnnotationDefinition(""), "");
            var valuesJsonArray = JArray.FromObject(values).ToString(Formatting.Indented);
            if (async)
            {
                AsyncMetric asyncMetric;
                switch (metricTarget)
                {
                    case MetricTarget.Global:
                        asyncMetric = DatasetCapture.ReportMetricAsync(metricDefinition);
                        break;
                    case MetricTarget.Capture:
                        asyncMetric = sensor.ReportMetricAsync(metricDefinition);
                        break;
                    case MetricTarget.Annotation:
                        asyncMetric = annotation.ReportMetricAsync(metricDefinition);
                        break;
                    default:
                        throw new Exception("unsupported");
                }

                Assert.IsTrue(asyncMetric.IsPending);
                if (asStringJsonArray)
                    asyncMetric.ReportValues(valuesJsonArray);
                else
                    asyncMetric.ReportValues(values);

                Assert.IsFalse(asyncMetric.IsPending);
            }
            else
            {
                switch (metricTarget)
                {
                    case MetricTarget.Global:
                        if (asStringJsonArray)
                            DatasetCapture.ReportMetric(metricDefinition, valuesJsonArray);
                        else
                            DatasetCapture.ReportMetric(metricDefinition, values);
                        break;
                    case MetricTarget.Capture:
                        if (asStringJsonArray)
                            sensor.ReportMetric(metricDefinition, valuesJsonArray);
                        else
                            sensor.ReportMetric(metricDefinition, values);
                        break;
                    case MetricTarget.Annotation:
                        if (asStringJsonArray)
                            annotation.ReportMetric(metricDefinition, valuesJsonArray);
                        else
                            annotation.ReportMetric(metricDefinition, values);
                        break;
                    default:
                        throw new Exception("unsupported");
                }
            }
            DatasetCapture.ResetSimulation();

            AssertJsonFileEquals(expectedMetric, Path.Combine(DatasetCapture.OutputDirectory, "metrics_000.json"), escapeGuids: true, ignoreFormatting: true);
        }

        [Test]
        public void CreateMetric_MultipleTimesWithDifferentParameters_WritesProperTypes()
        {
            var metricDefinitionGuid = new Guid(10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            var metricDefinitionsJsonExpected =
                $@"{{
  ""version"": ""{DatasetCapture.SchemaVersion}"",
  ""metric_definitions"": [
    {{
      ""id"": <guid>,
      ""name"": ""name""
    }},
    {{
      ""id"": <guid>,
      ""name"": ""name2"",
      ""description"": ""description""
    }}
  ]
}}";
            var metricDefinition1 = DatasetCapture.RegisterMetricDefinition("name", id: metricDefinitionGuid);
            var metricDefinition2 = DatasetCapture.RegisterMetricDefinition("name2", description: "description");

            DatasetCapture.ResetSimulation();

            var metricDefinitionsPath = Path.Combine(DatasetCapture.OutputDirectory, "metric_definitions.json");

            Assert.AreEqual(metricDefinitionGuid, metricDefinition1.Id);
            Assert.AreNotEqual(default(Guid), metricDefinition2.Id);

            AssertJsonFileEquals(metricDefinitionsJsonExpected, metricDefinitionsPath);
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

            string filename;
            string jsonContainerName;
            if (additionalInfoKind == AdditionalInfoKind.Annotation)
            {
                DatasetCapture.RegisterAnnotationDefinition("name", specValues);
                filename = "annotation_definitions.json";
                jsonContainerName = "annotation_definitions";
            }
            else
            {
                DatasetCapture.RegisterMetricDefinition("name", specValues);
                filename = "metric_definitions.json";
                jsonContainerName = "metric_definitions";
            }
            var additionalInfoString = (additionalInfoKind == AdditionalInfoKind.Annotation ? @"
      ""format"": ""json""," : null);

            var annotationDefinitionsJsonExpected =
                $@"{{
  ""version"": ""{DatasetCapture.SchemaVersion}"",
  ""{jsonContainerName}"": [
    {{
      ""id"": <guid>,
      ""name"": ""name"",{additionalInfoString}
      ""spec"": [
        {{
          ""label_id"": 1,
          ""label_name"": ""sky"",
          ""pixel_value"": [
            1,
            2,
            3
          ]
        }},
        {{
          ""label_id"": 2,
          ""label_name"": ""sidewalk"",
          ""pixel_value"": [
            4,
            5,
            6
          ]
        }}
      ]
    }}
  ]
}}";
            DatasetCapture.ResetSimulation();

            var annotationDefinitionsPath = Path.Combine(DatasetCapture.OutputDirectory, filename);

            AssertJsonFileEquals(annotationDefinitionsJsonExpected, annotationDefinitionsPath);
        }

        static void AssertJsonFileEquals(string jsonExpected, string jsonPath, bool escapeGuids = true, bool ignoreFormatting = false)
        {
            FileAssert.Exists(jsonPath);
            var jsonActual = File.ReadAllText(jsonPath);
            if (escapeGuids)
                jsonActual = EscapeGuids(jsonActual);


            jsonActual = TestHelper.NormalizeJson(jsonActual, ignoreFormatting);
            jsonExpected = TestHelper.NormalizeJson(jsonExpected, ignoreFormatting);

            Assert.AreEqual(jsonExpected, jsonActual, $"Expected:\n{jsonExpected}\nActual:\n{jsonActual}");
        }

        static string EscapeGuids(string text)
        {
            var result = Regex.Replace(text, @"""[a-z0-9]*-[a-z0-9]*-[a-z0-9]*-[a-z0-9]*-[a-z0-9]*""", "<guid>");
            result = TestHelper.NormalizeJson(result);
            return result;
        }
    }
}
