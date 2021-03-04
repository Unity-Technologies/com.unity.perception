using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class KeyPointGroundTruthTests : GroundTruthTestBase, IPrebuildSetup, IPostBuildCleanup
    {
        private const string kAnimatedCubeScenePath = "Packages/com.unity.perception/Tests/Runtime/TestAssets/AnimatedCubeScene.unity";

        public void Setup()
        {
#if UNITY_EDITOR
            var scenes = UnityEditor.EditorBuildSettings.scenes.ToList();
            scenes.Add(new UnityEditor.EditorBuildSettingsScene(kAnimatedCubeScenePath, true));
            UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            var scenes = UnityEditor.EditorBuildSettings.scenes;
            scenes = scenes.Where(s => s.path != kAnimatedCubeScenePath).ToArray();
            UnityEditor.EditorBuildSettings.scenes = scenes;
#endif
        }

        static GameObject SetupCamera(IdLabelConfig config, KeypointTemplate template, Action<int, List<KeypointLabeler.KeypointEntry>> computeListener, RenderTexture renderTexture = null)
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
            if (computeListener != null)
                keyPointLabeler.KeypointsComputed += computeListener;

            perceptionCamera.AddLabeler(keyPointLabeler);

            return cameraObject;
        }

        static KeypointTemplate CreateTestTemplate(Guid guid, string label)
        {
            var keypoints = new[]
            {
                new KeypointDefinition
                {
                    label = "FrontLowerLeft",
                    associateToRig = false,
                    color = Color.black
                },
                new KeypointDefinition
                {
                    label = "FrontUpperLeft",
                    associateToRig = false,
                    color = Color.black
                },
                new KeypointDefinition
                {
                    label = "FrontUpperRight",
                    associateToRig = false,
                    color = Color.black
                },
                new KeypointDefinition
                {
                    label = "FrontLowerRight",
                    associateToRig = false,
                    color = Color.black
                },
                new KeypointDefinition
                {
                    label = "BackLowerLeft",
                    associateToRig = false,
                    color = Color.black
                },
                new KeypointDefinition
                {
                    label = "BackUpperLeft",
                    associateToRig = false,
                    color = Color.black
                },
                new KeypointDefinition
                {
                    label = "BackUpperRight",
                    associateToRig = false,
                    color = Color.black
                },
                new KeypointDefinition
                {
                    label = "BackLowerRight",
                    associateToRig = false,
                    color = Color.black
                },
                new KeypointDefinition
                {
                    label = "Center",
                    associateToRig = false,
                    color = Color.black
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

        static void SetupCubeJoint(GameObject cube, KeypointTemplate template, string label, float x, float y, float z)
        {
            var joint = new GameObject();
            joint.transform.parent = cube.transform;
            joint.transform.localPosition = new Vector3(x, y, z);
            var jointLabel = joint.AddComponent<JointLabel>();
            jointLabel.templateInformation = new List<JointLabel.TemplateData>();
            var templateData = new JointLabel.TemplateData
            {
                template = template,
                label = label
            };
            jointLabel.templateInformation.Add(templateData);
        }

        static void SetupCubeJoints(GameObject cube, KeypointTemplate template)
        {
            SetupCubeJoint(cube, template, "FrontLowerLeft", -0.495f, -0.495f, -0.495f);
            SetupCubeJoint(cube, template, "FrontUpperLeft", -0.495f, 0.495f, -0.495f);
            SetupCubeJoint(cube, template, "FrontUpperRight", 0.495f, 0.495f, -0.495f);
            SetupCubeJoint(cube, template, "FrontLowerRight", 0.495f, -0.495f, -0.495f);
            SetupCubeJoint(cube, template, "BackLowerLeft", -0.495f, -0.495f, 0.495f);
            SetupCubeJoint(cube, template, "BackUpperLeft", -0.495f, 0.495f, 0.495f);
            SetupCubeJoint(cube, template, "BackUpperRight", 0.495f, 0.495f, 0.495f);
            SetupCubeJoint(cube, template, "BackLowerRight", 0.495f, -0.495f, 0.495f);
        }

        [UnityTest]
        public IEnumerator Keypoint_TestStaticLabeledCube()
        {
            var incoming = new List<List<KeypointLabeler.KeypointEntry>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture);

            var cube = TestHelper.CreateLabeledCube(scale: 6, z: 8);
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
            Assert.AreEqual(1, t.instance_id);
            Assert.AreEqual(1, t.label_id);
            Assert.AreEqual(template.templateID.ToString(), t.template_guid);
            Assert.AreEqual(9, t.keypoints.Length);

            Assert.AreEqual(t.keypoints[0].x, t.keypoints[1].x);
            Assert.AreEqual(t.keypoints[2].x, t.keypoints[3].x);
            Assert.AreEqual(t.keypoints[4].x, t.keypoints[5].x);
            Assert.AreEqual(t.keypoints[6].x, t.keypoints[7].x);

            Assert.AreEqual(t.keypoints[0].y, t.keypoints[3].y);
            Assert.AreEqual(t.keypoints[1].y, t.keypoints[2].y);
            Assert.AreEqual(t.keypoints[4].y, t.keypoints[7].y);
            Assert.AreEqual(t.keypoints[5].y, t.keypoints[6].y);

            for (var i = 0; i < 9; i++) Assert.AreEqual(i, t.keypoints[i].index);
            for (var i = 0; i < 8; i++) Assert.AreEqual(2, t.keypoints[i].state);
            Assert.Zero(t.keypoints[8].state);
            Assert.Zero(t.keypoints[8].x);
            Assert.Zero(t.keypoints[8].y);
        }

        [UnityTest]
        public IEnumerator Keypoint_TestAllOffScreen()
        {
            var incoming = new List<List<KeypointLabeler.KeypointEntry>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");

            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            });

            var cube = TestHelper.CreateLabeledCube(scale: 6, z: 8);
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
        public IEnumerator Keypoint_TestPartialOffScreen([Values(1,5)] int framesToRunBeforeAsserting)
        {
            var incoming = new List<List<KeypointLabeler.KeypointEntry>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture);

            var cube = TestHelper.CreateLabeledCube(scale: 6, z: 8);
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
            Assert.AreEqual(1, t.instance_id);
            Assert.AreEqual(1, t.label_id);
            Assert.AreEqual(template.templateID.ToString(), t.template_guid);
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
            Assert.Zero(t.keypoints[8].x);
            Assert.Zero(t.keypoints[8].y);
        }

        [UnityTest]
        public IEnumerator Keypoint_TestAllOnScreen()
        {
            var incoming = new List<List<KeypointLabeler.KeypointEntry>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture);

            var cube = TestHelper.CreateLabeledCube(scale: 6, z: 8);
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
            Assert.AreEqual(1, t.instance_id);
            Assert.AreEqual(1, t.label_id);
            Assert.AreEqual(template.templateID.ToString(), t.template_guid);
            Assert.AreEqual(9, t.keypoints.Length);

            Assert.AreEqual(t.keypoints[0].x, t.keypoints[1].x);
            Assert.AreEqual(t.keypoints[2].x, t.keypoints[3].x);
            Assert.AreEqual(t.keypoints[4].x, t.keypoints[5].x);
            Assert.AreEqual(t.keypoints[6].x, t.keypoints[7].x);

            Assert.AreEqual(t.keypoints[0].y, t.keypoints[3].y);
            Assert.AreEqual(t.keypoints[1].y, t.keypoints[2].y);
            Assert.AreEqual(t.keypoints[4].y, t.keypoints[7].y);
            Assert.AreEqual(t.keypoints[5].y, t.keypoints[6].y);

            for (var i = 0; i < 9; i++) Assert.AreEqual(i, t.keypoints[i].index);
            for (var i = 0; i < 8; i++) Assert.AreEqual(2, t.keypoints[i].state);
            Assert.Zero(t.keypoints[8].state);
            Assert.Zero(t.keypoints[8].x);
            Assert.Zero(t.keypoints[8].y);


        }

        [UnityTest]
        public IEnumerator Keypoint_TestAllBlockedByOther()
        {
            var incoming = new List<List<KeypointLabeler.KeypointEntry>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture);

            var cube = TestHelper.CreateLabeledCube(scale: 6, z: 8);
            SetupCubeJoints(cube, template);

            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = new Vector3(0, 0, 5);
            blocker.transform.localScale = new Vector3(7, 7, 7);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);
            AddTestObjectForCleanup(blocker);

            yield return null;

            //force all async readbacks to complete
            DestroyTestObject(cam);

            if (texture != null) texture.Release();

            var testCase = incoming.Last();
            Assert.AreEqual(1, testCase.Count);
            var t = testCase.First();
            Assert.NotNull(t);
            Assert.AreEqual(1, t.instance_id);
            Assert.AreEqual(1, t.label_id);
            Assert.AreEqual(template.templateID.ToString(), t.template_guid);
            Assert.AreEqual(9, t.keypoints.Length);

            for (var i = 0; i < 8; i++)
                Assert.AreEqual(1, t.keypoints[i].state);

            for (var i = 0; i < 9; i++) Assert.AreEqual(i, t.keypoints[i].index);
            Assert.Zero(t.keypoints[8].state);
            Assert.Zero(t.keypoints[8].x);
            Assert.Zero(t.keypoints[8].y);
        }

        [UnityTest]
        public IEnumerator Keypoint_TestPartiallyBlockedByOther()
        {
            var incoming = new List<List<KeypointLabeler.KeypointEntry>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");
            var texture = new RenderTexture(1024, 768, 16);
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture);

            var cube = TestHelper.CreateLabeledCube(scale: 6, z: 8);
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
            Assert.AreEqual(1, t.instance_id);
            Assert.AreEqual(1, t.label_id);
            Assert.AreEqual(template.templateID.ToString(), t.template_guid);
            Assert.AreEqual(9, t.keypoints.Length);

            Assert.AreEqual(2, t.keypoints[0].state);
            Assert.AreEqual(2, t.keypoints[1].state);
            Assert.AreEqual(2, t.keypoints[4].state);
            Assert.AreEqual(2, t.keypoints[5].state);

            Assert.AreEqual(1, t.keypoints[2].state);
            Assert.AreEqual(1, t.keypoints[3].state);
            Assert.AreEqual(1, t.keypoints[6].state);
            Assert.AreEqual(1, t.keypoints[7].state);

            for (var i = 0; i < 9; i++) Assert.AreEqual(i, t.keypoints[i].index);
            Assert.Zero(t.keypoints[8].state);
            Assert.Zero(t.keypoints[8].x);
            Assert.Zero(t.keypoints[8].y);
        }


        [UnityTest]
        public IEnumerator Keypoint_AnimatedCube_PositionsCaptured()
        {
            var incoming = new List<List<KeypointLabeler.KeypointEntry>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");

            var texture = new RenderTexture(1024, 768, 16);
            texture.Create();
            var cam = SetupCamera(SetUpLabelConfig(), template, (frame, data) =>
            {
                incoming.Add(data);
            }, texture);

            var cameraComponent = cam.GetComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = 2;
            var screenPointCenterExpected = cameraComponent.WorldToScreenPoint(
                new Vector3(-1.0f, -1.0f * -1 /*flip y for image-space*/, -1.0f));

            cam.transform.position = new Vector3(0, 0, -10);

            // ReSharper disable once Unity.LoadSceneUnknownSceneName
            SceneManager.LoadScene("AnimatedCubeScene", LoadSceneMode.Additive);
            AddSceneForCleanup("AnimatedCubeScene");
            //scenes are loaded at the end of the frame
            yield return null;

            var cube = GameObject.Find("AnimatedCube");
            cube.SetActive(false);
            var labeling = cube.AddComponent<Labeling>();
            labeling.labels.Add("label");

            SetupCubeJoint(cube, template, "Center",0f, 0f, 0f);

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
            Assert.AreEqual(1, t.instance_id);
            Assert.AreEqual(1, t.label_id);
            Assert.AreEqual(template.templateID.ToString(), t.template_guid);
            Assert.AreEqual(9, t.keypoints.Length);

            //large delta because the animation will already have taken it some distance from the starting location
            Assert.AreEqual(screenPointCenterExpected.x, t.keypoints[8].x, Screen.width * .1);
            Assert.AreEqual(screenPointCenterExpected.y, t.keypoints[8].y, Screen.height * .1);
            Assert.AreEqual(8, t.keypoints[8].index);
            Assert.AreEqual(2, t.keypoints[8].state);
        }
    }
}
