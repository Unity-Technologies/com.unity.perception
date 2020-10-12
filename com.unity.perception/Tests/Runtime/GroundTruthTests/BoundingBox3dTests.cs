using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
   [TestFixture]
    public class BoundingBox3dTests : GroundTruthTestBase
    {
        const float k_Delta = 0.0001f;

        static string PrintBox(BoundingBox3DLabeler.BoxData box)
        {
            var sb = new StringBuilder();
            sb.Append("label id: " + box.label_id + " ");
            sb.Append("label_name: " + box.label_name + " ");
            sb.Append("instance_id: " + box.instance_id + " ");
            sb.Append("translation: (" + box.translation[0] + ", " + box.translation[1] + ", " + box.translation[2] + ") ");
            sb.Append("size: (" + box.size[0] + ", " + box.size[1] + ", " + box.size[2] + ") ");
            sb.Append("rotation: " + box.rotation[0] + ", " + box.rotation[1] + ", " + box.rotation[2] + ", " + box.rotation[3] + ") ");
            sb.Append("velocity: " + box.velocity[0] + ", " + box.velocity[1] + ", " + box.velocity[2]);
            sb.Append("acceleration: (" + box.acceleration[0] + ", " + box.acceleration[1] + ", " + box.acceleration[2] + ")");

            return sb.ToString();
        }

        [UnityTest]
        public IEnumerator CameraOffset_ProduceProperTranslationTest()
        {
            var expected = new[]
            {
                new ExpectedResult
                {
                    instanceId = 1,
                    labelId = 1,
                    labelName = "label",
                    position = new Vector3(0, 0, 10),
                    scale = new Vector3(5, 5, 5),
                    rotation = Quaternion.identity
                }
            };
            var target = TestHelper.CreateLabeledCube();
            var cameraPosition = new Vector3(0, 0, -10);
            var cameraRotation = Quaternion.identity;
            return ExecuteTest(target, cameraPosition, cameraRotation, expected);
        }

        [UnityTest]
        public IEnumerator CameraOffsetAndRotated_ProduceProperTranslationTest()
        {
            var expected = new[]
            {
                new ExpectedResult
                {
                    instanceId = 1,
                    labelId = 1,
                    labelName = "label",
                    position = new Vector3(0, 0, Mathf.Sqrt(200)),
                    scale = new Vector3(5, 5, 5),
                    rotation = Quaternion.identity
                }
            };
            var target = TestHelper.CreateLabeledCube(x: 10, y: 0, z: 10, yaw: 45);
            var cameraPosition = new Vector3(0, 0, 0);
            var cameraRotation = Quaternion.Euler(0, 45, 0);
            return ExecuteTest(target, cameraPosition, cameraRotation, expected);
        }

        [UnityTest]
        public IEnumerator SimpleMultiMesh_ProduceProperTranslationTest()
        {
            var expected = new[]
            {
                new ExpectedResult
                {
                    instanceId = 1,
                    labelId = 1,
                    labelName = "label",
                    position = new Vector3(0, 0, 10),
                    scale = new Vector3(6.5f, 2.5f, 2.5f),
                    rotation = Quaternion.identity
                }
            };
            var target = CreateMultiMeshGameObject();
            target.transform.position = Vector3.zero;
            var cameraPosition = new Vector3(0, 0, -10);
            var cameraRotation = Quaternion.identity;
            return ExecuteTest(target, cameraPosition, cameraRotation, expected);
        }

        [UnityTest]
        public IEnumerator MultiInheritedMesh_ProduceProperTranslationTest()
        {
            var expected = new[]
            {
                new ExpectedResult
                {
                    instanceId = 1,
                    labelId = 2,
                    labelName = "car",
                    position = new Vector3(0, 0.525f, 20),
                    scale = new Vector3(2f, 0.875f, 2.4f),
                    rotation = Quaternion.identity
                },
            };

            var target = CreateTestReallyBadCar(new Vector3(0, 0.35f, 20), Quaternion.identity);
            target.transform.localPosition = new Vector3(0, 0, 20);
            var cameraPosition = new Vector3(0, 0, 0);
            var cameraRotation = Quaternion.identity;
            return ExecuteTest(target, cameraPosition, cameraRotation, expected);
        }

        [UnityTest]
        public IEnumerator MultiInheritedMeshDifferentLabels_ProduceProperTranslationTest()
        {
            var wheelScale = new Vector3(0.35f, 1.0f, 0.35f);
            var wheelRot = Quaternion.Euler(0, 0, 90);

            var expected = new[]
            {
                new ExpectedResult
                {
                    instanceId = 1,
                    labelId = 2,
                    labelName = "car",
                    position = new Vector3(0, 1.05f, 20),
                    scale = new Vector3(1f, 0.7f, 2.4f),
                    rotation = Quaternion.identity
                },
                new ExpectedResult
                {
                    instanceId = 2,
                    labelId = 3,
                    labelName = "wheel",
                    position = new Vector3(1, 0.35f, 18.6f),
                    scale = wheelScale,
                    rotation = wheelRot
                },
                new ExpectedResult
                {
                    instanceId = 4,
                    labelId = 3,
                    labelName = "wheel",
                    position = new Vector3(-1, 0.35f, 18.6f),
                    scale = wheelScale,
                    rotation = wheelRot
                }
            };

            var target = CreateTestReallyBadCar(new Vector3(0, 0.35f, 20), Quaternion.identity, false);
            var cameraPosition = new Vector3(0, 0, 0);
            var cameraRotation = Quaternion.identity;
            return ExecuteTest(target, cameraPosition, cameraRotation, expected);
        }

        [UnityTest]
        public IEnumerator TestOcclusion_Seen()
        {
            var target = TestHelper.CreateLabeledCube(scale: 15f, z: 50f);
            return ExecuteSeenUnseenTest(target, Vector3.zero, quaternion.identity, 1);
        }

        [UnityTest]
        public IEnumerator TestOcclusion_Unseen()
        {
            var target = TestHelper.CreateLabeledCube(scale: 15f, z: -50f);
            return ExecuteSeenUnseenTest(target, Vector3.zero, quaternion.identity, 0);
        }
        
        
        struct ExpectedResult
        {
            public int labelId;
            public string labelName;
            public int instanceId;
            public Vector3 position;
            public Vector3 scale;
            public Quaternion rotation;
        }

        IEnumerator ExecuteSeenUnseenTest(GameObject target, Vector3 cameraPos, Quaternion cameraRotation, int expectedSeen)
        {
            var receivedResults = new List<(int, List<BoundingBox3DLabeler.BoxData>)>();
            var gameObject = SetupCamera(SetupLabelConfig(), (frame, data) =>
            {
                receivedResults.Add((frame, data));
            });

            gameObject.transform.position = cameraPos;
            gameObject.transform.rotation = cameraRotation;

            AddTestObjectForCleanup(gameObject);

            gameObject.SetActive(false);
            receivedResults.Clear();
            gameObject.SetActive(true);

            yield return null;
            yield return null;
            
            Assert.AreEqual(expectedSeen, receivedResults[0].Item2.Count);

            DestroyTestObject(gameObject);
            UnityEngine.Object.DestroyImmediate(target);
        }
        
        IEnumerator ExecuteTest(GameObject target, Vector3 cameraPos, Quaternion cameraRotation, IList<ExpectedResult> expectations)
        {
            var receivedResults = new List<(int, List<BoundingBox3DLabeler.BoxData>)>();
            var gameObject = SetupCamera(SetupLabelConfig(), (frame, data) =>
            {
                receivedResults.Add((frame, data));
            });

            gameObject.transform.position = cameraPos;
            gameObject.transform.rotation = cameraRotation;

            AddTestObjectForCleanup(gameObject);

            gameObject.SetActive(false);
            receivedResults.Clear();
            gameObject.SetActive(true);

            yield return null;
            yield return null;
            
            Assert.AreEqual(expectations.Count, receivedResults[0].Item2.Count);

            for (var i = 0; i < receivedResults[0].Item2.Count; i++)
            {
                var b = receivedResults[0].Item2[i];

                Assert.AreEqual(expectations[i].labelId, b.label_id);
                Assert.AreEqual(expectations[i].labelName, b.label_name);
                Assert.AreEqual(expectations[i].instanceId, b.instance_id);
                TestResults(b, expectations[i]);
            }

            DestroyTestObject(gameObject);
            UnityEngine.Object.DestroyImmediate(target);
        }

        static IdLabelConfig SetupLabelConfig()
        {
            var labelConfig = ScriptableObject.CreateInstance<IdLabelConfig>();
            labelConfig.Init(new List<IdLabelEntry>()
            {
                new IdLabelEntry
                {
                    id = 1,
                    label = "label"
                },
                new IdLabelEntry
                {

                    id = 2,
                    label= "car"
                },
                new IdLabelEntry
                {
                    id = 3,
                    label= "wheel"
                },
            });

            return labelConfig;
        }

        static GameObject SetupCamera(IdLabelConfig config, Action<int, List<BoundingBox3DLabeler.BoxData>> computeListener)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 60;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000;

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            var bboxLabeler = new BoundingBox3DLabeler(config);
            if (computeListener != null)
                bboxLabeler.BoundingBoxComputed += computeListener;

            perceptionCamera.AddLabeler(bboxLabeler);

            return cameraObject;
        }

        static void TestResults(BoundingBox3DLabeler.BoxData data, ExpectedResult e)
        {
            var scale = e.scale * 2;

            Assert.IsNotNull(data);
            Assert.AreEqual(e.position[0], data.translation[0], k_Delta);
            Assert.AreEqual(e.position[1], data.translation[1], k_Delta);
            Assert.AreEqual(e.position[2], data.translation[2], k_Delta);
            Assert.AreEqual(scale[0], data.size[0], k_Delta);
            Assert.AreEqual(scale[1], data.size[1], k_Delta);
            Assert.AreEqual(scale[2], data.size[2], k_Delta);
            Assert.AreEqual(e.rotation[0], data.rotation[0], k_Delta);
            Assert.AreEqual(e.rotation[1], data.rotation[1], k_Delta);
            Assert.AreEqual(e.rotation[2], data.rotation[2], k_Delta);
            Assert.AreEqual(e.rotation[3], data.rotation[3], k_Delta);
            Assert.AreEqual(Vector3.zero, data.velocity);
            Assert.AreEqual(Vector3.zero, data.acceleration);
        }

        static GameObject CreateTestReallyBadCar(Vector3 position, Quaternion rotation, bool underOneLabel = true)
        {
            var badCar = new GameObject("BadCar");
            badCar.transform.position = position;
            badCar.transform.rotation = rotation;
            if (underOneLabel)
            {
                var labeling = badCar.AddComponent<Labeling>();
                labeling.labels.Add("car");
            }

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "body";
            body.transform.parent = badCar.transform;
            body.transform.localPosition = new Vector3(0, 0.7f, 0);
            body.transform.localScale = new Vector3(2f, 1.4f, 4.8f);
            if (!underOneLabel)
            {
                var labeling = body.AddComponent<Labeling>();
                labeling.labels.Add("car");
            }

            var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "wheel1";
            wheel.transform.parent = badCar.transform;
            wheel.transform.localPosition = new Vector3(1f, 0, -1.4f);
            wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            wheel.transform.localScale = new Vector3(0.7f, 1, 0.7f);
            if (!underOneLabel)
            {
                var labeling = wheel.AddComponent<Labeling>();
                labeling.labels.Add("wheel");
            }

            wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "wheel2";
            wheel.transform.parent = badCar.transform;
            wheel.transform.localPosition = new Vector3(1f, 0, 1.45f);
            wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            wheel.transform.localScale = new Vector3(0.7f, 1, 0.7f);
            if (!underOneLabel)
            {
                var labeling = wheel.AddComponent<Labeling>();
                labeling.labels.Add("wheel");
            }

            wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "wheel3";
            wheel.transform.parent = badCar.transform;
            wheel.transform.localPosition = new Vector3(-1f, 0, -1.4f);
            wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            wheel.transform.localScale = new Vector3(0.7f, 1, 0.7f);
            if (!underOneLabel)
            {
                var labeling = wheel.AddComponent<Labeling>();
                labeling.labels.Add("wheel");
            }

            wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = "wheel4";
            wheel.transform.parent = badCar.transform;
            wheel.transform.localPosition = new Vector3(-1f, 0, 1.45f);
            wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
            wheel.transform.localScale = new Vector3(0.7f, 1, 0.7f);
            if (!underOneLabel)
            {
                var labeling = wheel.AddComponent<Labeling>();
                labeling.labels.Add("wheel");
            }

            return badCar;
        }

        static GameObject CreateMultiMeshGameObject()
        {
            var go = new GameObject();
            var labeling = go.AddComponent<Labeling>();
            labeling.labels.Add("label");

            var left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.transform.parent = go.transform;
            left.transform.localPosition = new Vector3(-4, 0, 0);
            left.transform.localScale = new Vector3(5, 5, 5);

            var right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.transform.parent = go.transform;
            right.transform.localPosition = new Vector3(4, 0, 0);
            right.transform.localScale = new Vector3(5, 5, 5);

            var center = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            center.transform.parent = go.transform;
            center.transform.localPosition = Vector3.zero;
            center.transform.localRotation = Quaternion.Euler(0, 0, 90);
            center.transform.localScale = new Vector3(1, 3, 1);

            return go;
        }

        class CubeSpinner : MonoBehaviour
        {
            public Quaternion rotationPerFrame = Quaternion.Euler(0, 15, 0);

            void Update()
            {
                transform.localRotation *= rotationPerFrame;
            }
        }

        class CubeMover : MonoBehaviour
        {
            public Vector3 distancePerFrame = new Vector3(5, 0 , 0);

            void Update()
            {
                transform.localPosition += distancePerFrame;
            }
        }

        static GameObject CreateDynamicBox(bool spinning = false, bool moving = false, Vector3? translation = null, Quaternion? rotation = null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube";
            var labeling = cube.AddComponent<Labeling>();
            labeling.labels.Add("label");
            cube.transform.position = new Vector3(0f, 0f, 10f);
            cube.transform.localScale = new Vector3(30f, 30f, 30f);
            if (spinning)
            {
                var spin = cube.AddComponent<CubeSpinner>();
                spin.rotationPerFrame = rotation ?? spin.rotationPerFrame;
            }
            if (moving)
            {
                var move = cube.AddComponent<CubeMover>();
                move.distancePerFrame = translation ?? move.distancePerFrame;
            }
            return cube;
        }
    }
}
