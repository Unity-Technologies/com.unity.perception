using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Schema;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
   [TestFixture]
    public class BoundingBox3dTests : GroundTruthTestBase
    {
        static string PrintBox(BoundingBox3DLabeler.BoxData box)
        {
            var sb = new StringBuilder();
            sb.Append("label id: " + box.label_id + " ");
            sb.Append("label_name: " + box.label_name + " ");
            sb.Append("instance_id: " + box.instance_id + " ");

            switch (box)
            {
                case BoundingBox3DLabeler.KittiData k1:
                    sb.Append("translation: (" + k1.translation[0] + ", " + k1.translation[1] + ", " + k1.translation[2] + ") ");
                    sb.Append("size: (" + k1.size[0] + ", " + k1.size[1] + ", " + k1.size[2] + ") ");
                    sb.Append("yaw: " + k1.yaw);
                    break;
                case BoundingBox3DLabeler.VerboseData v:
                {
                    sb.Append("translation: (" + v.translation[0] + ", " + v.translation[1] + ", " + v.translation[2] + ") ");
                    sb.Append("size: (" + v.size[0] + ", " + v.size[1] + ", " + v.size[2] + ") ");
                    sb.Append("rotation: " + +v.rotation[0] + ", " + v.rotation[1] + ", " + v.rotation[2] + ", " + v.rotation[3] + ") ");
                    if (v.velocity == null)
                        sb.Append("velocity: null ");
                    else
                        sb.Append("velocity: (" + v.velocity[0] + ", " + v.velocity[1] + ", " + v.velocity[2] + ") ");
                    if (v.acceleration == null)
                        sb.Append("acceleration: null");
                    else
                        sb.Append("acceleration: (" + v.acceleration[0] + ", " + v.acceleration[1] + ", " + v.acceleration[2] + ")");
                    break;
                }
            }

            return sb.ToString();
        }

        const float k_Delta = 0.0001f;

        [UnityTest]
        public IEnumerator CameraOffset_PrduceProperTranslationTest()
        {
            var expected = new ExpectedResult[]
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
        public IEnumerator CameraOffsetAndRotated_PrduceProperTranslationTest()
        {
            var expected = new ExpectedResult[]
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
        public IEnumerator SimpleMultiMesh_PrduceProperTranslationTest()
        {
            var expected = new ExpectedResult[]
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
        public IEnumerator MultiInheritedMesh_PrduceProperTranslationTest()
        {
            var expected = new ExpectedResult[]
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
        public IEnumerator MultiInheritedMeshDifferentLabels_PrduceProperTranslationTest()
        {
            var wheelScale = new Vector3(0.35f, 1.0f, 0.35f);
            var wheelRot = Quaternion.Euler(0, 0, 90);

            var expected = new ExpectedResult[]
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
                    instanceId = 3,
                    labelId = 3,
                    labelName = "wheel",
                    position = new Vector3(1, 0.35f, 21.45f),
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
                },
                new ExpectedResult
                {
                    instanceId = 5,
                    labelId = 3,
                    labelName = "wheel",
                    position = new Vector3(-1, 0.35f, 21.45f),
                    scale = wheelScale,
                    rotation = wheelRot
                }
            };

            var target = CreateTestReallyBadCar(new Vector3(0, 0.35f, 20), Quaternion.identity, false);
            //target.transform.localPosition = new Vector3(0, 0, 20);
            var cameraPosition = new Vector3(0, 0, 0);
            var cameraRotation = Quaternion.identity;
            return ExecuteTest(target, cameraPosition, cameraRotation, expected);
        }

        [UnityTest]
        public IEnumerator SpinningBoxTest()
        {
            var pos = new Vector3(0, 0, 10);
            var expectedPosition = new Vector3[]{pos, pos, pos, pos, pos};
            var scale = new Vector3(15f, 15f, 15f);
            var expectedScale = new Vector3[] { scale, scale, scale, scale, scale };

            var rot = new Quaternion[5];

            var spinner = Quaternion.Euler(0, 15, 0);
            for (var i = 0; i < 5; i++)
            {
                rot[0] = Quaternion.Euler(0, 30, 0);
                rot[1] = rot[0] * spinner;
                rot[2] = rot[1] * spinner;
                rot[3] = rot[2] * spinner;
                rot[4] = rot[3] * spinner;
            }

            var target = CreateDynamicBox(spinning: true);
            target.transform.localPosition = new Vector3(0, 0, 10);
            var cameraPosition = new Vector3(0, 0, 0);
            var cameraRotation = Quaternion.identity;
            return ExecuteTest(target, cameraPosition, cameraRotation, expectedPosition, expectedScale, rot, BoundingBox3DLabeler.OutputMode.Verbose);
        }

        [UnityTest]
        public IEnumerator MovingboxTest()
        {
            var pos = new Vector3(-40, 0, 10);
            var movement = new Vector3(10, 0, 0);
            var expectedPosition = new Vector3[]
            {
                pos + movement * 2,
                pos + movement * 3,
                pos + movement * 4,
                pos + movement * 5,
                pos + movement * 6
            };
            var scale = new Vector3(15, 15, 15);
            var expectedScale = new Vector3[] { scale, scale, scale, scale, scale };
            var rot = Quaternion.identity;
            var expectedRot = new Quaternion[] { rot, rot, rot, rot, rot };
            var target = CreateDynamicBox(moving: true);
            target.transform.localPosition = pos;
            var cameraPosition = new Vector3(0, 0, 0);
            var cameraRotation = Quaternion.identity;
            return ExecuteTest(target, cameraPosition, cameraRotation, expectedPosition, expectedScale, expectedRot, BoundingBox3DLabeler.OutputMode.Verbose);
        }

        public struct ExpectedResult
        {
            public int labelId;
            public string labelName;
            public int instanceId;
            public Vector3 position;
            public Vector3 scale;
            public Quaternion rotation;
        }

        private IEnumerator ExecuteTest(GameObject target, Vector3 cameraPos, Quaternion cameraRotation, ExpectedResult[] expectations)
        {
            var receivedResults = new List<(int, BoundingBox3DLabeler.BoxData)>();
            var cameraObject = SetupCamera(SetupLabelConfig(), (frame, data) =>
            {
                receivedResults.Add((frame, data));
            });

            cameraObject.Item1.transform.position = cameraPos;
            cameraObject.Item1.transform.rotation = cameraRotation;

            AddTestObjectForCleanup(cameraObject.Item1);

            var firstTime = true;

            foreach (var mode in (BoundingBox3DLabeler.OutputMode[])Enum.GetValues(typeof(BoundingBox3DLabeler.OutputMode)))
            {
                cameraObject.Item1.SetActive(false);
                receivedResults.Clear();
                cameraObject.Item2.mode = mode;
                cameraObject.Item1.SetActive(true);

                if (firstTime)
                {
                    firstTime = false;
                    yield return null;
                }

                yield return null;

                Assert.AreEqual(expectations.Length, receivedResults.Count);

                for (var i = 0; i < expectations.Length; i++)
                {
                    var b = receivedResults[i].Item2;

                    Debug.Log(PrintBox(b));

                    Assert.AreEqual(expectations[i].labelId, b.label_id);
                    Assert.AreEqual(expectations[i].labelName, b.label_name);
                    Assert.AreEqual(expectations[i].instanceId, b.instance_id);

                    switch (mode)
                    {
                        case BoundingBox3DLabeler.OutputMode.Verbose:
                            Assert.IsAssignableFrom<BoundingBox3DLabeler.VerboseData>(b);
                            var v = b as BoundingBox3DLabeler.VerboseData;
                            TestVerboseResults(v, expectations[i]);
                            break;
                        case BoundingBox3DLabeler.OutputMode.Kitti:
                            Assert.IsAssignableFrom<BoundingBox3DLabeler.KittiData>(b);
                            var k = b as BoundingBox3DLabeler.KittiData;
                            TestKittiResults(k, expectations[i]);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            DestroyTestObject(cameraObject.Item1);
            UnityEngine.Object.DestroyImmediate(target);
        }



        private IEnumerator ExecuteTest(GameObject target, Vector3 cameraPos, Quaternion cameraRotation, Vector3[] expectedPosition, Vector3[] expectedScale, Quaternion[] expectedRotation, BoundingBox3DLabeler.OutputMode mode)
        {
            var receivedResults = new List<(int, BoundingBox3DLabeler.BoxData)>();
            var cameraObject = SetupCamera(SetupLabelConfig(), (frame, data) =>
            {
                receivedResults.Add((frame, data));
            });

            cameraObject.Item1.transform.position = cameraPos;
            cameraObject.Item1.transform.rotation = cameraRotation;

            AddTestObjectForCleanup(cameraObject.Item1);

            cameraObject.Item1.SetActive(false);
            receivedResults.Clear();
            cameraObject.Item2.mode = mode;
            cameraObject.Item1.SetActive(true);

            yield return null;

            for (int i = 0; i < expectedPosition.Length; i++)
            {
                yield return null;

                Assert.AreEqual(i + 1, receivedResults.Count);
                var b = receivedResults[i].Item2;

                Debug.Log(PrintBox(b));

                Assert.AreEqual(1, b.label_id);
                Assert.AreEqual("label", b.label_name);
                Assert.AreEqual(1, b.instance_id);

                // TODO fix this test up to use the expected results as an input
                var expected = new ExpectedResult
                {
                    position = expectedPosition[i],
                    rotation = expectedRotation[i],
                    scale = expectedScale[i],
                };

                switch (mode)
                {
                    case BoundingBox3DLabeler.OutputMode.Verbose:
                        Assert.IsAssignableFrom<BoundingBox3DLabeler.VerboseData>(b);
                        var v = b as BoundingBox3DLabeler.VerboseData;
                        TestVerboseResults(v, expected);
                        break;
                    case BoundingBox3DLabeler.OutputMode.Kitti:
                        Assert.IsAssignableFrom<BoundingBox3DLabeler.KittiData>(b);
                        var k = b as BoundingBox3DLabeler.KittiData;
                        TestKittiResults(k, expected);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            DestroyTestObject(cameraObject.Item1);
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

        static (GameObject, BoundingBox3DLabeler) SetupCamera(IdLabelConfig config, Action<int, BoundingBox3DLabeler.BoxData> computeListener)
        {
            var cameraObject = new GameObject();
            cameraObject.SetActive(false);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1;

            var perceptionCamera = cameraObject.AddComponent<PerceptionCamera>();
            perceptionCamera.captureRgbImages = false;
            var bboxLabeler = new BoundingBox3DLabeler(config);
            if (computeListener != null)
                bboxLabeler.BoundingBoxComputed += computeListener;

            perceptionCamera.AddLabeler(bboxLabeler);

            return (cameraObject, bboxLabeler);
        }

        public void TestVerboseResults(BoundingBox3DLabeler.VerboseData data, ExpectedResult e)
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
            Assert.IsNull(data.velocity);
            Assert.IsNull(data.acceleration);
        }

        public void TestKittiResults(BoundingBox3DLabeler.KittiData data, ExpectedResult e)
        {
            var size = e.scale * 2;

            Assert.IsNotNull(data);
            Assert.AreEqual(e.position[0], data.translation[0], k_Delta);
            Assert.AreEqual(e.position[1], data.translation[1], k_Delta);
            Assert.AreEqual(e.position[2], data.translation[2], k_Delta);
            Assert.AreEqual(size[0], data.size[0], k_Delta);
            Assert.AreEqual(size[1], data.size[1], k_Delta);
            Assert.AreEqual(size[2], data.size[2], k_Delta);
            Assert.AreEqual(e.rotation.eulerAngles.y, data.yaw, k_Delta);
        }

        private static GameObject CreateTestReallyBadCar(Vector3 position, Quaternion rotation, bool underOneLabel = true)
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

        private GameObject CreateMultiMeshGameObject()
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
            public Vector3 distancePerFrame = new Vector3(10, 0 , 0);

            void Update()
            {
                transform.localPosition += distancePerFrame;
            }
        }

        private static GameObject CreateDynamicBox(bool spinning = false, bool moving = false, Vector3? translation = null, Quaternion? rotation = null)
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
