using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class KeyPointGroundTruthTests : GroundTruthTestBase
    {
        static GameObject SetupCamera(IdLabelConfig config, KeyPointTemplate template, Action<List<KeyPointLabeler.KeyPointEntry>> computeListener)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 60;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000;

            camera.transform.position = new Vector3(0, 0, -10);

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            var keyPointLabeler = new KeyPointLabeler(config, template);
            if (computeListener != null)
                keyPointLabeler.KeyPointsComputed += computeListener;

            perceptionCamera.AddLabeler(keyPointLabeler);

            return cameraObject;
        }

        static KeyPointTemplate CreateTestTemplate(Guid guid, string label)
        {
            var keyPoints = new[]
            {
                new KeyPointDefinition
                {
                    label = "FrontLowerLeft",
                    associateToRig = false,
                    color = Color.black
                },
                new KeyPointDefinition
                {
                    label = "FrontUpperLeft",
                    associateToRig = false,
                    color = Color.black
                },
                new KeyPointDefinition
                {
                    label = "FrontUpperRight",
                    associateToRig = false,
                    color = Color.black
                },
                new KeyPointDefinition
                {
                    label = "FrontLowerRight",
                    associateToRig = false,
                    color = Color.black
                },
                new KeyPointDefinition
                {
                    label = "BackLowerLeft",
                    associateToRig = false,
                    color = Color.black
                },
                new KeyPointDefinition
                {
                    label = "BackUpperLeft",
                    associateToRig = false,
                    color = Color.black
                },
                new KeyPointDefinition
                {
                    label = "BackUpperRight",
                    associateToRig = false,
                    color = Color.black
                },
                new KeyPointDefinition
                {
                    label = "BackLowerRight",
                    associateToRig = false,
                    color = Color.black
                },
                new KeyPointDefinition
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

            var template = ScriptableObject.CreateInstance<KeyPointTemplate>();
            template.templateID = guid.ToString();
            template.templateName = label;
            template.jointTexture = null;
            template.skeletonTexture = null;
            template.keyPoints = keyPoints;
            template.skeleton = skeleton;

            return template;
        }

        [Test]
        public void KeypointTemplate_CreateTemplateTest()
        {
            var guid = Guid.NewGuid();
            const string label = "TestTemplate";
            var template = CreateTestTemplate(guid, label);

            Assert.AreEqual(template.templateID, guid);
            Assert.AreEqual(template.templateName, label);
            Assert.IsNull(template.jointTexture);
            Assert.IsNull(template.skeletonTexture);
            Assert.IsNotNull(template.keyPoints);
            Assert.IsNotNull(template.skeleton);
            Assert.AreEqual(template.keyPoints.Length, 9);
            Assert.AreEqual(template.skeleton.Length, 12);

            var k0 = template.keyPoints[0];
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

        static void SetupCubeJoint(GameObject cube, KeyPointTemplate template, string label, float x, float y, float z)
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

        static void SetupCubeJoints(GameObject cube, KeyPointTemplate template)
        {
            SetupCubeJoint(cube, template, "FrontLowerLeft", -0.5f, -0.5f, -0.5f);
            SetupCubeJoint(cube, template, "FrontUpperLeft", -0.5f, 0.5f, -0.5f);
            SetupCubeJoint(cube, template, "FrontUpperRight", 0.5f, 0.5f, -0.5f);
            SetupCubeJoint(cube, template, "FrontLowerRight", 0.5f, -0.5f, -0.5f);
            SetupCubeJoint(cube, template, "BackLowerLeft", -0.5f, -0.5f, 0.5f);
            SetupCubeJoint(cube, template, "BackUpperLeft", -0.5f, 0.5f, 0.5f);
            SetupCubeJoint(cube, template, "BackUpperRight", 0.5f, 0.5f, 0.5f);
            SetupCubeJoint(cube, template, "BackLowerRight", 0.5f, -0.5f, 0.5f);
        }

        [UnityTest]
        public IEnumerator Keypoint_TestStaticLabeledCube()
        {
            var incoming = new List<List<KeyPointLabeler.KeyPointEntry>>();
            var template = CreateTestTemplate(Guid.NewGuid(), "TestTemplate");

            var cam = SetupCamera(SetUpLabelConfig(), template, (data) =>
            {
                incoming.Add(data);
            });

            var cube = TestHelper.CreateLabeledCube(scale: 6, z: 8);
            SetupCubeJoints(cube, template);

            cube.SetActive(true);
            cam.SetActive(true);

            AddTestObjectForCleanup(cam);
            AddTestObjectForCleanup(cube);

            yield return null;
            yield return null;

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
            for (var i = 0; i < 8; i++) Assert.AreEqual(1, t.keypoints[i].state);
            Assert.Zero(t.keypoints[8].state);
            Assert.Zero(t.keypoints[8].x);
            Assert.Zero(t.keypoints[8].y);
        }
    }
}
