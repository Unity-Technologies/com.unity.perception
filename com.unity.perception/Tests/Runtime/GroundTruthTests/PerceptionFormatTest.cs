using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Rendering;
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

        PerceptionCamera SetupCamera(IdLabelConfig config)
        {
            var cameraObject = new GameObject();
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 60;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000;

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = true;
            return perceptionCamera;
        }

        [UnityTest]
        public IEnumerator AddNewLabelerTest_ReportsCorrectOutput()
        {
            DatasetCapture.ResetSimulation();

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "camera", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var sensor = new RgbSensor(sensorDef, Vector3.zero, Quaternion.identity);
            var def = new CustomLabeler.CustomDefinition();

            var cfg = ScriptableObject.CreateInstance<IdLabelConfig>();
            cfg.Init(new List<IdLabelEntry> {new IdLabelEntry {id = 1, label = "test"}});

            var labeler = new CustomLabeler(cfg);

            var cam = SetupCamera(cfg);
            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();
            Assert.IsFalse(sensorHandle.IsValid);

            // Verify that annotation was written to annotation_definitions.json
            var annDefPath = PathUtils.CombineUniversal(m_Endpoint.datasetPath, "annotation_definitions.json");
            FileAssert.Exists(annDefPath);

            var annDef = JObject.Parse(File.ReadAllText(annDefPath));

            Assert.IsTrue(annDef.ContainsKey("annotation_definitions"));
            var sub = annDef["annotation_definitions"];

            var expectedDef = new JArray
            {
                new JObject
                {
                    {"@type", "custom.type"},
                    {"id", "TestTestTest"},
                    { "description", "labeler" },
                    { "spec", new JArray
                      {
                          new JObject
                          {
                              { "label_id", 1 },
                              { "label_name", "test" }
                          }
                      }}
                }
            };

            Assert.IsTrue(JToken.DeepEquals(sub, expectedDef));

            var capturesPath = PathUtils.CombineUniversal(m_Endpoint.datasetPath, "captures_000.json");

            FileAssert.Exists(capturesPath);

            var cap = JObject.Parse(File.ReadAllText(capturesPath));
            var a = cap["captures"][0]["annotations"][0];

            var expectedAnn = new JObject
            {
                { "@type", "custom.type" },
                { "id", "TestTestTest" },
                { "sensorId", "camera" },
                { "description", "labeler" },
                { "values", new JArray
                  {
                      new JObject
                      {
                          { "instanceId", 0 },
                          { "labelId", 1 },
                          { "labelName", "test"},
                          { "vals", new JArray {0, 17, 42 } }
                      }
                  }}
            };

            Assert.IsTrue(JToken.DeepEquals(a, expectedAnn));

            UnityEngine.Object.DestroyImmediate(cam.gameObject);
        }

        [Test]
        [Ignore("Interacts with PlayerPrefs, which causes other tests to fail")]
        public void PerceptionEndpointIsValidTest()
        {
            // Create a directory, verify that endpoint is valid
            var newDir = Guid.NewGuid().ToString();
            var p = PathUtils.CombineUniversal(m_Endpoint.currentPath, newDir);
            Directory.CreateDirectory(p);

            m_Endpoint.basePath = p;
            Assert.IsTrue(m_Endpoint.IsValid(out _));

            // Delete that directory, verify that endpoint is not longer valid
            Directory.Delete(p);

            Assert.IsFalse(m_Endpoint.IsValid(out _));
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

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor(id, modality, def, firstFrame, mode, delta, framesBetween);
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

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "camera", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var(sensor, json) = CreateMocRgbCapture(sensorDef, Time.frameCount);
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

            var(def1, sensorHandle1) = TestHelper.RegisterSensor("camera1", "camera", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var(def2, sensorHandle2) = TestHelper.RegisterSensor("camera2", "camera", "", 0, CaptureTriggerMode.Scheduled, 1, 0);

            var(sensor1, sensor2, json) = CreateMocRgbCapture2(def1, def2, Time.frameCount);

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

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "camera", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
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
            var frameAtRecord = Time.frameCount;

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

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);

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

            var(sensorDef, sensorHandle) = TestHelper.RegisterSensor("camera", "", "", 0, CaptureTriggerMode.Scheduled, 1, 0);

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

    internal class CustomLabeler : CameraLabeler
    {
        internal struct CustomLabel : IMessageProducer
        {
            public int labelId { get; set; }
            public string labelName { get; set; }
            public uint instanceId { get; set; }
            public int[] vals { get; set; }

            public void ToMessage(IMessageBuilder builder)
            {
                builder.AddInt("instanceId", (int)instanceId);
                builder.AddInt("labelId", labelId);
                builder.AddString("labelName", labelName);
                builder.AddIntArray("vals", vals);
            }
        }

        internal class CustomDefinition : AnnotationDefinition
        {
            internal const string myDescription = "labeler";
            internal const string sId = "test.def";

            public CustomDefinition() : base(sId) {}

            public override string modelType => "custom.type";
            public override string description => myDescription;

            public IdLabelConfig.LabelEntrySpec[] spec { get; }

            public CustomDefinition(string id, IdLabelConfig.LabelEntrySpec[] spec)
                : base(id)
            {
                this.spec = spec;
            }

            public override void ToMessage(IMessageBuilder builder)
            {
                base.ToMessage(builder);
                foreach (var e in spec)
                {
                    var nested = builder.AddNestedMessageToVector("spec");
                    e.ToMessage(nested);
                }
            }
        }

        internal class CustomAnnotation : Annotation
        {
            readonly List<CustomLabel> values;

            public CustomAnnotation(AnnotationDefinition definition, string sensorId, List<CustomLabel> values)
                : base(definition, sensorId)
            {
                this.values = values;
            }

            public override void ToMessage(IMessageBuilder builder)
            {
                base.ToMessage(builder);
                foreach (var v in values)
                {
                    var nested = builder.AddNestedMessageToVector("values");
                    v.ToMessage(nested);
                }
            }

            public override bool IsValid() => true;
        }

        public override string description => CustomDefinition.myDescription;
        public override string labelerId => "TestTestTest";
        protected override bool supportsVisualization => false;
        public IdLabelConfig idLabelConfig;
        CustomDefinition m_Definition;

        public CustomLabeler()
        {
        }

        public CustomLabeler(IdLabelConfig cfg)
        {
            idLabelConfig = cfg;
        }

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("IdLabelConfig field must be assigned");

            m_Definition = new CustomDefinition(labelerId, idLabelConfig.GetAnnotationSpecification());

            DatasetCapture.RegisterAnnotationDefinition(m_Definition);

            visualizationEnabled = supportsVisualization;
        }

        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            var label = new CustomLabel();
            label.instanceId = 0;
            label.labelId = 1;
            label.labelName = "test";
            label.vals = new[] { 0, 17, 42 };

            var annotation = new CustomAnnotation(m_Definition, perceptionCamera.id, new List<CustomLabel> { label });
            perceptionCamera.SensorHandle.ReportAnnotation(m_Definition, annotation);
        }
    }
}
