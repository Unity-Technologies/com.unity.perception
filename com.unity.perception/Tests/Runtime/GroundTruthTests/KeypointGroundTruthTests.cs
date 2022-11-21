using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class KeyPointGroundTruthTests : GroundTruthTestBase
    {
        const double k_Delta = .01;

        [UnitySetUp]
        public IEnumerator SetupTest()
        {
            foreach (var p in LoadCubeScene()) yield return p;
        }

        static GameObject SetupCamera(IdLabelConfig config, KeypointTemplate template, Action<int, List<KeypointComponent>> computeListener, RenderTexture renderTexture = null, KeypointObjectFilter keypointObjectFilter = KeypointObjectFilter.Visible)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 60;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000;

            camera.transform.position = new Vector3(0, 0, -10);

            if (renderTexture)
            {
                camera.targetTexture = renderTexture;
            }

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            var keyPointLabeler = new KeypointLabeler(config, template);
            keyPointLabeler.objectFilter = keypointObjectFilter;
            if (computeListener != null)
                keyPointLabeler.KeypointsComputed += computeListener;

            perceptionCamera.AddLabeler(keyPointLabeler);

            return cameraObject;
        }

        static KeypointTemplate CreateTestTemplate(Guid guid, string label, float selfOcclusionDistance = 0.15f)
        {
            var keypoints = new[]
            {
                new KeypointDefinition
                {
                    label = "FrontLowerLeft",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                },
                new KeypointDefinition
                {
                    label = "FrontUpperLeft",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                },
                new KeypointDefinition
                {
                    label = "FrontUpperRight",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                },
                new KeypointDefinition
                {
                    label = "FrontLowerRight",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                },
                new KeypointDefinition
                {
                    label = "BackLowerLeft",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                },
                new KeypointDefinition
                {
                    label = "BackUpperLeft",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                },
                new KeypointDefinition
                {
                    label = "BackUpperRight",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                },
                new KeypointDefinition
                {
                    label = "BackLowerRight",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                },
                new KeypointDefinition
                {
                    label = "Center",
                    associateToRig = false,
                    color = Color.black,
                    selfOcclusionDistance = selfOcclusionDistance
                }
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
                new SkeletonDefinition
                {
                    joint1 = 2,
                    joint2 = 3,
                    color = Color.magenta
                },
                new SkeletonDefinition
                {
                    joint1 = 3,
                    joint2 = 0,
                    color = Color.magenta
                },

                new SkeletonDefinition
                {
                    joint1 = 4,
                    joint2 = 5,
                    color = Color.blue
                },
                new SkeletonDefinition
                {
                    joint1 = 5,
                    joint2 = 6,
                    color = Color.blue
                },
                new SkeletonDefinition
                {
                    joint1 = 6,
                    joint2 = 7,
                    color = Color.blue
                },
                new SkeletonDefinition
                {
                    joint1 = 7,
                    joint2 = 4,
                    color = Color.blue
                },

                new SkeletonDefinition
                {
                    joint1 = 0,
                    joint2 = 4,
                    color = Color.green
                },
                new SkeletonDefinition
                {
                    joint1 = 1,
                    joint2 = 5,
                    color = Color.green
                },
                new SkeletonDefinition
                {
                    joint1 = 2,
                    joint2 = 6,
                    color = Color.green
                },
                new SkeletonDefinition
                {
                    joint1 = 3,
                    joint2 = 7,
                    color = Color.green
                },
            };

            var template = ScriptableObject.CreateInstance<KeypointTemplate>();
            template.templateID = guid.ToString();
            template.templateName = label;
            template.jointTexture = null;
            template.skeletonTexture = null;
            template.keypoints = keypoints;
            template.skeleton = skeleton;

            return template;
        }

        [Test]
        public void KeypointTemplate_CreateTemplateTest()
        {
            var guid = Guid.NewGuid();
            const string label = "TestTemplate";
            var template = CreateTestTemplate(guid, label);

            Assert.AreEqual(template.templateID, guid.ToString());
            Assert.AreEqual(template.templateName, label);
            Assert.IsNull(template.jointTexture);
            Assert.IsNull(template.skeletonTexture);
            Assert.IsNotNull(template.keypoints);
            Assert.IsNotNull(template.skeleton);
            Assert.AreEqual(template.keypoints.Length, 9);
            Assert.AreEqual(template.skeleton.Length, 12);

            var k0 = template.keypoints[0];
            Assert.NotNull(k0);
            Assert.AreEqual(k0.label, "FrontLowerLeft");
            Assert.False(k0.associateToRig);
            Assert.AreEqual(k0.color, Color.black);

            var s0 = template.skeleton[0];
            Assert.NotNull(s0);
            Assert.AreEqual(s0.joint1, 0);
            Assert.AreEqual(s0.joint2, 1);
            Assert.AreEqual(s0.color, Color.magenta);
        }

        static IdLabelConfig SetUpLabelConfig()
        {
            var cfg = ScriptableObject.CreateInstance<IdLabelConfig>();
            cfg.Init(new List<IdLabelEntry>()
            {
                new IdLabelEntry
                {
                    id = 1,
                    label = "label"
                }
            });

            return cfg;
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
            SetupCubeJoint(cube, "FrontUpperRight", dim, dim, -dim, selfOcclusionDistance);
            SetupCubeJoint(cube, "FrontLowerRight", dim, -dim, -dim, selfOcclusionDistance);
            SetupCubeJoint(cube, "BackLowerLeft", -dim, -dim, dim, selfOcclusionDistance);
            SetupCubeJoint(cube, "BackUpperLeft", -dim, dim, dim, selfOcclusionDistance);
            SetupCubeJoint(cube, "BackUpperRight", dim, dim, dim, selfOcclusionDistance);
            SetupCubeJoint(cube, "BackLowerRight", dim, -dim, dim, selfOcclusionDistance);
        }

        [UnityTest]
        public IEnumerator Keypoint_TestStaticLabeledCube()
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(new List<KeypointComponent>(data));
            }, texture);

            var cube = SetupLabeledCube(scale: 6, z: 8);
            var instanceId = cube.GetComponent<Labeling>().instanceId;
            SetupCubeJoints(cube, template);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);
            texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(instanceId, t.instanceId);
            Assert.AreEqual(1, t.labelId);

            Assert.AreEqual(9, t.keypoints.Length);
            Assert.AreEqual(t.keypoints[0].location.x, t.keypoints[1].location.x);
            Assert.AreEqual(t.keypoints[2].location.x, t.keypoints[3].location.x);
            Assert.AreEqual(t.keypoints[4].location.x, t.keypoints[5].location.x);
            Assert.AreEqual(t.keypoints[6].location.x, t.keypoints[7].location.x);

            Assert.AreEqual(t.keypoints[0].location.y, t.keypoints[3].location.y);
            Assert.AreEqual(t.keypoints[1].location.y, t.keypoints[2].location.y);
            Assert.AreEqual(t.keypoints[4].location.y, t.keypoints[7].location.y);
            Assert.AreEqual(t.keypoints[5].location.y, t.keypoints[6].location.y);

            // test camera coordinates
            var expectedMinX = -3f;
            var expectedMaxX = 3f;
            var expectedMinY = -3f;
            var expectedMaxY = 3f;
            var expectedMinZ = 15f;
            var expectedMaxZ = 21f;

            Assert.AreEqual(9, t.keypoints.Length);
            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.x, t.keypoints[1].cameraCartesianLocation.x);
            Assert.AreEqual(t.keypoints[2].cameraCartesianLocation.x, t.keypoints[3].cameraCartesianLocation.x);
            Assert.AreEqual(t.keypoints[4].cameraCartesianLocation.x, t.keypoints[5].cameraCartesianLocation.x);
            Assert.AreEqual(t.keypoints[6].cameraCartesianLocation.x, t.keypoints[7].cameraCartesianLocation.x);

            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.y, t.keypoints[3].cameraCartesianLocation.y);
            Assert.AreEqual(t.keypoints[1].cameraCartesianLocation.y, t.keypoints[2].cameraCartesianLocation.y);
            Assert.AreEqual(t.keypoints[4].cameraCartesianLocation.y, t.keypoints[7].cameraCartesianLocation.y);
            Assert.AreEqual(t.keypoints[5].cameraCartesianLocation.y, t.keypoints[6].cameraCartesianLocation.y);

            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.z, t.keypoints[1].cameraCartesianLocation.z);
            Assert.AreEqual(t.keypoints[2].cameraCartesianLocation.z, t.keypoints[3].cameraCartesianLocation.z);
            Assert.AreEqual(t.keypoints[4].cameraCartesianLocation.z, t.keypoints[5].cameraCartesianLocation.z);
            Assert.AreEqual(t.keypoints[6].cameraCartesianLocation.z, t.keypoints[7].cameraCartesianLocation.z);

            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.x, expectedMinX);
            Assert.AreEqual(t.keypoints[2].cameraCartesianLocation.x, expectedMaxX);
            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.y, expectedMinY);
            Assert.AreEqual(t.keypoints[1].cameraCartesianLocation.y, expectedMaxY);
            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.z, expectedMinZ);
            Assert.AreEqual(t.keypoints[4].cameraCartesianLocation.z, expectedMaxZ);

            for (var i = 0; i < 9; i++) Assert.AreEqual(i, t.keypoints[i].index);
            for (var i = 0; i < 4; i++) Assert.AreEqual(2, t.keypoints[i].state);
            for (var i = 4; i < 8; i++) Assert.AreEqual(1, t.keypoints[i].state);
            Assert.Zero(t.keypoints[8].state);
            Assert.Zero(t.keypoints[8].location.x);
            Assert.Zero(t.keypoints[8].location.y);
        }

        [UnityTest]
        public IEnumerator Keypoint_TestStaticLabeledCube_CameraCartesianForCameraOffset()
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(new List<KeypointComponent>(data));
            }, texture);

            cam.transform.position = new Vector3(2500, 17, -52);
            cam.transform.rotation = Quaternion.Euler(0, 90, 0);

            var cube = SetupLabeledCube(scale: 6, z: 8);
            SetupCubeJoints(cube, template);
            var labelingComponent = cube.GetComponent<Labeling>();
            var instanceId = labelingComponent.instanceId;

            cube.transform.position = new Vector3(2510, 17, -52);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);
            texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(instanceId, t.instanceId);
            Assert.AreEqual(1, t.labelId);

            Assert.AreEqual(9, t.keypoints.Length);

            // test camera coordinates
            var expectedMinX = -3f;
            var expectedMaxX = 3f;
            var expectedMinY = -3f;
            var expectedMaxY = 3f;
            var expectedMinZ = 7f;
            var expectedMaxZ = 13f;

            Assert.AreEqual(9, t.keypoints.Length);
            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.x, t.keypoints[1].cameraCartesianLocation.x);
            Assert.AreEqual(t.keypoints[2].cameraCartesianLocation.x, t.keypoints[3].cameraCartesianLocation.x);
            Assert.AreEqual(t.keypoints[4].cameraCartesianLocation.x, t.keypoints[5].cameraCartesianLocation.x);
            Assert.AreEqual(t.keypoints[6].cameraCartesianLocation.x, t.keypoints[7].cameraCartesianLocation.x);

            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.y, t.keypoints[3].cameraCartesianLocation.y);
            Assert.AreEqual(t.keypoints[1].cameraCartesianLocation.y, t.keypoints[2].cameraCartesianLocation.y);
            Assert.AreEqual(t.keypoints[4].cameraCartesianLocation.y, t.keypoints[7].cameraCartesianLocation.y);
            Assert.AreEqual(t.keypoints[5].cameraCartesianLocation.y, t.keypoints[6].cameraCartesianLocation.y);

            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.z, t.keypoints[1].cameraCartesianLocation.z);
            Assert.AreEqual(t.keypoints[2].cameraCartesianLocation.z, t.keypoints[3].cameraCartesianLocation.z);
            Assert.AreEqual(t.keypoints[4].cameraCartesianLocation.z, t.keypoints[5].cameraCartesianLocation.z);
            Assert.AreEqual(t.keypoints[6].cameraCartesianLocation.z, t.keypoints[7].cameraCartesianLocation.z);

            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.x, expectedMaxX, delta: 0.001f);
            Assert.AreEqual(t.keypoints[4].cameraCartesianLocation.x, expectedMinX, delta: 0.001f);
            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.y, expectedMinY, delta: 0.001f);
            Assert.AreEqual(t.keypoints[1].cameraCartesianLocation.y, expectedMaxY, delta: 0.001f);
            Assert.AreEqual(t.keypoints[0].cameraCartesianLocation.z, expectedMinZ, delta: 0.001f);
            Assert.AreEqual(t.keypoints[2].cameraCartesianLocation.z, expectedMaxZ, delta: 0.001f);
        }

        [UnityTest]
        public IEnumerator Keypoint_TestStaticLabeledCube_WithDisabledLabeling_AndSwitchingLabelingState()
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(new List<KeypointComponent>(data));
            }, texture);

            var cube = SetupLabeledCube(scale: 6, z: 8);
            var instanceId = cube.GetComponent<Labeling>().instanceId;
            SetupCubeJoints(cube, template);
            var labeling = cube.GetComponent<Labeling>();

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            labeling.enabled = false;
            yield return null;

            labeling.enabled = true;
            yield return null;

            labeling.enabled = false;
            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);
            texture.Release();

            var testCase = incoming[0];
            Assert.AreEqual(0, testCase.Count);

            testCase = incoming[1];
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(instanceId, t.instanceId);
            Assert.AreEqual(1, t.labelId);
            Assert.AreEqual(9, t.keypoints.Length);

            Assert.AreEqual(t.keypoints[0].location.x, t.keypoints[1].location.x);
            Assert.AreEqual(t.keypoints[2].location.x, t.keypoints[3].location.x);
            Assert.AreEqual(t.keypoints[4].location.x, t.keypoints[5].location.x);
            Assert.AreEqual(t.keypoints[6].location.x, t.keypoints[7].location.x);

            Assert.AreEqual(t.keypoints[0].location.y, t.keypoints[3].location.y);
            Assert.AreEqual(t.keypoints[1].location.y, t.keypoints[2].location.y);
            Assert.AreEqual(t.keypoints[4].location.y, t.keypoints[7].location.y);
            Assert.AreEqual(t.keypoints[5].location.y, t.keypoints[6].location.y);

            for (var i = 0; i < 9; i++) Assert.AreEqual(i, t.keypoints[i].index);
            for (var i = 0; i < 4; i++) Assert.AreEqual(2, t.keypoints[i].state);
            for (var i = 4; i < 8; i++) Assert.AreEqual(1, t.keypoints[i].state);
            Assert.Zero(t.keypoints[8].state);
            Assert.Zero(t.keypoints[8].location.x);
            Assert.Zero(t.keypoints[8].location.y);

            testCase = incoming[2];
            Assert.AreEqual(0, testCase.Count);
        }

        [UnityTest]
        public IEnumerator Keypoint_TestAllOffScreen()
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");

            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(new List<KeypointComponent>(data));
            });

            var cube = SetupLabeledCube(scale: 6, z: 8);
            SetupCubeJoints(cube, template);

            cube.transform.position = new Vector3(-1000, -1000, 0);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            yield return null;
            //force all async readbacks to complete
            DestroyTestObject(cam);

            foreach (var i in incoming)
            {
                Assert.Zero(i.Count);
            }
        }

        [UnityTest]
        public IEnumerator Keypoint_TestMovingCube()
        {
            var incoming = new List<List<KeypointComponent>>();

            var keypoints = new[]
            {
                new KeypointDefinition
                {
                    label = "Center",
                    associateToRig = false,
                    color = Color.black
                }
            };
            var template = ScriptableObject.CreateInstance<KeypointTemplate>();
            template.templateID = Guid.NewGuid().ToString();
            template.templateName = "label";
            template.jointTexture = null;
            template.skeletonTexture = null;
            template.keypoints = keypoints;
            template.skeleton = new SkeletonDefinition[0];

            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(new List<KeypointComponent>(data));
            }, texture);
            cam.GetComponent<PerceptionCamera>().showVisualizations = false;

            var cube = SetupLabeledCube(scale: 6, z: 8);
            var instanceId = cube.GetComponent<Labeling>().instanceId;
            SetupCubeJoint(cube, "Center", 0, 0, -.5f);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            yield return null;
            cube.transform.localPosition = new Vector3(-1, 0, 0);
            yield return null;


            //force all async readbacks to complete
            DestroyTestObject(cam);
            texture.Release();

            var testCase = incoming[0];
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(instanceId, t.instanceId);
            Assert.AreEqual(1, t.labelId);
            Assert.AreEqual(1, t.keypoints.Length);

            Assert.AreEqual(1024 / 2, t.keypoints[0].location.x);
            Assert.AreEqual(768 / 2, t.keypoints[0].location.y);

            Assert.AreEqual(0, t.keypoints[0].index);
            Assert.AreEqual(2, t.keypoints[0].state);

            var testCase2 = incoming[1];
            Assert.AreEqual(1, testCase2.Count);
            var t2 = testCase2.First();
            Assert.AreEqual(416, t2.keypoints[0].location.x, 1);
            Assert.AreEqual(768 / 2, t2.keypoints[0].location.y);
        }

        [UnityTest]
        public IEnumerator Keypoint_TestPartialOffScreen([Values(1, 5)] int framesToRunBeforeAsserting)
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(new List<KeypointComponent>(data));
            }, texture);

            var cube = SetupLabeledCube(scale: 6, z: 8);
            var instanceId = cube.GetComponent<Labeling>().instanceId;
            SetupCubeJoints(cube, template);
            cube.transform.position += Vector3.right * 13.5f;

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            for (var i = 0; i < framesToRunBeforeAsserting; i++)
                yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);
            if (texture != null) texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(instanceId, t.instanceId);
            Assert.AreEqual(1, t.labelId);
            Assert.AreEqual(9, t.keypoints.Length);

            Assert.NotZero(t.keypoints[0].state);
            Assert.NotZero(t.keypoints[1].state);
            Assert.NotZero(t.keypoints[4].state);
            Assert.NotZero(t.keypoints[5].state);

            Assert.Zero(t.keypoints[2].state);
            Assert.Zero(t.keypoints[3].state);
            Assert.Zero(t.keypoints[6].state);
            Assert.Zero(t.keypoints[7].state);

            for (var i = 0; i < 9; i++) Assert.AreEqual(i, t.keypoints[i].index);
            Assert.Zero(t.keypoints[8].state);
            Assert.Zero(t.keypoints[8].location.x);
            Assert.Zero(t.keypoints[8].location.y);
        }

        [UnityTest]
        public IEnumerator Keypoint_TestAllOnScreen()
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(new List<KeypointComponent>(data));
            }, texture);

            var cube = SetupLabeledCube(scale: 6, z: 8);
            var instanceId = cube.GetComponent<Labeling>().instanceId;
            SetupCubeJoints(cube, template);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            //for (int i = 0; i < 10000; i++)
            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);

            if (texture != null) texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(instanceId, t.instanceId);
            Assert.AreEqual(1, t.labelId);
            Assert.AreEqual(9, t.keypoints.Length);

            Assert.AreEqual(t.keypoints[0].location.x, t.keypoints[1].location.x);
            Assert.AreEqual(t.keypoints[2].location.x, t.keypoints[3].location.x);
            Assert.AreEqual(t.keypoints[4].location.x, t.keypoints[5].location.x);
            Assert.AreEqual(t.keypoints[6].location.x, t.keypoints[7].location.x);

            Assert.AreEqual(t.keypoints[0].location.y, t.keypoints[3].location.y);
            Assert.AreEqual(t.keypoints[1].location.y, t.keypoints[2].location.y);
            Assert.AreEqual(t.keypoints[4].location.y, t.keypoints[7].location.y);
            Assert.AreEqual(t.keypoints[5].location.y, t.keypoints[6].location.y);

            for (var i = 0; i < 9; i++) Assert.AreEqual(i, t.keypoints[i].index);
            for (var i = 0; i < 4; i++) Assert.AreEqual(2, t.keypoints[i].state);
            for (var i = 4; i < 8; i++) Assert.AreEqual(1, t.keypoints[i].state);
            Assert.Zero(t.keypoints[8].state);
            Assert.Zero(t.keypoints[8].location.x);
            Assert.Zero(t.keypoints[8].location.y);
        }

        [UnityTest]

        public IEnumerator Keypoint_FullyOccluded_DoesNotReport()
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(new List<KeypointComponent>(data));
            }, texture);

            CreateFullyOccludedScene(template, cam);

            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);

            if (texture != null) texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(0, testCase.Count);
        }

        [UnityTest]
        public IEnumerator Keypoint_FullyOccluded_WithIncludeOccluded_ReportsProperly(
            [Values(KeypointObjectFilter.VisibleAndOccluded, KeypointObjectFilter.All)] KeypointObjectFilter keypointObjectFilter)
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture, keypointObjectFilter);

            var instanceId = CreateFullyOccludedScene(template, cam);

            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);

            if (texture != null) texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);

            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(instanceId, t.instanceId);
            Assert.AreEqual(1, t.labelId);
            Assert.AreEqual(9, t.keypoints.Length);

            for (var i = 0; i < 8; i++)
                Assert.AreEqual(1, t.keypoints[i].state);

            for (var i = 0; i < 9; i++) Assert.AreEqual(i, t.keypoints[i].index);
            Assert.Zero(t.keypoints[8].state);
            Assert.Zero(t.keypoints[8].location.x);
            Assert.Zero(t.keypoints[8].location.y);
        }

        uint CreateFullyOccludedScene(KeypointTemplate template, GameObject cam)
        {
            var cube = SetupLabeledCube(scale: 6, z: 8);
            SetupCubeJoints(cube, template);

            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = new Vector3(0, 0, 5);
            blocker.transform.localScale = new Vector3(7, 7, 7);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);
            AddTestObjectForCleanup(blocker);

            return cube.GetComponent<Labeling>().instanceId;
        }

        [UnityTest]
        public IEnumerator Keypoint_Offscreen_DoesNotReport(
            [Values(KeypointObjectFilter.VisibleAndOccluded, KeypointObjectFilter.Visible)] KeypointObjectFilter keypointObjectFilter)
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture, keypointObjectFilter);

            var cube = SetupLabeledCube(scale: 6, z: -100);
            SetupCubeJoints(cube, template);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);

            if (texture != null) texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(0, testCase.Count);
        }

        [UnityTest]
        public IEnumerator Keypoint_Offscreen_WithIncludeAll_ReportsProperly()
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture, KeypointObjectFilter.All);

            var cube = SetupLabeledCube(scale: 6, z: -20);
            var instanceId = cube.GetComponent<Labeling>().instanceId;
            SetupCubeJoints(cube, template);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);

            if (texture != null) texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);

            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(instanceId, t.instanceId);
            Assert.AreEqual(1, t.labelId);
            Assert.AreEqual(9, t.keypoints.Length);

            for (var i = 0; i < 9; i++)
            {
                Assert.Zero(t.keypoints[i].state);
                Assert.Zero(t.keypoints[i].location.x);
                Assert.Zero(t.keypoints[i].location.y);
            }
        }

        [UnityTest]
        public IEnumerator Keypoint_TestPartiallyBlockedByOther()
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(new List<KeypointComponent>(data));
            }, texture);

            var cube = SetupLabeledCube(scale: 6, z: 8);
            var instanceId = cube.GetComponent<Labeling>().instanceId;
            SetupCubeJoints(cube, template);

            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = new Vector3(3, 0, 5);
            blocker.transform.localScale = new Vector3(7, 7, 7);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);
            AddTestObjectForCleanup(blocker);

            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);
            texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(instanceId, t.instanceId);
            Assert.AreEqual(1, t.labelId);
            Assert.AreEqual(9, t.keypoints.Length);

            Assert.AreEqual(2, t.keypoints[0].state);
            Assert.AreEqual(2, t.keypoints[1].state);
            Assert.AreEqual(1, t.keypoints[4].state);
            Assert.AreEqual(1, t.keypoints[5].state);

            Assert.AreEqual(1, t.keypoints[2].state);
            Assert.AreEqual(1, t.keypoints[3].state);
            Assert.AreEqual(1, t.keypoints[6].state);
            Assert.AreEqual(1, t.keypoints[7].state);

            for (var i = 0; i < 9; i++) Assert.AreEqual(i, t.keypoints[i].index);
            Assert.Zero(t.keypoints[8].state);
            Assert.Zero(t.keypoints[8].location.x);
            Assert.Zero(t.keypoints[8].location.y);
        }

        [UnityTest]

        public IEnumerator Keypoint_AnimatedCube_PositionsCaptured([Values("AnimatedCube", "NonAnimatedCube")] string testPrefix)
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");

            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(new List<KeypointComponent>(data));
            }, texture);

            var cameraComponent = cam.GetComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = 2;
            var screenPointCenterExpected = cameraComponent.WorldToScreenPoint(
                new Vector3(-1.0f, -1.0f * -1 /*flip y for image-space*/, -1.0f));

            cam.transform.position = new Vector3(0, 0, -10);

            // ReSharper disable once Unity.LoadSceneUnknownSceneName
            SceneManager.LoadScene($"{testPrefix}Scene", LoadSceneMode.Additive);
            AddSceneForCleanup($"{testPrefix}Scene");
            //scenes are loaded at the end of the frame
            yield return null;

            var cube = GameObject.Find(testPrefix);
            cube.SetActive(false);
            var labeling = cube.AddComponent<Labeling>();
            labeling.labels.Add("label");

            SetupCubeJoint(cube, "Center", 0f, 0f, -.5f);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);

            if (texture != null) texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(labeling.instanceId, t.instanceId);
            Assert.AreEqual(1, t.labelId);
            Assert.AreEqual(9, t.keypoints.Length);

            //large delta because the animation will already have taken it some distance from the starting location
            Assert.AreEqual(screenPointCenterExpected.x, t.keypoints[8].location.x, Screen.width * .2);
            Assert.AreEqual(screenPointCenterExpected.y, t.keypoints[8].location.y, Screen.height * .2);
            Assert.AreEqual(8, t.keypoints[8].index);
            Assert.AreEqual(2, t.keypoints[8].state);
        }

        static IEnumerable<(
            float scale,
            bool expectObject,
            int expectedStateFront,
            int expectedStateBack,
            KeypointObjectFilter keypointFilter,
            Vector2 expectedTopLeft,
            Vector2 expectedBottomRight)>
        Keypoint_OnBox_ReportsProperCoordinates_TestCases()
        {
            yield return (
                1,
                true,
                2,
                1,
                KeypointObjectFilter.Visible,
                new Vector2(0, 0),
                new Vector2(1023.99f, 1023.99f));
            yield return (
                1.001f,
                true,
                0,
                0,
                KeypointObjectFilter.Visible,
                new Vector2(0, 0),
                new Vector2(0, 0));
            yield return (
                1.2f,
                true,
                0,
                0,
                KeypointObjectFilter.Visible,
                new Vector2(0, 0),
                new Vector2(0, 0));
            yield return (
                0f,
                false,
                1,
                1,
                KeypointObjectFilter.Visible,
                new Vector2(512, 512),
                new Vector2(512, 512));
            yield return (
                0f,
                true,
                1,
                1,
                KeypointObjectFilter.VisibleAndOccluded,
                new Vector2(512, 512),
                new Vector2(512, 512));
        }

        [UnityTest]
        public IEnumerator Keypoint_OnBox_ReportsProperCoordinates(
            [ValueSource(nameof(Keypoint_OnBox_ReportsProperCoordinates_TestCases))]
                (float scale, bool expectObject, int expectedStateFront, int expectedStateBack, KeypointObjectFilter keypointFilter, Vector2 expectedTopLeft, Vector2 expectedBottomRight) args)
        {
            var incoming = new List<List<KeypointComponent>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var frameSize = 1024;
            var texture = new RenderTexture(frameSize, frameSize, 16);
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture, args.keypointFilter);

            var camComponent = cam.GetComponent<Camera>();
            camComponent.orthographic = true;
            camComponent.orthographicSize = .5f;


            //For some reason the back of this cube is being resolved to 7.5 away from the camera, but on the CPU side it is being recorded as 18.34375
            var cube = SetupLabeledCube(scale: args.scale, z: 0);
            var instanceId = cube.GetComponent<Labeling>().instanceId;
            SetupCubeJoints(cube, template);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            //for (int i = 0; i < 10000; i++)
            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);
            texture.Release();

            var testCase = incoming.Last();
            if (!args.expectObject)
            {
                Assert.AreEqual(0, testCase.Count);
                yield break;
            }
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(instanceId, t.instanceId);
            Assert.AreEqual(1, t.labelId);
            Assert.AreEqual(9, t.keypoints.Length);

            CollectionAssert.AreEqual(Enumerable.Repeat(args.expectedStateFront, 4),
                t.keypoints.Take(4).Select(k => k.state),
                "State mismatch on front");
            CollectionAssert.AreEqual(Enumerable.Repeat(args.expectedStateBack, 4),
                t.keypoints.Skip(4).Take(4).Select(k => k.state),
                "State mismatch on front");
            Assert.AreEqual(args.expectedTopLeft.x, t.keypoints[0].location.x, k_Delta);
            Assert.AreEqual(args.expectedBottomRight.y, t.keypoints[0].location.y, k_Delta);

            Assert.AreEqual(args.expectedTopLeft.x, t.keypoints[1].location.x, k_Delta);
            Assert.AreEqual(args.expectedTopLeft.y, t.keypoints[1].location.y, k_Delta);

            Assert.AreEqual(args.expectedBottomRight.x, t.keypoints[2].location.x, k_Delta);
            Assert.AreEqual(args.expectedTopLeft.y, t.keypoints[2].location.y, k_Delta);

            Assert.AreEqual(args.expectedBottomRight.x, t.keypoints[3].location.x, k_Delta);
            Assert.AreEqual(args.expectedBottomRight.y, t.keypoints[3].location.y, k_Delta);
        }

        public enum CheckDistanceType
        {
            Global,
            JointLabel
        }
        public enum ProjectionKind
        {
            Orthographic,
            Projection
        }

        public static IEnumerable<(CheckDistanceType checkDistanceType, Vector3 origin, Vector3 objectScale, Quaternion rotation,
            float checkDistance, float pointDistance, float cameraFieldOfView, bool expectOccluded)>
        Keypoint_InsideBox_RespectsThreshold_TestCases()
        {
            foreach (var checkDistanceType in new[] {CheckDistanceType.Global, CheckDistanceType.JointLabel})
            {
                yield return (
                    checkDistanceType,
                    Vector3.zero,
                    Vector3.one,
                    Quaternion.identity,
                    0.1f,
                    0.2f,
                    60f,
                    true);
                yield return (
                    checkDistanceType,
                    Vector3.zero,
                    Vector3.one,
                    Quaternion.identity,
                    0.2f,
                    0.005f,
                    60f,
                    false);
                yield return (
                    checkDistanceType,
                    Vector3.zero,
                    Vector3.one,
                    Quaternion.identity,
                    0.1f,
                    0.05f,
                    60f,
                    false);
                yield return (
                    checkDistanceType,
                    new Vector3(0, 0, 88),
                    Vector3.one,
                    Quaternion.identity,
                    0.1f,
                    0.05f,
                    1f,
                    false);
                //larger value here for the occluded check due to lack of depth precision close to far plane.
                //We choose to mark points not occluded when the point depth and geometry depth are the same in the depth buffer
                yield return (
                    checkDistanceType,
                    new Vector3(0, 0, 88),
                    Vector3.one,
                    Quaternion.identity,
                    1f,
                    2f,
                    1f,
                    true);
            }
            yield return (
                CheckDistanceType.Global,
                Vector3.zero,
                Vector3.one * .5f,
                Quaternion.identity,
                0.2f,
                0.3f,
                60f,
                true);
            yield return (
                CheckDistanceType.Global,
                Vector3.zero,
                new Vector3(1f, 1f, .5f),
                Quaternion.identity,
                0.2f,
                0.3f,
                60f,
                true);
            yield return (
                CheckDistanceType.JointLabel,
                Vector3.zero,
                new Vector3(1f, 1f, .5f),
                Quaternion.identity,
                0.2f,
                0.3f,
                60f,
                true);
            yield return (
                CheckDistanceType.JointLabel,
                Vector3.zero,
                Vector3.one * .5f,
                Quaternion.identity,
                0.2f,
                0.3f,
                60f,
                true);
            yield return (
                CheckDistanceType.JointLabel,
                Vector3.zero,
                new Vector3(1f, 1f, .05f),
                Quaternion.AngleAxis(45, Vector3.right),
                0.2f,
                0.21f,
                60f,
                true);
        }

        [UnityTest]
        public IEnumerator Keypoint_InsideBox_RespectsThreshold(
            [ValueSource(nameof(Keypoint_InsideBox_RespectsThreshold_TestCases))]
                (CheckDistanceType checkDistanceType, Vector3 origin, Vector3 objectScale, Quaternion rotation,
                float checkDistance, float pointDistance, float cameraFieldOfView, bool expectOccluded) args,
            [Values(ProjectionKind.Orthographic, ProjectionKind.Projection)] ProjectionKind projectionKind)
        {
            var incoming = new List<List<KeypointComponent>>();
            var labelerSelfOcclusionDistance =
                args.checkDistanceType == CheckDistanceType.Global ? args.checkDistance : 0.5f;
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate", selfOcclusionDistance: labelerSelfOcclusionDistance);
            var frameSize = 1024;
            var texture = new RenderTexture(frameSize, frameSize, 16);
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture);
            var camComponent = cam.GetComponent<Camera>();
            camComponent.fieldOfView = args.cameraFieldOfView;
            camComponent.farClipPlane = 100f;

            if (projectionKind == ProjectionKind.Orthographic)
            {
                camComponent.orthographic = true;
                camComponent.orthographicSize = .5f;
            }
            var cube = GameObject.Find("Cube");
            TestHelper.SetupLabeledObject(cube, scale: 1f, x: args.origin.x, y: args.origin.y, z: args.origin.z);
            cube.transform.localScale = args.objectScale;
            cube.transform.localRotation = args.rotation;
            var localSelfOcclusionDistance = args.checkDistanceType == CheckDistanceType.JointLabel ? (float?)args.checkDistance : null;
            SetupCubeJoint(cube, "Center", 0f, 0f, args.pointDistance - 0.5f, localSelfOcclusionDistance);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            //for (int i = 0; i < 10000; i++)
            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);
            texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.AreEqual(args.expectOccluded ? 1 : 2, t.keypoints[8].state);
        }

        private IEnumerable LoadCubeScene()
        {
            SceneManager.LoadScene("CubeScene", LoadSceneMode.Additive);
            AddSceneForCleanup("CubeScene");
            yield return null;
        }

        public static IEnumerable<(Vector3 objectScale, Quaternion rotation, float checkDistance, Vector3 pointLocalPosition, bool expectOccluded)>
        Keypoint_OnCorner_OfRotatedScaledBox_RespectsThreshold_TestCases()
        {
            yield return (
                new Vector3(90f, 90f, 10f),
                Quaternion.identity,
                .11f,
                new Vector3(-.4f, -.4f, -.4f),
                false);
            yield return (
                new Vector3(90f, 90f, 1f),
                Quaternion.identity,
                .5f,
                new Vector3(-.4f, -.4f, .4f),
                true);
            yield return (
                new Vector3(90, 90, 9),
                Quaternion.AngleAxis(90, Vector3.right),
                .11f,
                new Vector3(-.4f, -.4f, -.4f),
                false);
            yield return (
                new Vector3(90, 90, 90),
                Quaternion.AngleAxis(90, Vector3.right),
                .11f,
                new Vector3(-.4f, -.4f, -.4f),
                false);
            yield return (
                new Vector3(90, 60, 90),
                Quaternion.AngleAxis(45, Vector3.right),
                .11f,
                new Vector3(-.4f, -.4f, -.4f),
                true);
        }

        [UnityTest]
        public IEnumerator Keypoint_OnCorner_OfRotatedScaledBox_RespectsThreshold(
            [ValueSource(nameof(Keypoint_OnCorner_OfRotatedScaledBox_RespectsThreshold_TestCases))]
                (Vector3 objectScale, Quaternion rotation, float checkDistance, Vector3 pointLocalPosition, bool expectOccluded) args)
        {
            var incoming = new List<List<KeypointComponent>>();
            var labelerSelfOcclusionDistance = 0.5f;
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate", labelerSelfOcclusionDistance);
            var frameSize = 1024;
            var texture = new RenderTexture(frameSize, frameSize, 16);
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture);
            var camComponent = cam.GetComponent<Camera>();
            camComponent.orthographic = true;
            camComponent.orthographicSize = 100f;
            cam.transform.localPosition = new Vector3(0, 0, -95f);

            var cube = GameObject.Find("Cube");
            TestHelper.SetupLabeledObject(cube, scale: 1f);
            cube.transform.localScale = args.objectScale;
            cube.transform.localRotation = args.rotation;
            SetupCubeJoint(cube, "Center", args.pointLocalPosition.x, args.pointLocalPosition.y, args.pointLocalPosition.z, args.checkDistance);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            //for (int i = 0; i < 10000; i++)
            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);
            texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.AreEqual(args.expectOccluded ? 1 : 2, t.keypoints[8].state);
        }

        public static IEnumerable<(Vector3 objectScale, Quaternion rotation, float checkDistance, Vector3 pointLocalPosition, float overrideScalar, bool expectOccluded)>
        Keypoint_OnCorner_OfRotatedScaledBox_RespectsModelOverrideThreshold_TestCases()
        {
            yield return (
                new Vector3(90f, 90f, 10f),
                Quaternion.identity,
                .11f,
                new Vector3(-.4f, -.4f, -.4f),
                1f,
                false);
            yield return (
                new Vector3(90f, 90f, 1f),
                Quaternion.identity,
                .5f,
                new Vector3(-.4f, -.4f, .4f),
                .5f,
                true);
            yield return (
                new Vector3(90, 90, 9),
                Quaternion.AngleAxis(90, Vector3.right),
                .11f,
                new Vector3(-.4f, -.4f, -.4f),
                1f,
                false);
            yield return (
                new Vector3(90, 90, 90),
                Quaternion.AngleAxis(90, Vector3.right),
                .11f,
                new Vector3(-.4f, -.4f, -.4f),
                .5f,
                true);
            yield return (
                new Vector3(90, 90, 90),
                Quaternion.AngleAxis(90, Vector3.right),
                .055f,
                new Vector3(-.4f, -.4f, -.4f),
                1f,
                true);
            yield return (
                new Vector3(90, 90, 90),
                Quaternion.AngleAxis(90, Vector3.right),
                .055f,
                new Vector3(-.4f, -.4f, -.4f),
                2f,
                false);
        }

        [UnityTest]
        public IEnumerator Keypoint_OnCorner_OfRotatedScaledBox_RespectsModelOverrideThreshold(
            [ValueSource(nameof(Keypoint_OnCorner_OfRotatedScaledBox_RespectsModelOverrideThreshold_TestCases))]
                (Vector3 objectScale, Quaternion rotation, float checkDistance, Vector3 pointLocalPosition, float overrideScalar, bool expectOccluded) args)
        {
            var incoming = new List<List<KeypointComponent>>();
            var labelerSelfOcclusionDistance = 0.5f;
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate", labelerSelfOcclusionDistance);
            var frameSize = 1024;
            var texture = new RenderTexture(frameSize, frameSize, 16);
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture);
            var camComponent = cam.GetComponent<Camera>();
            camComponent.orthographic = true;
            camComponent.orthographicSize = 100f;
            cam.transform.localPosition = new Vector3(0, 0, -95f);

            var cube = SetupLabeledCube(scale: 1f);
            cube.transform.localScale = args.objectScale;
            cube.transform.localRotation = args.rotation;
            SetupCubeJoint(cube, "Center", args.pointLocalPosition.x, args.pointLocalPosition.y, args.pointLocalPosition.z, args.checkDistance);

            var kpOc = cube.AddComponent<KeypointOcclusionOverrides>();
            kpOc.distanceScale = args.overrideScalar;

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            //for (int i = 0; i < 10000; i++)
            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);
            texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.AreEqual(args.expectOccluded ? 1 : 2, t.keypoints[8].state);
        }

        public static GameObject SetupLabeledCube(float scale = 10, string label = "label", float x = 0, float y = 0,
            float z = 0, float roll = 0, float pitch = 0, float yaw = 0)
        {
            return TestHelper.SetupLabeledObject(GameObject.Find("Cube"), scale, label, x, y, z, roll, pitch, yaw);
        }

        [UnityTest]
        public IEnumerator ManyObjects_LabelsCorrectly()
        {
            var incoming = new List<List<KeypointComponent>>();
            var labelerSelfOcclusionDistance = 0.5f;
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate", selfOcclusionDistance: labelerSelfOcclusionDistance);
            var frameSize = 1024;
            var texture = new RenderTexture(frameSize, frameSize, 16);
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(new List<KeypointComponent>(data));
            }, texture);

            void PlaceObjects(Rect rect, float z, Vector2Int count)
            {
                var cubeBase = GameObject.Find("Cube");
                for (int x = 0; x < count.x; x++)
                {
                    for (int y = 0; y < count.y; y++)
                    {
                        var cube = GameObject.Instantiate(cubeBase);
                        TestHelper.SetupLabeledObject(
                            cube,
                            scale: rect.width / count.x - .001f,
                            x: rect.width / count.x * x + rect.xMin,
                            y: rect.height / count.y * y + rect.yMin,
                            z: z);
                        SetupCubeJoints(cube, template, .1f);
                        cube.SetActive(true);
                        AddTestObjectForCleanup(cube);
                    }
                }
            }

            PlaceObjects(new Rect(-2, -2, 2, 2), 0, new Vector2Int(10, 10));
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);

            // TestHelper.LoadAndStartRenderDocCapture();
            yield return null;

            PlaceObjects(new Rect(0, 0, 4, 4), 0, new Vector2Int(25, 25));

            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);
            texture.Release();

            // TestHelper.EndCaptureRenderDoc();

            Assert.AreEqual(2, incoming.Count);

            Assert.AreEqual(10 * 10, incoming[0].Count);
            Assert.AreEqual(10 * 10 + 25 * 25, incoming[1].Count);
            var idx = 0;
            foreach (var entry in incoming[0].Concat(incoming[1]))
            {
                Assert.AreEqual(9, entry.keypoints.Length);

                CollectionAssert.AreEqual(Enumerable.Repeat(2, 4),
                    entry.keypoints.Take(4).Select(k => k.state),
                    $"State mismatch on front in entry {idx}");
                CollectionAssert.AreEqual(Enumerable.Repeat(1, 4),
                    entry.keypoints.Skip(4).Take(4).Select(k => k.state),
                    $"State mismatch on back in entry {idx}");
                idx++;
            }
        }

        [UnityTest]
        public IEnumerator Keypoint_DeleteAndRecreateForegroundRandomizer()
        {
            SceneManager.LoadScene("Keypoint_Null_Check_On_Animator", LoadSceneMode.Additive);
            AddSceneForCleanup("Keypoint_Null_Check_On_Animator");
            yield return null;

            var prefab = GameObject.Find("LabeledAndRandomized");

            var scenarioGO = new GameObject("scenario");
            AddTestObjectForCleanup(scenarioGO);
            var scenario = scenarioGO.AddComponent<FixedLengthScenario>();
            var randomizer = new DeleteAndRecreateForegroundRandomizer();

            randomizer.prefabs = new CategoricalParameter<GameObject>();
            randomizer.prefabs.SetOptions(new[] { prefab });

            scenario.AddRandomizer(randomizer);
            scenario.AddRandomizer(new AnimationRandomizer());
            scenario.constants.iterationCount = 10;
            scenario.constants.totalIterations = 10;
            scenario.constants.startIteration = 0;

            // wait for a few frames
            for (var i = 0; i < 6; i++)
            {
                yield return null;
            }

            //No error logs should be at this point
            Assert.AreEqual(0, 0);
        }

        [UnityTest]
        public IEnumerator Keypoint_ForegroundObjectPlacementRandomizer()
        {
            SceneManager.LoadScene("Keypoint_Null_Check_On_Animator_Foreground", LoadSceneMode.Additive);
            AddSceneForCleanup("Keypoint_Null_Check_On_Animator_Foreground");
            yield return null;

            var prefab = GameObject.Find("LabeledAndRandomized_Foreground");
            var scenarioGO = new GameObject("scenario");
            AddTestObjectForCleanup(scenarioGO);

            var scenario = scenarioGO.AddComponent<FixedLengthScenario>();
            var randomizer = new ForegroundObjectPlacementRandomizer();
            randomizer.placementArea = new Vector2(1, 10);
            randomizer.depth = 5;
            randomizer.prefabs = new CategoricalParameter<GameObject>();
            randomizer.prefabs.SetOptions(new[] { prefab });

            scenario.AddRandomizer(randomizer);
            scenario.AddRandomizer(new AnimationRandomizer());
            scenario.constants.iterationCount = 10;
            scenario.constants.totalIterations = 10;
            scenario.constants.startIteration = 0;

            // wait for a few frames
            for (var i = 0; i < 6; i++)
            {
                yield return null;
            }

            //No error logs should be at this point
            Assert.AreEqual(0, 0);
        }

        [AddRandomizerMenu("")]
        internal class DeleteAndRecreateForegroundRandomizer : Randomizer
        {
            /// <summary>
            /// The list of prefabs sample and randomly place
            /// </summary>
            [Tooltip("The list of Prefabs to be placed by this Randomizer.")]
            public CategoricalParameter<GameObject> prefabs;

            GameObject m_Instance;

            /// <inheritdoc/>
            protected override void OnAwake()
            {
            }

            /// <inheritdoc/>
            protected override void OnDestroy()
            {
                if (m_Instance != null)
                {
                    GameObject.Destroy(m_Instance);
                    m_Instance = null;
                }
            }

            /// <summary>
            /// Generates a foreground layer of objects at the start of each scenario iteration
            /// </summary>
            protected override void OnIterationStart()
            {
                if (m_Instance != null)
                {
                    GameObject.Destroy(m_Instance);
                    m_Instance = null;
                }
                m_Instance = GameObject.Instantiate(prefabs.Sample());
                m_Instance.SetActive(true);
                m_Instance.transform.position = Vector3.zero;
            }

            /// <summary>
            /// Deletes generated foreground objects after each scenario iteration is complete
            /// </summary>
            protected override void OnIterationEnd()
            {
                if (m_Instance != null)
                {
                    GameObject.Destroy(m_Instance);
                    m_Instance = null;
                }
            }
        }
    }
}
