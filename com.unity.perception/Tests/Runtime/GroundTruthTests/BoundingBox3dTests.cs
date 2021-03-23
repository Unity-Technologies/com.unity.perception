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
                    scale = new Vector3(10, 10, 10),
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
                    scale = new Vector3(10, 10, 10),
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
                    scale = new Vector3(13f, 5f, 5f),
                    rotation = Quaternion.identity
                }
            };
            var target = CreateMultiMeshGameObject();
            target.transform.position = Vector3.zero;
            var cameraPosition = new Vector3(0, 0, -10);
            var cameraRotation = Quaternion.identity;
            return ExecuteTest(target, cameraPosition, cameraRotation, expected);
        }

        public class ParentedTestData
        {
            public string name;

            public Vector3 expectedScale = Vector3.one;
            public Vector3 expectedPosition = new Vector3(0, 0, 10);
            public Quaternion expectedRotation = Quaternion.identity;

            public Vector3 childScale = Vector3.one;
            public Vector3 childPosition = Vector3.zero;
            public Quaternion childRotation = Quaternion.identity;

            public Vector3 grandchildScale = Vector3.one;
            public Vector3 grandchildPosition = Vector3.zero;
            public Quaternion grandchildRotation = Quaternion.identity;

            public Vector3 parentScale = Vector3.one;
            public Vector3 parentPosition = Vector3.zero;
            public Quaternion parentRotation = Quaternion.identity;

            public Vector3 grandparentScale = Vector3.one;
            public Vector3 grandparentPosition = Vector3.zero;
            public Quaternion grandparentRotation = Quaternion.identity;

            public Vector3 cameraParentScale = Vector3.one;
            public Vector3 cameraParentPosition = Vector3.zero;
            public Quaternion cameraParentRotation = Quaternion.identity;

            public Vector3 cameraScale = Vector3.one;
            public Vector3 cameraPosition = new Vector3(0, 0, -10);
            public Quaternion cameraRotation = Quaternion.identity;

            public override string ToString()
            {
                return name;
            }
        }

        static IEnumerable<ParentedTestData> ParentedObject_ProduceProperResults_Values()
        {
            yield return new ParentedTestData()
            {
                name = "ParentScale",
                expectedScale = new Vector3(1/5f, 1/5f, 1/5f),
                parentScale = new Vector3(1/5f, 1/5f, 1/5f),
            };
            yield return new ParentedTestData()
            {
                name = "GrandparentScale",
                expectedScale = new Vector3(1/5f, 1/5f, 1/5f),
                grandparentScale = new Vector3(1/5f, 1/5f, 1/5f),
            };
            yield return new ParentedTestData()
            {
                name = "ChildScale",
                expectedScale = new Vector3(1/5f, 1/5f, 1/5f),
                childScale = new Vector3(1/5f, 1/5f, 1/5f),
            };
            yield return new ParentedTestData()
            {
                name = "ChildAndParentScale",
                expectedScale = new Vector3(1f, 1f, 1f),
                childScale = new Vector3(5, 5, 5),
                parentScale = new Vector3(1/5f, 1/5f, 1/5f),
            };
            yield return new ParentedTestData()
            {
                name = "GrandchildScale",
                expectedScale = new Vector3(2, 2, 2),
                childScale = new Vector3(2, 2, 2),
                grandchildScale = new Vector3(5, 5, 5),
                parentScale = new Vector3(1/5f, 1/5f, 1/5f),
            };
            yield return new ParentedTestData()
            {
                name = "ParentRotation",
                expectedRotation = Quaternion.Euler(1f, 2f, 3f),
                parentRotation = Quaternion.Euler(1f, 2f, 3f),
            };
            yield return new ParentedTestData()
            {
                name = "ChildRotation",
                expectedRotation = Quaternion.Euler(1f, 2f, 3f),
                childRotation = Quaternion.Euler(1f, 2f, 3f),
            };
            yield return new ParentedTestData()
            {
                name = "ParentAndChildRotation",
                expectedRotation = Quaternion.identity,
                childRotation = Quaternion.Euler(20f, 0, 0),
                parentRotation = Quaternion.Euler(-20f, 0, 0),
            };
            var diagonalSize = Mathf.Sqrt(2 * 2 + 2 * 2); //A^2 + B^2 = C^2
            yield return new ParentedTestData()
            {
                name = "GrandchildRotation",
                expectedRotation = Quaternion.identity,
                expectedScale = new Vector3(diagonalSize / 2f, 1, diagonalSize / 2f),
                grandchildRotation = Quaternion.Euler(0, 45, 0),
            };
            yield return new ParentedTestData()
            {
                name = "GrandparentRotation",
                expectedRotation = Quaternion.Euler(-20f, 0, 0),
                grandparentRotation = Quaternion.Euler(-20f, 0, 0),
            };
            yield return new ParentedTestData()
            {
                name = "GrandchildTRS",
                expectedRotation = Quaternion.identity,
                expectedPosition = new Vector3(-5, 0, 10),
                expectedScale = new Vector3(.5f * diagonalSize / 2f, .5f, .5f * diagonalSize / 2f),
                grandchildRotation = Quaternion.Euler(0, -45, 0),
                grandchildPosition = new Vector3(-5, 0, 0),
                grandchildScale = new Vector3(.5f, .5f, .5f),
            };
            yield return new ParentedTestData()
            {
                name = "CamParentPositionAndScale",
                expectedRotation = Quaternion.identity,
                expectedPosition = new Vector3(2, 0, 6.5f),
                expectedScale = new Vector3(1, 1, 1),
                childPosition = new Vector3(0, 0, 4),
                cameraParentPosition = new Vector3(-2, 0, 0),
                cameraParentScale = new Vector3(1/2f, 1/3f, 1/4f),
            };
            //point at the left side of the box
            yield return new ParentedTestData()
            {
                name = "CamParentRotate",
                expectedRotation = Quaternion.Euler(0, -90, 0),
                expectedPosition = new Vector3(0, 0, 10),
                cameraParentRotation = Quaternion.Euler(0, 90, 0),
            };
            //point at the left side of the box
            yield return new ParentedTestData()
            {
                name = "CamParentScale",
                expectedPosition = new Vector3(0, 0, 2.5f),
                //Scale on the camera's hierarchy only affects the position of the camera. It does not affect the camera frustum
                cameraParentScale = new Vector3(1/2f, 1/3f, 1/4f),
            };
            yield return new ParentedTestData()
            {
                name = "CamRotationParentScale",
                expectedRotation = Quaternion.Euler(0, -90, 0),
                expectedPosition = new Vector3(0, 0, 5),
                cameraParentPosition = new Vector3(-5, 0, 0),
                cameraParentScale = new Vector3(.5f, 1, 1),
                cameraPosition = Vector3.zero,
                cameraRotation = Quaternion.Euler(0, 90, 0),
            };
        }
        [UnityTest]
        public IEnumerator ParentedObject_ProduceProperResults([ValueSource(nameof(ParentedObject_ProduceProperResults_Values))] ParentedTestData parentedTestData)
        {
            var expected = new[]
            {
                new ExpectedResult
                {
                    instanceId = 1,
                    labelId = 1,
                    labelName = "label",
                    position = parentedTestData.expectedPosition,
                    scale = parentedTestData.expectedScale,
                    rotation = parentedTestData.expectedRotation
                }
            };

            var goGrandparent = new GameObject();
            goGrandparent.transform.localPosition = parentedTestData.grandparentPosition;
            goGrandparent.transform.localScale = parentedTestData.grandparentScale;
            goGrandparent.transform.localRotation = parentedTestData.grandparentRotation;

            var goParent = new GameObject();
            goParent.transform.SetParent(goGrandparent.transform, false);
            goParent.transform.localPosition = parentedTestData.parentPosition;
            goParent.transform.localScale = parentedTestData.parentScale;
            goParent.transform.localRotation = parentedTestData.parentRotation;

            var goChild = new GameObject();
            goChild.transform.SetParent(goParent.transform, false);

            goChild.transform.localPosition = parentedTestData.childPosition;
            goChild.transform.localScale = parentedTestData.childScale;
            goChild.transform.localRotation = parentedTestData.childRotation;

            var labeling = goChild.AddComponent<Labeling>();
            labeling.labels.Add("label");

            var goGrandchild = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goGrandchild.transform.SetParent(goChild.transform, false);

            goGrandchild.transform.localPosition = parentedTestData.grandchildPosition;
            goGrandchild.transform.localScale = parentedTestData.grandchildScale;
            goGrandchild.transform.localRotation = parentedTestData.grandchildRotation;

            var goCameraParent = new GameObject();
            goCameraParent.transform.localPosition = parentedTestData.cameraParentPosition;
            goCameraParent.transform.localScale = parentedTestData.cameraParentScale;
            goCameraParent.transform.localRotation = parentedTestData.cameraParentRotation;

            var receivedResults = new List<(int, List<BoundingBox3DLabeler.BoxData>)>();
            var cameraObject = SetupCamera(SetupLabelConfig(), (frame, data) =>
            {
                receivedResults.Add((frame, data));
            });

            cameraObject.transform.SetParent(goCameraParent.transform, false);
            cameraObject.transform.localPosition = parentedTestData.cameraPosition;
            cameraObject.transform.localScale = parentedTestData.cameraScale;
            cameraObject.transform.localRotation = parentedTestData.cameraRotation;
            cameraObject.SetActive(true);

            return ExecuteTestOnCamera(goGrandparent, expected, goCameraParent, receivedResults);
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
                    scale = new Vector3(4f, 1.75f, 4.8f),
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
            var wheelScale = new Vector3(0.7f, 2.0f, 0.7f);
            var wheelRot = Quaternion.Euler(0, 0, 90);

            var expected = new[]
            {
                new ExpectedResult
                {
                    instanceId = 1,
                    labelId = 2,
                    labelName = "car",
                    position = new Vector3(0, 1.05f, 20),
                    scale = new Vector3(2f, 1.4f, 4.8f),
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

            return ExecuteTestOnCamera(target, expectations, gameObject, receivedResults);
        }

        private IEnumerator ExecuteTestOnCamera(GameObject target, IList<ExpectedResult> expectations, GameObject cameraObject,
            List<(int, List<BoundingBox3DLabeler.BoxData>)> receivedResults)
        {
            AddTestObjectForCleanup(cameraObject);
            AddTestObjectForCleanup(target);

            cameraObject.SetActive(false);
            receivedResults.Clear();
            cameraObject.SetActive(true);

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

            DestroyTestObject(cameraObject);
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
            Assert.IsNotNull(data);
            Assert.AreEqual(e.position[0], data.translation[0], k_Delta);
            Assert.AreEqual(e.position[1], data.translation[1], k_Delta);
            Assert.AreEqual(e.position[2], data.translation[2], k_Delta);
            Assert.AreEqual(e.scale[0], data.size[0], k_Delta);
            Assert.AreEqual(e.scale[1], data.size[1], k_Delta);
            Assert.AreEqual(e.scale[2], data.size[2], k_Delta);
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
