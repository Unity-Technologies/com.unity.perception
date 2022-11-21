using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GroundTruthTests
{
    [TestFixture]
    public class OcclusionLabelerTests
    {
        const float k_OcclusionJitter = 0.01f;
        List<Object> m_ObjectsToDestroy = new List<Object>();

        [UnitySetUp]
        public IEnumerator Init()
        {
            DatasetCapture.OverrideEndpoint(new NoOutputEndpoint());
            DatasetCapture.ResetSimulation();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var o in m_ObjectsToDestroy)
                Object.DestroyImmediate(o);
            m_ObjectsToDestroy.Clear();

            DatasetCapture.ResetSimulation();
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptySceneProducesNoOcclusionMetrics()
        {
            var camera = SetupCamera();
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);

            // Confirm that 0 occlusion metrics are produced for an empty scene
            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                Assert.AreEqual(metricEntries.Length, 0);
            };
            yield return null;
        }

        [UnityTest]
        public IEnumerator NoOcclusion()
        {
            var camera = SetupCamera();
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);

            // Create a labeled quad
            CreateLabeledQuad();

            // Confirm that 100% of the labeled quad is visible
            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                Assert.AreEqual(metricEntries.Length, 1);
                var metric = metricEntries[0];
                Assert.AreEqual(metric.percentVisible, 1.0f, k_OcclusionJitter);
                Assert.AreEqual(metric.percentInFrame, 1.0f, k_OcclusionJitter);
                Assert.AreEqual(metric.visibilityInFrame, 1.0f, k_OcclusionJitter);
            };
            yield return null;
        }

        [UnityTest]
        public IEnumerator OutputMetricsForMultipleVisibleObjects()
        {
            var camera = SetupCamera();
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);

            // Create a row of 3 labeled quads.
            CreateLabeledQuad().transform.position = new Vector3(-1.5f, 0f, 0f);
            CreateLabeledQuad();
            CreateLabeledQuad().transform.position = new Vector3(1.5f, 0f, 0f);

            // Confirm that all 3 quads are 100% visible.
            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                Assert.AreEqual(metricEntries.Length, 3);
                foreach (var metric in metricEntries)
                {
                    Assert.AreEqual(metric.percentVisible, 1.0f, k_OcclusionJitter);
                    Assert.AreEqual(metric.percentInFrame, 1.0f, k_OcclusionJitter);
                    Assert.AreEqual(metric.visibilityInFrame, 1.0f, k_OcclusionJitter);
                }
            };

            yield return null;
        }

        [UnityTest]
        public IEnumerator PartialFrameOcclusion()
        {
            var camera = SetupCamera();
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);

            // Create a labeled quad
            CreateLabeledQuad();

            // Turn the camera so that half of the quad is out of frame
            var fovHorizontal = GetHorizontalFov(camera.GetComponent<Camera>());
            camera.transform.rotation = Quaternion.Euler(0, fovHorizontal / 2, 0);

            // Confirm that only 50% of the labeled quad is visible
            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                Assert.AreEqual(metricEntries.Length, 1);
                var metric = metricEntries[0];
                Assert.AreEqual(metric.percentVisible, 0.5f, k_OcclusionJitter);
                Assert.AreEqual(metric.percentInFrame, 0.5f, k_OcclusionJitter);
                Assert.AreEqual(metric.visibilityInFrame, 1.0f, k_OcclusionJitter);
            };

            yield return null;
        }

        [UnityTest]
        public IEnumerator PartialObjectOcclusion()
        {
            var camera = SetupCamera();
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);

            // Create a labeled quad
            CreateLabeledQuad();

            // Place a skinny quad in front of the labeled quad so that half of the first quad is visible
            var occludingQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            occludingQuad.transform.localScale = new Vector3(0.5f, 1, 1);
            occludingQuad.transform.position = new Vector3(0, 0, -0.01f);
            AddTestObjectForCleanup(occludingQuad);

            // Confirm that only 50% of the labeled quad is visible
            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                Assert.AreEqual(metricEntries.Length, 1);
                var metric = metricEntries[0];
                Assert.AreEqual(metric.percentVisible, 0.5f, k_OcclusionJitter);
                Assert.AreEqual(metric.percentInFrame, 1.0f, k_OcclusionJitter);
                Assert.AreEqual(metric.visibilityInFrame, 0.5f, k_OcclusionJitter);
            };

            yield return null;
        }

        [UnityTest]
        public IEnumerator CompleteObjectOcclusion()
        {
            var camera = SetupCamera();
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);

            // Create a labeled quad
            CreateLabeledQuad();

            // Create an occluding quad and move it forward to completely occlude the labeled quad
            var occludingQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            occludingQuad.transform.position = new Vector3(0, 0, -1);
            AddTestObjectForCleanup(occludingQuad);

            // Confirm that zero occlusion metrics are output since the
            // one labeled object in the scene is completely occluded.
            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                Assert.AreEqual(metricEntries.Length, 0);
            };

            yield return null;
        }

        [UnityTest]
        public IEnumerator CompleteFrameOcclusion()
        {
            var camera = SetupCamera();
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);

            // Create a labeled quad.
            var quad = CreateLabeledQuad();

            // Move the quad behind the camera so as to move it out of frame.
            quad.transform.position = new Vector3(0, 0, -10);

            // Confirm that zero occlusion metrics are output since the
            // one labeled object in the scene is outside of the camera's field of view.
            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                Assert.AreEqual(metricEntries.Length, 0);
            };

            yield return null;
        }

        [UnityTest]
        public IEnumerator CombinedObjectAndFrameOcclusion()
        {
            var camera = SetupCamera();
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);

            // Create a labeled quad
            CreateLabeledQuad();

            // Place a skinny occluding quad in front of the labeled quad so that half of the labeled quad is visible
            var occludingQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            occludingQuad.transform.localScale = new Vector3(0.5f, 1, 1);
            occludingQuad.transform.position = new Vector3(0, 0, -0.01f);
            AddTestObjectForCleanup(occludingQuad);

            // Turn the camera so that half of the quad is out of frame
            var fovHorizontal = GetHorizontalFov(camera.GetComponent<Camera>());
            camera.transform.rotation = Quaternion.Euler(0, fovHorizontal / 2, 0);

            // Confirm that only 25% of the labeled quad is visible
            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                Assert.AreEqual(metricEntries.Length, 1);
                var metric = metricEntries[0];
                Assert.AreEqual(metric.percentVisible, 0.25f, k_OcclusionJitter);
                Assert.AreEqual(metric.percentInFrame, 0.5f, k_OcclusionJitter);
                Assert.AreEqual(metric.visibilityInFrame, 0.5f, k_OcclusionJitter);
            };

            yield return null;
        }

        [UnityTest]
        public IEnumerator ValidateFieldOfViewRange([Values(1f, 30f, 60f, 90f, 120f, 150f, 179f)] float fov)
        {
            var camera = SetupCamera();
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);

            // Set camera fov
            camera.GetComponent<Camera>().fieldOfView = fov;

            // Create a labeled quad
            CreateLabeledQuad();

            // Confirm that 100% of the labeled quad is visible for each field of view,
            // except when the fov is 1 or 179 degrees wide.
            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                if (fov <= 1f)
                {
                    Assert.AreEqual(metricEntries.Length, 1);
                    var metric = metricEntries[0];
                    Assert.Greater(metric.percentVisible, 0f);
                    Assert.Greater(metric.percentInFrame, 0f);
                    Assert.AreEqual(metric.visibilityInFrame, 1.0f, k_OcclusionJitter);
                }
                else if (fov >= 179f)
                {
                    // With a fov of 179 degrees, the quad should be smaller than a single pixel
                    // and thus not be assigned a visibility metric.
                    Assert.AreEqual(metricEntries.Length, 0);
                }
                else
                {
                    // For more standard field of views, the visibility should be 100%.
                    Assert.AreEqual(metricEntries.Length, 1);
                    var metric = metricEntries[0];
                    Assert.AreEqual(metric.percentVisible, 1.0f, k_OcclusionJitter);
                    Assert.AreEqual(metric.percentInFrame, 1.0f, k_OcclusionJitter);
                    Assert.AreEqual(metric.visibilityInFrame, 1.0f, k_OcclusionJitter);
                }
            };
            yield return null;
        }

        [UnityTest]
        public IEnumerator ValidateCameraAspectRatioRange([Values(0.5f, 9f / 16f, 1f, 16f / 9f, 2f)] float aspectRatio)
        {
            // Set the camera's aspect ratio to the test ratio
            const int height = 512;
            var width = Mathf.RoundToInt(aspectRatio * height);
            var camera = SetupCamera(width, height);
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);

            // Create a labeled quad
            CreateLabeledQuad();

            // Confirm that 100% of the labeled quad is visible
            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                Assert.AreEqual(metricEntries.Length, 1);
                var metric = metricEntries[0];
                Assert.AreEqual(metric.percentVisible, 1.0f, k_OcclusionJitter);
                Assert.AreEqual(metric.percentInFrame, 1.0f, k_OcclusionJitter);
                Assert.AreEqual(metric.visibilityInFrame, 1.0f, k_OcclusionJitter);
            };

            yield return null;
        }

        [UnityTest]
        public IEnumerator ValidateDifferentOutOfFrameResolutions([Values(100, 500, 1000)] int sqrWidth)
        {
            // Configure a camera to output a specific pixel width and height.
            var camera = SetupCamera();
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);
            occlusionLabeler.outOfFrameResolution = sqrWidth;

            // Create a labeled quad
            CreateLabeledQuad();

            // Turn the camera so that half of the quad is out of frame
            var fovHorizontal = GetHorizontalFov(camera.GetComponent<Camera>());
            camera.transform.rotation = Quaternion.Euler(0, fovHorizontal / 2, 0);

            // Confirm that only ~50% of the labeled quad is visible
            // with some wiggle room given the test's resolution parameter.
            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                Assert.AreEqual(metricEntries.Length, 1);
                var metric = metricEntries[0];
                Assert.AreEqual(metric.percentVisible, 0.5f, k_OcclusionJitter);
                Assert.AreEqual(metric.percentInFrame, 0.5f, k_OcclusionJitter);
                Assert.AreEqual(metric.visibilityInFrame, 1.0f, k_OcclusionJitter);
            };

            yield return null;
        }

        [UnityTest]
        public IEnumerator LinearVisibilityMetricForRotationallySymmetricObjects()
        {
            // Place a camera at the origin of the scene.
            var camera = SetupCamera();
            camera.transform.position = Vector3.zero;
            var occlusionLabeler = AddOcclusionLabelerToCamera(camera);

            // Get the camera's horizontal field of view.
            var fovHorizontal = GetHorizontalFov(camera.GetComponent<Camera>());

            // Create inverted cylinder wall that will span the fov of the camera.
            // This cylinder is a rotationally symmetric along the Y axis.
            CreateLabeledInvertedCylinderWall(fovHorizontal * Mathf.Deg2Rad, 3f, 1f);

            const int iterations = 10;
            var startFrame = Time.frameCount;

            occlusionLabeler.occlusionMetricsComputed += (frame, metricEntries) =>
            {
                var iteration = frame - startFrame;

                // On the first iteration, the cylinder should not be visible.
                if (iteration == 0)
                {
                    Assert.AreEqual(metricEntries.Length, 0);
                    return;
                }

                Assert.AreEqual(metricEntries.Length, 1);
                var visibility = iteration / (float)iterations;

                // Confirm that the cylinder's visibility is linearly increasing.
                var metric = metricEntries[0];
                Assert.AreEqual(metric.percentVisible, visibility, k_OcclusionJitter);
                Assert.AreEqual(metric.percentInFrame, visibility, k_OcclusionJitter);
                Assert.AreEqual(metric.visibilityInFrame, 1.0f, k_OcclusionJitter);
            };

            // Rotate the camera so that the cylinder's visibility metric
            // increases linearly from 0 to 1 as it enters the frame.
            for (var i = 0; i <= iterations; i++)
            {
                var t = i / (float)iterations;
                var angle = Mathf.Lerp(0f, fovHorizontal, t);
                camera.transform.rotation = Quaternion.Euler(0, angle, 0);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ValidateOcclusionMetricsSentToEndpoint()
        {
            // Override the endpoint in order to collect the generated dataset.
            var collector = new CollectEndpoint();
            DatasetCapture.OverrideEndpoint(collector);

            // Reset the simulation so that the override endpoint is used.
            DatasetCapture.ResetSimulation();

            // Setup a PerceptionCamera and add an OcclusionLabeler.
            var camera = SetupCamera();
            AddOcclusionLabelerToCamera(camera);

            // Create a row of 3 labeled quads partially layered one in front of the other.
            CreateLabeledQuad().transform.position = new Vector3(-0.5f, 0f, 0.01f);
            CreateLabeledQuad();
            CreateLabeledQuad().transform.position = new Vector3(0.5f, 0f, -0.01f);

            // Wait one frame.
            yield return null;

            // Reset the simulation to wait for the OcclusionLabeler's
            // async processes to finish producing their metrics.
            DatasetCapture.ResetSimulation();

            // Check that 1 frame was captured.
            Assert.AreEqual(collector.currentRun.TotalFrames, 1);

            // Validate that 3 sets of occlusion metrics were produced.
            var metrics = collector.currentRun.frames[0].metrics;
            Assert.AreEqual(metrics.Count, 1);
            var occlusionMetricsArray = metrics[0];
            var reportedMetricsEntries = occlusionMetricsArray.GetValues<OcclusionMetricEntry>();
            Assert.AreEqual(reportedMetricsEntries.Length, 3);
            void ValidateReportedMetrics(
                OcclusionMetricEntry entry,
                float percentVisible, float percentInFrame, float visibilityInFrame)
            {
                const float delta = 0.02f;
                Assert.AreEqual(entry.percentVisible, percentVisible, delta);
                Assert.AreEqual(entry.percentInFrame, percentInFrame, delta);
                Assert.AreEqual(entry.visibilityInFrame, visibilityInFrame, delta);
            }

            // Validate the values reported in the 3 sets of occlusion metrics.
            ValidateReportedMetrics(reportedMetricsEntries[0], 0.5f, 1.0f, 0.5f);
            ValidateReportedMetrics(reportedMetricsEntries[1], 0.5f, 1.0f, 0.5f);
            ValidateReportedMetrics(reportedMetricsEntries[2], 1.0f, 1.0f, 1.0f);
        }

        void AddTestObjectForCleanup(Object obj) => m_ObjectsToDestroy.Add(obj);

        PerceptionCamera SetupCamera(int width = 800, int height = 600)
        {
            var cameraObject = new GameObject("Camera");
            cameraObject.transform.position = new Vector3(0, 0, -4f);
            var camera = cameraObject.AddComponent<Camera>();
            var targetTexture = new RenderTexture(width, height, 32, GraphicsFormat.R8G8B8A8_SRGB);
            camera.targetTexture = targetTexture;
            camera.orthographic = false;
            camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(60, camera.aspect);

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            perceptionCamera.showVisualizations = false;

            AddTestObjectForCleanup(cameraObject);
            return perceptionCamera;
        }

        OcclusionLabeler AddOcclusionLabelerToCamera(PerceptionCamera camera)
        {
            var idLabelConfig = ScriptableObject.CreateInstance<IdLabelConfig>();
            idLabelConfig.Init(new List<IdLabelEntry>
            {
                new IdLabelEntry
                {
                    label = "label",
                    id = 1
                }
            });
            var occlusionLabeler = new OcclusionLabeler(idLabelConfig);
            camera.AddLabeler(occlusionLabeler);
            AddTestObjectForCleanup(idLabelConfig);
            return occlusionLabeler;
        }

        GameObject CreateLabeledQuad()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var labeling = quad.AddComponent<Labeling>();
            labeling.labels.Add("label");
            AddTestObjectForCleanup(quad);
            return quad;
        }

        static float GetHorizontalFov(Camera camera)
        {
            return Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect);
        }

        void CreateLabeledInvertedCylinderWall(float angle, float radius, float height)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var filter = cylinder.GetComponent<MeshFilter>();
            filter.mesh = CreateInvertedCylinderWallMesh(angle, radius, height);
            var labeling = cylinder.AddComponent<Labeling>();
            labeling.labels.Add("label");
            AddTestObjectForCleanup(cylinder);
        }

        static Mesh CreateInvertedCylinderWallMesh(float angle, float radius, float height)
        {
            const int radialResolution = 32;
            const int verticalResolution = 8;
            const int vertexCount = radialResolution * verticalResolution;
            const int indicesCount = (radialResolution - 1) * (verticalResolution - 1) * 6;

            var mesh = new Mesh();

            var halfHeight = height / 2f;
            var endAngle = Mathf.PI / 2 - angle / 2;
            var startAngle = endAngle - angle;
            var vertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
            for (var i = 0; i < verticalResolution; i++)
            {
                var currentHeight = Mathf.Lerp(-halfHeight, halfHeight, i / (verticalResolution - 1f));
                for (var j = 0; j < radialResolution; j++)
                {
                    var t = j / (radialResolution - 1f);
                    var currentAngle = Mathf.Lerp(startAngle, endAngle, t);
                    vertices[i * radialResolution + j] = new Vector3
                    {
                        x = Mathf.Cos(currentAngle) * radius,
                        y = currentHeight,
                        z = Mathf.Sin(currentAngle) * radius,
                    };
                }
            }

            var normals = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
            for (var i = 0; i < normals.Length; i++)
            {
                var vertex = vertices[i];
                normals[i] = (new Vector3(0, vertex.y, 0) - vertex).normalized;
            }

            var indices = new NativeArray<int>(indicesCount, Allocator.Temp);
            var index = 0;
            for (var i = 0; i < verticalResolution - 1; i++)
            {
                for (var j = 0; j < radialResolution - 1; j++)
                {
                    var offset = i * radialResolution + j;
                    indices[index++] = offset;
                    indices[index++] = offset + radialResolution + 1;
                    indices[index++] = offset + radialResolution;
                    indices[index++] = offset;
                    indices[index++] = offset + 1;
                    indices[index++] = offset + radialResolution + 1;
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            vertices.Dispose();
            normals.Dispose();
            indices.Dispose();

            return mesh;
        }
    }
}
