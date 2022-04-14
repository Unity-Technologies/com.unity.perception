using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class PerceptionFormatTest
    {
        PerceptionEndpoint m_Endpoint;

        [SetUp]
        public void Init()
        {
            m_Endpoint = new PerceptionEndpoint();
            DatasetCapture.OverrideEndpoint(m_Endpoint);
        }

        [TearDown]
        public void Cleanup()
        {
            Debug.Log($"Filename was: {m_Endpoint.currentPath}");

            if (Directory.Exists(m_Endpoint.currentPath))
                Directory.Delete(m_Endpoint.currentPath, true);
            DatasetCapture.OverrideEndpoint(null);
        }

        static (RgbSensorDefinition, SensorHandle) RegisterSensor(string id, string modality, string sensorDescription, int firstCaptureFrame, CaptureTriggerMode captureTriggerMode, float simDeltaTime, int framesBetween, bool affectTiming = false)
        {
            var sensorDefinition = new RgbSensorDefinition(id, modality, sensorDescription)
            {
                firstCaptureFrame = firstCaptureFrame,
                captureTriggerMode = captureTriggerMode,
                simulationDeltaTime = simDeltaTime,
                framesBetweenCaptures = framesBetween,
                manualSensorsAffectTiming = affectTiming
            };
            return (sensorDefinition, DatasetCapture.RegisterSensor(sensorDefinition));
        }

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

            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var (sensorDef, sensorHandle) = RegisterSensor(id, modality, def, firstFrame, mode, delta, framesBetween);
            Assert.IsTrue(sensorHandle.IsValid);

            yield return null;

            DatasetCapture.ResetSimulation();
            var sensorJsonExpected =
                $@"{{
  ""version"": ""{PerceptionEndpoint.version}"",
  ""sensors"": [
    {{
      ""id"": ""camera"",
      ""modality"": ""{modality}"",
      ""description"": ""{def}""
    }}
  ]
}}";

            PathUtils.CombineUniversal(m_Endpoint.datasetPath, "sensors.json");
            var sensorsPath = PathUtils.CombineUniversal(m_Endpoint.datasetPath, "sensors.json");

            FileAssert.Exists(sensorsPath);

            AssertJsonFileEquals(sensorJsonExpected, sensorsPath);
        }

        (RgbSensor, string) CreateMocRgbCapture(RgbSensorDefinition def, int frame)
        {
            var name = "camera";
            var position = new float3(.2f, 1.1f, .3f);
            var frameId = $"frame_{frame}";
            var rotation = new Quaternion(.3f, .2f, .1f, .5f);
            var velocity = new Vector3(.1f, .2f, .3f);
            var intrinsics = new float3x3(.1f, .2f, .3f, 1f, 2f, 3f, 10f, 20f, 30f);

            var sensor = new RgbSensor(def, position, rotation, velocity, Vector3.zero)
            {
                matrix = intrinsics,
                buffer = Array.Empty<byte>()
            };

            var capturesJsonExpected =
                $@"{{
  ""version"": ""{PerceptionEndpoint.version}"",
  ""captures"": [
    {{
      ""id"": ""<guid>"",
      ""sequence_id"": ""<guid>"",
      ""step"": 0,
      ""timestamp"": 0.0,
      ""sensor"": {{
        ""sensor_id"": ""{name}"",
        ""ego_id"": ""ego"",
        ""modality"": ""{name}"",
        ""translation"": [
          {Format(position.x)},
          {Format(position.y)},
          {Format(position.z)}
        ],
        ""rotation"": [
          {Format(rotation.x)},
          {Format(rotation.y)},
          {Format(rotation.z)},
          {Format(rotation.w)}
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
        ],
        ""projection"": ""perspective""
      }},
      ""ego"": {{
        ""ego_id"": ""ego"",
        ""translation"": [
          0.0,
          0.0,
          0.0
        ],
        ""rotation"": [
          0.0,
          0.0,
          0.0,
          1.0
        ],
        ""velocity"": null,
        ""acceleration"": null
      }},
      ""filename"": ""RGB<guid>/rgb_{frame}.png"",
      ""format"": ""PNG"",
      ""annotations"": []
    }}
  ]
}}";

            return (sensor, capturesJsonExpected);
        }

        [UnityTest]
        public IEnumerator ReportCapture_ReportsProperJson()
        {
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var (sensorDef, sensorHandle) = RegisterSensor("camera", "camera", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var (sensor, json) = CreateMocRgbCapture(sensorDef, DatasetCapture.currentSimulation.currentFrame);
            sensorHandle.ReportSensor(sensor);

            yield return null;
            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            var capturesPath = PathUtils.CombineUniversal(m_Endpoint.datasetPath, "captures_000.json");

            FileAssert.Exists(capturesPath);

            AssertJsonFileEquals(json, capturesPath);
        }

        [UnityTest]
        public IEnumerator ReportMultipleCameras_ReportsProperJson()
        {
            // Need to reset simulation so that the override endpoint is used
            DatasetCapture.ResetSimulation();

            var (def1, sensorHandle1) = RegisterSensor("camera1", "camera", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var (def2, sensorHandle2) = RegisterSensor("camera2", "camera", "", 0, CaptureTriggerMode.Scheduled, 1, 0);

            var (sensor1, sensor2, json) = CreateMocRgbCapture2(def1, def2, DatasetCapture.currentSimulation.currentFrame);

            sensorHandle1.ReportSensor(sensor1);
            sensorHandle2.ReportSensor(sensor2);

            yield return null;
            DatasetCapture.ResetSimulation();

            Assert.IsFalse(sensorHandle1.IsValid);
            Assert.IsFalse(sensorHandle2.IsValid);

            var capturesPath = PathUtils.CombineUniversal(m_Endpoint.datasetPath, "captures_000.json");

            FileAssert.Exists(capturesPath);

            AssertJsonFileEquals(json, capturesPath);
        }

        [UnityTest]
        public IEnumerator ReportAnnotation_AddsProperJsonToCapture()
        {
            DatasetCapture.ResetSimulation();

            var (sensorDef, sensorHandle) = RegisterSensor("camera", "camera", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var sensor = new RgbSensor(sensorDef, Vector3.zero, Quaternion.identity)
            {
                buffer = Array.Empty<byte>()
            };

            sensorHandle.ReportSensor(sensor);

            var id = Guid.NewGuid().ToString();

            var def = new SemanticSegmentationDefinition(id, new[]
            {
                new SemanticSegmentationDefinitionEntry()
                {
                    labelName = "Box",
                    pixelValue = new Color(1.0f, 0.0f, 0.0f, 1.0f)
                }
            });
            var annotation = new SemanticSegmentationAnnotation(
                def, sensorHandle.Id, ImageEncodingFormat.Png, Vector2.zero, new List<SemanticSegmentationDefinitionEntry>(), Array.Empty<byte>());

            DatasetCapture.RegisterAnnotationDefinition(def);
            sensorHandle.ReportAnnotation(def, annotation);
            var frameAtRecord = DatasetCapture.currentFrame;

            yield return null;
            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

           var annotationDefinitionsJsonExpected =
                $@"{{
  ""version"": ""{PerceptionEndpoint.version}"",
  ""annotation_definitions"": [
    {{
      ""id"": ""<guid>"",
      ""name"": ""semantic segmentation"",
      ""description"": ""{def.description}"",
      ""format"": ""PNG"",
      ""spec"": [
        {{
          ""label_name"": ""Box"",
          ""pixel_value"": {{
            ""r"": 1.0,
            ""g"": 0.0,
            ""b"": 0.0,
            ""a"": 1.0
          }}
        }}
      ]
    }}
  ]
}}";
            var annotationsJsonExpected =
                $@"      ""annotations"": [
        {{
          ""id"": ""<guid>"",
          ""annotation_definition"": ""<guid>"",
          ""filename"": ""SemanticSegmentation<guid>/segmentation_{frameAtRecord}.png""
        }}
      ]";

            var annotationDefinitionsPath = PathUtils.CombineUniversal(m_Endpoint.datasetPath, "annotation_definitions.json");
            var capturesPath = PathUtils.CombineUniversal(m_Endpoint.datasetPath, "captures_000.json");

            AssertJsonFileEquals(annotationDefinitionsJsonExpected, annotationDefinitionsPath);

            FileAssert.Exists(capturesPath);

            var b = EscapeGuids(File.ReadAllText(capturesPath));

            StringAssert.Contains("\"id\": \"<guid>\"", b);
            StringAssert.Contains("\"annotation_definition\": \"<guid>\"", b);
            StringAssert.Contains($"\"filename\": \"SemanticSegmentation<guid>/segmentation_{frameAtRecord}.png\"", b);
        }

        class TestMetricDef : MetricDefinition
        {
            public TestMetricDef() : base("test", "counting things") {}
        }

        [UnityTest]
        public IEnumerator MetricReportValues_WithNoReportsInFrames_DoesNotIncrementStep()
        {
            DatasetCapture.ResetSimulation();

            var def = new TestMetricDef();
            DatasetCapture.RegisterMetric(def);

            const string expectedLine = @"""step"": 0";

            var (sensorDef, sensorHandle) = RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);

            yield return null;
            yield return null;
            yield return null;
            var metric = new GenericMetric(1, def);
            DatasetCapture.ReportMetric(def, metric);

            DatasetCapture.ResetSimulation();

            var text = File.ReadAllText(PathUtils.CombineUniversal(m_Endpoint.datasetPath, "metrics_000.json"));
            StringAssert.Contains(expectedLine, text);
        }

        [UnityTest]
        public IEnumerator SensorHandleReportMetric_BeforeReportCapture_ReportsProperJson()
        {
            DatasetCapture.ResetSimulation();

            const string expectedLine = @"""step"": 0";

            var def = new TestMetricDef();
            DatasetCapture.RegisterMetric(def);

            var (sensorDef, sensorHandle) = RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);

            yield return null;

            var vals = new[] { 1 };
            var metric = new GenericMetric(vals, def);
            sensorHandle.ReportMetric(def, metric);
            var sensor = new RgbSensor(sensorDef, Vector3.zero, Quaternion.identity)
            {
                buffer = Array.Empty<byte>()
            };
            sensorHandle.ReportSensor(sensor);

            var path = m_Endpoint.datasetPath;

            DatasetCapture.ResetSimulation();

            var metricsTest = File.ReadAllText(PathUtils.CombineUniversal(path, "metrics_000.json"));
            var captures = File.ReadAllText(PathUtils.CombineUniversal(path, "captures_000.json"));
            StringAssert.Contains(expectedLine, metricsTest);
            StringAssert.Contains(expectedLine, captures);
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
            var pattern = @"[a-z0-9]{8}-[a-z0-9]*-[a-z0-9]*-[a-z0-9]*-[a-z0-9]*";
            var result = Regex.Replace(text, pattern, "<guid>");
            result = TestHelper.NormalizeJson(result);
            return result;
        }

        static string Format(float value)
        {
            var result = value.ToString("R", CultureInfo.InvariantCulture);
            if (!result.Contains("."))
                return result + ".0";

            return result;
        }

        (RgbSensor, RgbSensor, string) CreateMocRgbCapture2(RgbSensorDefinition def1, RgbSensorDefinition def2, int frame)
        {
            var rotation = new Quaternion(.3f, .2f, .1f, .5f);
            var velocity = new Vector3(.1f, .2f, .3f);
            var intrinsics = new float3x3(.1f, .2f, .3f, 1f, 2f, 3f, 10f, 20f, 30f);

            var name1 = "camera1";
            var name2 = "camera2";
            var pos1 = Vector3.zero;
            var pos2 = Vector3.one;

            var sensor1 = new RgbSensor(def1, pos1, rotation, velocity, Vector3.zero)
            {
                matrix = intrinsics,
                buffer = Array.Empty<byte>()
            };

            var sensor2 = new RgbSensor(def2, pos2, rotation, velocity, Vector3.zero)
            {
                matrix = intrinsics,
                buffer = Array.Empty<byte>()
            };

            var capturesJsonExpected =
                $@"{{
  ""version"": ""{PerceptionEndpoint.version}"",
  ""captures"": [
    {{
      ""id"": ""<guid>"",
      ""sequence_id"": ""<guid>"",
      ""step"": 0,
      ""timestamp"": 0.0,
      ""sensor"": {{
        ""sensor_id"": ""{name1}"",
        ""ego_id"": ""ego"",
        ""modality"": ""camera"",
        ""translation"": [
          {Format(pos1.x)},
          {Format(pos1.y)},
          {Format(pos1.z)}
        ],
        ""rotation"": [
          {Format(rotation.x)},
          {Format(rotation.y)},
          {Format(rotation.z)},
          {Format(rotation.w)}
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
        ],
        ""projection"": ""perspective""
      }},
      ""ego"": {{
        ""ego_id"": ""ego"",
        ""translation"": [
          0.0,
          0.0,
          0.0
        ],
        ""rotation"": [
          0.0,
          0.0,
          0.0,
          1.0
        ],
        ""velocity"": null,
        ""acceleration"": null
      }},
      ""filename"": ""RGB<guid>/rgb_{frame}.png"",
      ""format"": ""PNG"",
      ""annotations"": []
    }},
    {{
      ""id"": ""<guid>"",
      ""sequence_id"": ""<guid>"",
      ""step"": 0,
      ""timestamp"": 0.0,
      ""sensor"": {{
        ""sensor_id"": ""{name2}"",
        ""ego_id"": ""ego"",
        ""modality"": ""camera"",
        ""translation"": [
          {Format(pos2.x)},
          {Format(pos2.y)},
          {Format(pos2.z)}
        ],
        ""rotation"": [
          {Format(rotation.x)},
          {Format(rotation.y)},
          {Format(rotation.z)},
          {Format(rotation.w)}
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
        ],
        ""projection"": ""perspective""
      }},
      ""ego"": {{
        ""ego_id"": ""ego"",
        ""translation"": [
          0.0,
          0.0,
          0.0
        ],
        ""rotation"": [
          0.0,
          0.0,
          0.0,
          1.0
        ],
        ""velocity"": null,
        ""acceleration"": null
      }},
      ""filename"": ""RGB<guid>/rgb_{frame}.png"",
      ""format"": ""PNG"",
      ""annotations"": []
    }}
  ]
}}";

            return (sensor1, sensor2, capturesJsonExpected);
    }
    }
}
