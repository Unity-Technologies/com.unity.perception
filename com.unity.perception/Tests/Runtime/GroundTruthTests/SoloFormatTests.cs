using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class SoloFormatTests : GroundTruthTestBase
    {
        SoloEndpoint m_Endpoint;

        [UnitySetUp]
        public override IEnumerator Init()
        {
            m_Endpoint = new SoloEndpoint();
            DatasetCapture.OverrideEndpoint(m_Endpoint);
            DatasetCapture.ResetSimulation();
            yield return null;
        }

        [TearDown]
        public void CleanUpDataset()
        {
            Debug.Log($"Filename was: {m_Endpoint.currentPath}");

            if (Directory.Exists(m_Endpoint.currentPath))
                Directory.Delete(m_Endpoint.currentPath, true);
        }

        static GameObject CreateLabeledCube()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetPositionAndRotation(new Vector3(0, 0, 30), quaternion.identity);
            cube.transform.localScale = new Vector3(5, 5, 5);
            var labeling = cube.AddComponent<Labeling>();
            labeling.labels.Add("test");
            return cube;
        }

        PerceptionCamera SetupCamera(IdLabelConfig config, RenderTexture renderTexture = null)
        {
            var cameraObject = new GameObject("camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 60;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000;

            if (renderTexture)
                camera.targetTexture = renderTexture;

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = true;
            return perceptionCamera;
        }

        (PerceptionCamera, IdLabelConfig, GameObject) SetupTestScene(RenderTexture texture = null)
        {
            var cfg = ScriptableObject.CreateInstance<IdLabelConfig>();
            cfg.Init(new List<IdLabelEntry> {new IdLabelEntry {id = 1, label = "test"}});
            var cam = SetupCamera(cfg, texture);
            var cube = CreateLabeledCube();

            var sensorDef = TestHelper.CreateSensorDefinition("camera", "camera", "", 0, CaptureTriggerMode.Scheduled, 1, 0);
            var sensor = new RgbSensor(sensorDef, Vector3.zero, Quaternion.identity);

            AddTestObjectForCleanup(cube);
            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cfg);

            return (cam, cfg, cube);
        }

        void TestMetadata(string annName, string annType)
        {
            var mdPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "metadata.json");
            FileAssert.Exists(mdPath);

            var md = JObject.Parse(File.ReadAllText(mdPath));

            Assert.IsTrue(md.ContainsKey("annotators"));
            var a = md["annotators"];

            var expectedMd = new JArray
            {
                new JObject
                {
                    { "name", annName},
                    { "type", annType}
                }
            };

            Assert.IsTrue(JToken.DeepEquals(expectedMd, a));
        }

        void TestMetricMetadata(string metricName)
        {
            var mdPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "metadata.json");
            FileAssert.Exists(mdPath);

            var md = JObject.Parse(File.ReadAllText(mdPath));

            Assert.IsTrue(md.ContainsKey("metricCollectors"));
            var a = md["metricCollectors"];

            var expectedMd = new JArray { metricName };

            Assert.IsTrue(JToken.DeepEquals(expectedMd, a));
        }

        void TestDefintionKeypoints(string annName, string annType, string annDesc)
        {
            var defPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "annotation_definitions.json");
            FileAssert.Exists(defPath);

            var def = JObject.Parse(File.ReadAllText(defPath));

            Assert.IsTrue(def.ContainsKey("annotationDefinitions"));
            var d = def["annotationDefinitions"];

            var keypoints = new JArray
            {
                new JObject
                {
                    { "label", "FrontLowerLeft" },
                    { "index", 0 },
                    { "color", new JArray { 0, 0, 0, 255 } }
                },
                new JObject
                {
                    { "label", "FrontUpperLeft" },
                    { "index", 1 },
                    { "color", new JArray { 0, 0, 0, 255 } }
                },
            };

            var skeleton = new JArray
            {
                new JObject
                {
                    { "joint1", 0 },
                    { "joint2", 1 },
                    { "color", new JArray { 255, 0, 255, 255 } },
                },
                new JObject
                {
                    { "joint1", 1 },
                    { "joint2", 2 },
                    { "color", new JArray { 255, 0, 255, 255 } },
                }
            };

            var expectedDef = new JArray
            {
                new JObject
                {
                    { "@type", annType },
                    { "id", annName },
                    { "description", annDesc },
                    { "template", new JObject
                      {
                          { "templateId", "test_template_id" },
                          { "templateName", "test_template" },
                          { "keypoints", keypoints },
                          { "skeleton", skeleton }
                      }}
                }
            };

            Assert.IsTrue(JToken.DeepEquals(expectedDef, d));
        }

        void TestDefintionSimpleConfig(string annName, string annType, string annDesc)
        {
            var defPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "annotation_definitions.json");
            FileAssert.Exists(defPath);

            var def = JObject.Parse(File.ReadAllText(defPath));

            Assert.IsTrue(def.ContainsKey("annotationDefinitions"));
            var d = def["annotationDefinitions"];

            var expectedDef = new JArray
            {
                new JObject
                {
                    { "@type", annType },
                    { "id", annName },
                    { "description", annDesc },
                    {
                        "spec", new JArray
                        {
                            new JObject
                            {
                                { "label_id", 1 },
                                { "label_name", "test" }
                            }
                        }
                    }
                }
            };

            Assert.IsTrue(JToken.DeepEquals(expectedDef, d));
        }

        void TestDefintionNoLabelConfig(string annName, string annType, string annDesc)
        {
            var defPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "annotation_definitions.json");
            FileAssert.Exists(defPath);

            var def = JObject.Parse(File.ReadAllText(defPath));

            Assert.IsTrue(def.ContainsKey("annotationDefinitions"));
            var d = def["annotationDefinitions"];

            var expectedDef = new JArray
            {
                new JObject
                {
                    { "@type", annType },
                    { "id", annName },
                    { "description", annDesc }
                }
            };

            Assert.IsTrue(JToken.DeepEquals(expectedDef, d));
        }

        void TestMetricDefintionConfig(string annName, string annType, string annDesc)
        {
            var defPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "metric_definitions.json");
            FileAssert.Exists(defPath);

            var def = JObject.Parse(File.ReadAllText(defPath));

            Assert.IsTrue(def.ContainsKey("metricDefinitions"));
            var d = def["metricDefinitions"];

            var expectedDef = new JArray
            {
                new JObject
                {
                    { "@type", annType },
                    { "id", annName },
                    { "description", annDesc }
                }
            };

            Assert.IsTrue(JToken.DeepEquals(expectedDef, d));
        }

        void TestResults(string annName, string annType, string annDesc, JArray values)
        {
            var dataPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "sequence.0", "step0.frame_data.json");
            FileAssert.Exists(dataPath);

            var data = JObject.Parse(File.ReadAllText(dataPath));

            Assert.IsTrue(data.ContainsKey("captures"));
            var cap = data["captures"][0] as JObject;
            Assert.IsTrue(cap.ContainsKey("annotations"));
            var sub = cap["annotations"];

            var expected = new JArray
            {
                new JObject
                {
                    { "@type", annType },
                    { "id", annName },
                    { "sensorId", "camera" },
                    { "description", annDesc },
                    {
                        "values", values
                    }
                }
            };

            Assert.IsTrue(JToken.DeepEquals(expected, sub));
        }

        void TestResults_Keypoints(int instanceId, string annName, string annType, string annDesc)
        {
            var dataPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "sequence.0", "step0.frame_data.json");
            FileAssert.Exists(dataPath);

            var data = JObject.Parse(File.ReadAllText(dataPath));

            Assert.IsTrue(data.ContainsKey("captures"));
            var cap = data["captures"][0] as JObject;
            Assert.IsTrue(cap.ContainsKey("annotations"));
            var sub = cap["annotations"];

            var keypoints = new JArray
            {
                new JObject
                {
                    { "index", 0 },
                    { "location", new JArray { 451.535675, 444.4643 } },
                    { "cameraCartesianLocation", new JArray { -2.5, -2.5, 27.5 } },
                    { "state", 2 }
                },
                new JObject
                {
                    { "index", 1 },
                    { "location", new JArray { 451.535675, 323.5357 } },
                    { "cameraCartesianLocation", new JArray { -2.5, 2.5, 27.5 } },
                    { "state", 2 }
                }
            };

            var values = new JArray
            {
                new JObject
                {
                    { "instanceId", instanceId },
                    { "labelId", 1 },
                    { "pose", "unset" },
                    { "keypoints", keypoints }
                }
            };

            var expected = new JArray
            {
                new JObject
                {
                    { "@type", annType },
                    { "id", annName },
                    { "sensorId", "camera" },
                    { "description", annDesc },
                    {"templateId", "test_template_id"},
                    {
                        "values", values
                    }
                }
            };

            Assert.IsTrue(JToken.DeepEquals(expected, sub));
        }

        void TestResults_Segmentation(string annName, string annType, string annDesc, LosslessImageEncodingFormat imgFormat, JArray instances = null)
        {
            var dataPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "sequence.0", "step0.frame_data.json");
            FileAssert.Exists(dataPath);

            var data = JObject.Parse(File.ReadAllText(dataPath));

            Assert.IsTrue(data.ContainsKey("captures"));
            var cap = data["captures"][0] as JObject;
            Assert.IsTrue(cap.ContainsKey("annotations"));
            var sub = cap["annotations"];


            var expected = new JArray
            {
                new JObject
                {
                    { "@type", annType },
                    { "id", annName },
                    { "sensorId", "camera" },
                    { "description", annDesc },
                    { "imageFormat", imgFormat.ToString() },
                    { "dimension", new JArray { 1024.0, 768.0 }},
                    { "filename", $"step0.camera.{annName}.{imgFormat.ToString().ToLower()}"},
                }
            };

            if (instances != null)
            {
                expected[0]["instances"] = instances;
            }

            Assert.IsTrue(JToken.DeepEquals(expected, sub));

            // Verify that a file was written to disk
            var annPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "sequence.0", $"step0.camera.{annName}.{imgFormat.ToString().ToLower()}");
            FileAssert.Exists(annPath);
        }

        void TestMetricResults(string annName, string annType, string annDesc, JArray values)
        {
            var dataPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "sequence.0", "step0.frame_data.json");
            FileAssert.Exists(dataPath);

            var data = JObject.Parse(File.ReadAllText(dataPath));

            Assert.IsTrue(data.ContainsKey("metrics"));
            var sub = data["metrics"];

            var expected = new JArray
            {
                new JObject
                {
                    { "@type", annType },
                    { "id", annName },
                    { "sensorId", "camera" },
                    { "annotationId", "" },
                    { "description", annDesc },
                    { "values", values }
                }
            };

            Assert.IsTrue(JToken.DeepEquals(expected, sub));
        }

        void TestSensorDefinitionResults(string sensorName, string sensorType)
        {
            var dataPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "sensor_definitions.json");
            FileAssert.Exists(dataPath);

            var data = JObject.Parse(File.ReadAllText(dataPath));

            Assert.IsTrue(data.ContainsKey("sensorDefinitions"));
            var sub = data["sensorDefinitions"];

            var expected = new JArray
            {
                new JObject
                {
                    { "@type", sensorType },
                    { "id", sensorName },
                    { "modality", sensorName},
                    {"description", null},
                    {"firstCaptureFrame", 0.0},
                    {"captureTriggerMode", "Scheduled"},
                    {"simulationDeltaTime", 0.0166},
                    {"framesBetweenCaptures", 0},
                    {"manualSensorsAffectTiming", false}
                }
            };

            Assert.IsTrue(JToken.DeepEquals(expected, sub));
        }

        void TestSensorMetadata()
        {
            var dataPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "metadata.json");
            FileAssert.Exists(dataPath);

            var data = JObject.Parse(File.ReadAllText(dataPath));

            Assert.IsTrue(data.ContainsKey("unityVersion"));
            Assert.IsTrue(data.ContainsKey("perceptionVersion"));
            Assert.IsTrue(data.ContainsKey("renderPipeline"));
            Assert.IsTrue(data.ContainsKey("simulationStartTime"));
            Assert.IsTrue(data.ContainsKey("totalFrames"));
            Assert.IsTrue(data.ContainsKey("totalSequences"));
            Assert.IsTrue(data.ContainsKey("sensors"));
            Assert.IsTrue(data.ContainsKey("metricCollectors"));
            Assert.IsTrue(data.ContainsKey("simulationEndTime"));
            Assert.IsTrue(JToken.DeepEquals(1, data["totalFrames"]));
            Assert.IsTrue(JToken.DeepEquals(1, data["totalSequences"]));
            var expected = new JArray { "camera" };
            Assert.IsTrue(JToken.DeepEquals(expected, data["sensors"]));
        }

        [UnityTest]
        public IEnumerator TestRgbCaptureSolo()
        {
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();

            DatasetCapture.ResetSimulation();

            var(cam, cfg, _) = SetupTestScene(texture);

            yield return null;
            DatasetCapture.ResetSimulation();

            var sensorName = "camera";
            var sensorType = "type.unity.com/unity.solo.RGBCamera";

            TestSensorDefinitionResults(sensorName, sensorType);
            TestSensorMetadata();

            var dataPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "sequence.0", "step0.frame_data.json");
            FileAssert.Exists(dataPath);

            var data = JObject.Parse(File.ReadAllText(dataPath));

            Assert.IsTrue(data.ContainsKey("captures"));
            var cap = data["captures"][0] as JObject;

            var expected =
                new JObject
            {
                { "@type", sensorType },
                { "id", sensorName },
                { "description", null },
                { "position", new JArray { 0.0, 0.0, 0.0 } },
                { "rotation", new JArray { 0.0, 0.0, 0.0, 1.0 } },
                { "velocity", new JArray { 0.0, 0.0, 0.0 } },
                { "acceleration", new JArray { 0.0, 0.0, 0.0 } },
                { "filename", $"step0.{sensorName}.png" },
                { "imageFormat", "Png" },
                { "dimension", new JArray { 1024.0, 768.0 } },
                { "projection", "Perspective" },
                {
                    "matrix", new JArray
                    {
                        1.299038,
                        0.0,
                        0.0,
                        0.0,
                        1.73205078,
                        0.0,
                        0.0,
                        0.0,
                        -1.0006001
                    }
                }
            };

            Assert.IsTrue(JToken.DeepEquals(expected, cap));

            // Verify that a file was written to disk
            var annPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "sequence.0", $"step0.{sensorName}.png");
            FileAssert.Exists(annPath);

            texture.Release();
        }

        [UnityTest]
        public IEnumerator TestBoundingBox3DSolo()
        {
            DatasetCapture.ResetSimulation();

            var(cam, cfg, cube) = SetupTestScene();

            var labeler = new BoundingBox3DLabeler(cfg);
            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();

            var annName = "bounding box 3D";
            var annType = "type.unity.com/unity.solo.BoundingBox3DAnnotation";
            var annDesc = "Produces 3D bounding box ground truth data for all visible objects that bear a label defined in this labeler's associated label configuration.";

            var instanceId = cube.GetComponent<Labeling>().instanceId;

            var values = new JArray
            {
                new JObject
                {
                    { "instanceId", instanceId },
                    { "labelId", 1 },
                    { "labelName", "test" },
                    { "translation", new JArray { 0.0, 0.0, 30.0 } },
                    { "size", new JArray { 5.0, 5.0, 5.0 } },
                    { "rotation", new JArray { 0.0, 0.0, 0.0, 1.0 } },
                    { "velocity", new JArray { 0.0, 0.0, 0.0 } },
                    { "acceleration", new JArray { 0.0, 0.0, 0.0 } }
                }
            };

            TestMetadata(annName, annType);
            TestDefintionSimpleConfig(annName, annType, annDesc);
            TestResults(annName, annType, annDesc, values);
        }

        [UnityTest]
        public IEnumerator TestBoundingBox2DSolo()
        {
            TearDown();
            DatasetCapture.ResetSimulation();

            // Since we will be comparing pixel values, we will need to render to a set size texture
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();

            var(cam, cfg, cube) = SetupTestScene(texture);

            var labeler = new BoundingBox2DLabeler(cfg);
            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();

            var annName = "bounding box";
            var annType = "type.unity.com/unity.solo.BoundingBox2DAnnotation";
            var annDesc = "Produces 2D bounding box annotations for all visible objects that bear a label defined in this labeler's associated label configuration.";

            var instanceId = cube.GetComponent<Labeling>().instanceId;

            var values = new JArray
            {
                new JObject
                {
                    { "instanceId", instanceId },
                    { "labelId", 1 },
                    { "labelName", "test" },
                    { "origin", new JArray { 452.0, 324.0 } },
                    { "dimension", new JArray { 120.0, 120.0} }
                }
            };

            TestMetadata(annName, annType);
            TestDefintionSimpleConfig(annName, annType, annDesc);
            TestResults(annName, annType, annDesc, values);

            texture.Release();
        }

        static KeypointTemplate CreateTestTemplate()
        {
            var keypoints = new[]
            {
                new KeypointDefinition
                {
                    label = "FrontLowerLeft",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = 0.15f
                },
                new KeypointDefinition
                {
                    label = "FrontUpperLeft",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = 0.15f
                },
            };

            var skeleton = new[]
            {
                new SkeletonDefinition
                {
                    joint1 = 0,
                    joint2 = 1,
                    color = Color.magenta
                },
                new SkeletonDefinition
                {
                    joint1 = 1,
                    joint2 = 2,
                    color = Color.magenta
                },
            };

            var template = ScriptableObject.CreateInstance<KeypointTemplate>();
            template.templateID = "test_template_id";
            template.templateName = "test_template";
            template.jointTexture = null;
            template.skeletonTexture = null;
            template.keypoints = keypoints;
            template.skeleton = skeleton;

            return template;
        }

        static void SetupCubeJoint(GameObject cube, string label, float x, float y, float z, float? selfOcclusionDistance = null)
        {
            var joint = new GameObject();
            joint.transform.SetParent(cube.transform, false);
            joint.transform.localPosition = new Vector3(x, y, z);
            var jointLabel = joint.AddComponent<JointLabel>();
            jointLabel.labels.Add(label);
            if (selfOcclusionDistance.HasValue)
            {
                jointLabel.overrideSelfOcclusionDistance = true;
                jointLabel.selfOcclusionDistance = selfOcclusionDistance.Value;
            }
            else
                jointLabel.overrideSelfOcclusionDistance = false;
        }

        static void SetupCubeJoints(GameObject cube, KeypointTemplate template, float? selfOcclusionDistance = null)
        {
            const float dim = 0.5f;
            SetupCubeJoint(cube, "FrontLowerLeft", -dim, -dim, -dim, selfOcclusionDistance);
            SetupCubeJoint(cube, "FrontUpperLeft", -dim, dim, -dim, selfOcclusionDistance);
        }

        [UnityTest]
        public IEnumerator TestKeypointsSolo()
        {
            DatasetCapture.ResetSimulation();

            // Since we will be comparing pixel values, we will need to render to a set size texture
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();

            var(cam, cfg, cube) = SetupTestScene(texture);

            var template = CreateTestTemplate();
            SetupCubeJoints(cube, template);

            var labeler = new KeypointLabeler(cfg, template);
            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();

            var annName = "keypoints";
            var annType = "type.unity.com/unity.solo.KeypointAnnotation";
            var annDesc = "Produces keypoint annotations for all visible labeled objects that have a humanoid animation avatar component.";

            var instanceId = cube.GetComponent<Labeling>().instanceId;

            var values = new JArray
            {
                new JObject
                {
                    { "instanceId", instanceId },
                    { "labelId", 1 },
                    { "labelName", "test" },
                    { "origin", new JArray { 452.0, 324.0 } },
                    { "dimension", new JArray { 120.0, 120.0} }
                }
            };

            TestMetadata(annName, annType);
            TestDefintionKeypoints(annName, annType, annDesc);
            TestResults_Keypoints((int)instanceId, annName, annType, annDesc);

            texture.Release();
        }

        [UnityTest]
        public IEnumerator TestInstanceSegmentationSolo()
        {
            DatasetCapture.ResetSimulation();

            // Since we will be comparing pixel values, we will need to render to a set size texture
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();

            var(cam, cfg, cube) = SetupTestScene(texture);

            var labeler = new InstanceSegmentationLabeler(cfg);
            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();

            var annName = "instance segmentation";
            var annType = "type.unity.com/unity.solo.InstanceSegmentationAnnotation";
            var annDesc = "Produces an instance segmentation image for each frame. The image will render the pixels of each labeled object in a distinct color.";

            var instanceId = cube.GetComponent<Labeling>().instanceId;

            var instances = new JArray
            {
                new JObject
                {
                    { "instanceId", instanceId },
                    { "labelId", 1 },
                    { "labelName", "test" },
                    { "color", new JArray { 255, 0, 0, 255 } }
                }
            };

            TestMetadata(annName, annType);
            TestDefintionSimpleConfig(annName, annType, annDesc);
            TestResults_Segmentation(annName, annType, annDesc, LosslessImageEncodingFormat.Png, instances);

            texture.Release();
        }

        [UnityTest]
        public IEnumerator TestSemanticSegmentationSolo()
        {
            DatasetCapture.ResetSimulation();

            // Since we will be comparing pixel values, we will need to render to a set size texture
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();

            var(cam, cfg, _) = SetupTestScene(texture);

            var cfg2 = ScriptableObject.CreateInstance<SemanticSegmentationLabelConfig>();
            cfg2.Init(new List<SemanticSegmentationLabelEntry>
            {
                new SemanticSegmentationLabelEntry
                {
                    label = "test",
                    color = Color.green
                }
            });
            AddTestObjectForCleanup(cfg2);

            var labeler = new SemanticSegmentationLabeler(cfg2);

            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();

            var annName = "semantic segmentation";
            var annType = "type.unity.com/unity.solo.SemanticSegmentationAnnotation";
            var annDesc = "Generates a semantic segmentation image for each captured frame. Each object is rendered to the semantic segmentation image using the color associated with it based on this labeler's associated semantic segmentation label configuration. Semantic segmentation images are saved to the dataset in PNG format. Please note that only one SemanticSegmentationLabeler can render at once across all cameras.";

            var instances = new JArray
            {
                new JObject
                {
                    { "labelName", "test" },
                    { "pixelValue", new JArray { 0, 255, 0, 255 } }
                }
            };


            TestMetadata(annName, annType);
            TestDefintionNoLabelConfig(annName, annType, annDesc);
            TestResults_Segmentation(annName, annType, annDesc, LosslessImageEncodingFormat.Png, instances);

            texture.Release();
        }

        [UnityTest]
        public IEnumerator TestDepthSolo()
        {
            DatasetCapture.ResetSimulation();

            // Since we will be comparing pixel values, we will need to render to a set size texture
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();

            var(cam, _, _) = SetupTestScene(texture);

            var labeler = new DepthLabeler();
            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();

            var annName = "Depth";
            var annType = "type.unity.com/unity.solo.DepthAnnotation";
            var annDesc = "Generates a 32-bit depth image in EXR format where each pixel contains the actual distance in Unity units (usually meters) from the camera to the object in the scene.";
            var imgFormat = LosslessImageEncodingFormat.Exr;
            var measurementStrategy = DepthMeasurementStrategy.Depth;

            TestMetadata(annName, annType);
            TestDefintionNoLabelConfig(annName, annType, annDesc);

            var dataPath = PathUtils.CombineUniversal(m_Endpoint.currentPath, "sequence.0", "step0.frame_data.json");
            FileAssert.Exists(dataPath);

            var data = JObject.Parse(File.ReadAllText(dataPath));

            Assert.IsTrue(data.ContainsKey("captures"));
            var cap = data["captures"][0] as JObject;
            Assert.IsTrue(cap.ContainsKey("annotations"));
            var sub = cap["annotations"];

            var expected = new JArray
            {
                new JObject
                {
                    { "@type", annType },
                    { "id", annName },
                    { "sensorId", "camera" },
                    { "description", annDesc },
                    { "measurementStrategy", measurementStrategy.ToString() },
                    { "imageFormat", imgFormat.ToString() },
                    { "dimension", new JArray { 1024.0, 768.0 }},
                    { "filename", $"step0.camera.{annName}.{imgFormat.ToString().ToLower()}"},
                }
            };

            Assert.IsTrue(JToken.DeepEquals(expected, sub));

            // Verify that a file was written to disk
            var annPath = PathUtils.CombineUniversal(
                m_Endpoint.currentPath, "sequence.0", $"step0.camera.{annName}.{imgFormat.ToString().ToLower()}");
            FileAssert.Exists(annPath);

            texture.Release();
        }

        [UnityTest]
        public IEnumerator TestNormalSolo()
        {
            DatasetCapture.ResetSimulation();

            // Since we will be comparing pixel values, we will need to render to a set size texture
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();

            var(cam, cfg, _) = SetupTestScene(texture);

            var labeler = new NormalLabeler();
            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();

            var annName = "Normal";
            var annType = "type.unity.com/unity.solo.NormalAnnotation";
            var annDesc = "Produces an image capturing the vertex normals of objects within the frame.";
            var imgType = LosslessImageEncodingFormat.Exr;

            TestMetadata(annName, annType);
            TestDefintionNoLabelConfig(annName, annType, annDesc);
            TestResults_Segmentation(annName, annType, annDesc, imgType);

            texture.Release();
        }

        [UnityTest]
        public IEnumerator TestPixelPositionSolo()
        {
            DatasetCapture.ResetSimulation();

            // Since we will be comparing pixel values, we will need to render to a set size texture
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();

            var(cam, cfg, _) = SetupTestScene(texture);

            var labeler = new PixelPositionLabeler();
            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();

            var annName = "PixelPosition";
            var annType = "type.unity.com/unity.solo.PixelPositionAnnotation";
            var annDesc = "Generates a pixelized position image where RGB channels denote the " +
                "XYZ components of the vector from the camera to the object at a pixel respectively.";

            TestMetadata(annName, annType);
            TestDefintionNoLabelConfig(annName, annType, annDesc);
            TestResults_Segmentation(annName, annType, annDesc, LosslessImageEncodingFormat.Exr);

            texture.Release();
        }

        [UnityTest]
        public IEnumerator TestObjectCountSolo()
        {
            DatasetCapture.ResetSimulation();

            // Since we will be comparing pixel values, we will need to render to a set size texture
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();

            var(cam, cfg, _) = SetupTestScene(texture);

            var labeler = new ObjectCountLabeler(cfg);
            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();

            var annName = "ObjectCount";
            var annType = "type.unity.com/unity.solo.ObjectCountMetric";
            var annDesc = "Produces object counts for each label defined in this labeler's associated label configuration.";

            var values = new JArray
            {
                new JObject
                {
                    { "labelId", 1 },
                    { "labelName", "test" },
                    { "count", 1 }
                }
            };

            TestMetricMetadata(annName);
            TestMetricDefintionConfig(annName, annType, annDesc);
            TestMetricResults(annName, annType, annDesc, values);

            texture.Release();
        }

        [UnityTest]
        public IEnumerator TestRenderObjectInfoSolo()
        {
            DatasetCapture.ResetSimulation();

            // Since we will be comparing pixel values, we will need to render to a set size texture
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();

            var(cam, cfg, cube) = SetupTestScene(texture);

            var labeler = new RenderedObjectInfoLabeler(cfg);
            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();

            var annName = "RenderedObjectInfo";
            var annType = "type.unity.com/unity.solo.RenderedObjectInfoMetric";
            var annDesc = "Produces label id, instance id, and visible pixel count in a single metric each frame for each object which takes up one or more pixels in the camera's frame, based on this labeler's associated label configuration.";

            var instanceId = cube.GetComponent<Labeling>().instanceId;

            var values = new JArray
            {
                new JObject
                {
                    { "labelId", 1 },
                    { "instanceId", instanceId },
                    { "color", new JArray { 255, 0, 0, 255 } },
                    { "visiblePixels", 14400 },
                    { "parentInstanceId", -1},
                    { "childrenInstanceIds", new JArray() },
                    { "labels", new JArray { "test" } }
                }
            };

            TestMetricMetadata(annName);
            TestMetricDefintionConfig(annName, annType, annDesc);
            TestMetricResults(annName, annType, annDesc, values);

            texture.Release();
        }

        [UnityTest]
        public IEnumerator TestOcclusionSolo()
        {
            DatasetCapture.ResetSimulation();

            // Since we will be comparing pixel values, we will need to render to a set size texture
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();

            var(cam, cfg, cube) = SetupTestScene(texture);

            var labeler = new OcclusionLabeler(cfg);
            cam.AddLabeler(labeler);

            yield return null;
            DatasetCapture.ResetSimulation();

            var annName = "Occlusion";
            var annType = "type.unity.com/unity.solo.OcclusionMetric";
            var annDesc = "Visibility metrics for labeled objects";

            var instanceId = cube.GetComponent<Labeling>().instanceId;

            var values = new JArray
            {
                new JObject
                {
                    { "instanceId", instanceId },
                    { "percentVisible", 1.0 },
                    { "percentInFrame", 1.0 },
                    { "visibilityInFrame", 1.0 },
                }
            };

            TestMetricMetadata(annName);
            TestMetricDefintionConfig(annName, annType, annDesc);
            TestMetricResults(annName, annType, annDesc, values);

            texture.Release();
        }
    }
}
