using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.MetadataReporter.Tags;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GroundTruthTests
{
    [TestFixture]
    public class MetadataTests : GroundTruthTestBase
    {
        [Test]
        public void TestSetValues()
        {
            var metadata = new SimulationMetadata();

            const int testInt = 42;
            const string testString = "so long";
            const float testFloat = 3.14159f;
            const bool testBool = true;
            const uint testUInt = UInt32.MaxValue;

            var testInts = new[] { 8, 6, 7, 5, 3, 0, 9 };
            var testStrings = new[] { "and", "thanks", "for", "all", "the", "fish" };
            var testFloats = new[] { 1.1f, 2.2f, 3.3f, 4.4f };
            var testBools = new[] { true, false, false, true, false };


            metadata.Add("testInt", testInt);
            metadata.Add("testUInt", testUInt);
            metadata.Add("testString", testString);
            metadata.Add("testFloat", testFloat);
            metadata.Add("testBool", testBool);
            metadata.Add("testStrings", testStrings);
            metadata.Add("testFloats", testFloats);
            metadata.Add("testBools", testBools);
            metadata.Add("testInts", testInts);

            var ia = Array.Empty<int>();
            Assert.DoesNotThrow(() => ia = metadata.GetIntArray("testInts"));
            Assert.AreEqual(testInts, ia);
            Assert.IsTrue(metadata.TryGetValue("testInts", out ia));
            Assert.AreEqual(testInts, ia);

            var ja = Array.Empty<string>();
            Assert.DoesNotThrow(() => ja = metadata.GetStringArray("testStrings"));
            Assert.AreEqual(testStrings, ja);
            Assert.IsTrue(metadata.TryGetValue("testStrings", out ja));
            Assert.AreEqual(testStrings, ja);

            var ka = Array.Empty<float>();
            Assert.DoesNotThrow(() => ka = metadata.GetFloatArray("testFloats"));
            Assert.AreEqual(testFloats, ka);
            Assert.IsTrue(metadata.TryGetValue("testFloats", out ka));
            Assert.AreEqual(testFloats, ka);

            var ba = Array.Empty<bool>();
            Assert.DoesNotThrow(() => ba = metadata.GetBoolArray("testBools"));
            Assert.AreEqual(testBools, ba);
            Assert.IsTrue(metadata.TryGetValue("testBools", out ba));
            Assert.AreEqual(testBools, ba);


            var i = 0;
            Assert.DoesNotThrow(() => i = metadata.GetInt("testInt"));
            Assert.AreEqual(testInt, i);
            Assert.IsTrue(metadata.TryGetValue("testInt", out i));
            Assert.AreEqual(testInt, i);

            var j = string.Empty;
            Assert.DoesNotThrow(() => j = metadata.GetString("testString"));
            Assert.AreEqual(testString, j);
            Assert.IsTrue(metadata.TryGetValue("testString", out j));
            Assert.AreEqual(testString, j);

            var k = 0f;
            Assert.DoesNotThrow(() => k = metadata.GetFloat("testFloat"));
            Assert.AreEqual(testFloat, k);
            Assert.IsTrue(metadata.TryGetValue("testFloat", out k));
            Assert.AreEqual(testFloat, k);

            var b = false;
            Assert.DoesNotThrow(() => b = metadata.GetBool("testBool"));
            Assert.AreEqual(testBool, b);
            Assert.IsTrue(metadata.TryGetValue("testBool", out b));
            Assert.AreEqual(testBool, b);

            var l = uint.MaxValue;
            Assert.DoesNotThrow(() => l = metadata.GetUInt("testUInt"));
            Assert.AreEqual(testUInt, l);
            Assert.IsTrue(metadata.TryGetValue("testUInt", out l));
            Assert.AreEqual(testUInt, l);
        }

        [Test]
        public void TestChangeValue()
        {
            var metadata = new SimulationMetadata();
            var key = "key";
            var val1 = 22;
            var val2 = 33;
            var val3 = 55;
            var retrieved = 0;

            metadata.Add(key, val1);
            Assert.DoesNotThrow(() => retrieved = metadata.GetInt(key));
            Assert.AreEqual(val1, retrieved);

            metadata.Add(key, val2);
            Assert.DoesNotThrow(() => retrieved = metadata.GetInt(key));
            Assert.AreEqual(val2, retrieved);

            metadata.Add(key, val3);
            Assert.DoesNotThrow(() => retrieved = metadata.GetInt(key));
            Assert.AreEqual(val3, retrieved);
        }

        [Test]
        public void TestChangeValueTypeForKey()
        {
            var metadata = new SimulationMetadata();
            var key = "key";
            var val1 = "catch";
            var val2 = 22;
            var ret1 = string.Empty;
            var ret2 = 0;

            metadata.Add(key, val1);
            Assert.DoesNotThrow(() => ret1 = metadata.GetString(key));
            Assert.AreEqual(val1, ret1);

            metadata.Add(key, val2);
            Assert.DoesNotThrow(() => ret2 = metadata.GetInt(key));
            Assert.AreEqual(val2, ret2);
        }

        [Test]
        public void TestGetThrow_NoKey()
        {
            var metadata = new SimulationMetadata();
            const string key = "key";
            const string key2 = "NotHere";
            const int val = 42;
            var outVal = 0;

            metadata.Add(key, val);

            Assert.Throws<ArgumentException>(() => outVal = metadata.GetInt(key2));
            Assert.DoesNotThrow(() => outVal = metadata.GetInt(key));
            Assert.AreEqual(val, outVal);
        }

        [Test]
        public void TestTryGetMiss_NoKey()
        {
            var metadata = new SimulationMetadata();
            const string key = "key";
            const string key2 = "NotHere";
            const int val = 42;
            var outVal = 0;

            metadata.Add(key, val);
            Assert.IsFalse(metadata.TryGetValue(key2, out outVal));
            Assert.IsTrue(metadata.TryGetValue(key, out outVal));
            Assert.AreEqual(val, outVal);
        }

        [Test]
        public void TestReadWriteJson()
        {
            var m = new Metadata();
            m.Add("int_value", 7);
            m.Add("uint_value", uint.MaxValue);
            m.Add("float_value", 4.2f);
            m.Add("string_value", "hello_world");
            m.Add("bool_value", false);
            m.Add("int[]_valle", new[] {0, 1, 2, 3});
            m.Add("float[]_value", new[] {1.1f, 2.2f, 3, 3f, 4.4f, 5.5f});
            m.Add("string[]_value", new[] {"this", "is", "an", "array"});
            m.Add("bool[]_value", new[] {false, true, true, false});

            var nested = new Metadata();
            nested.Add("int_value", 42);
            nested.Add("string_array", new[] {"life", "universe", "everything"});

            m.Add("nested", nested);

            var nested2 = new Metadata();
            nested2.Add("int_value", 43);
            nested2.Add("int_value2", 44);

            var nestedArray = new[] { nested, nested2 };
            m.Add("nested_array", nestedArray);

            var json = m.ToJson();

            var m2 = Metadata.FromJson(json);

            Assert.AreEqual(7, m2.GetInt("int_value"));
            Assert.AreEqual(uint.MaxValue, m2.GetUInt("uint_value"));
            Assert.AreEqual(4.2f, m2.GetFloat("float_value"));
            Assert.AreEqual("hello_world", m2.GetString("string_value"));
            Assert.AreEqual(false, m2.GetBool("bool_value"));
            Assert.AreEqual(new[] {0, 1, 2, 3}, m2.GetIntArray("int[]_valle"));
            Assert.AreEqual(new[] {1.1f, 2.2f, 3, 3f, 4.4f, 5.5f}, m2.GetFloatArray("float[]_value"));
            Assert.AreEqual(new[] {"this", "is", "an", "array"}, m2.GetStringArray("string[]_value"));
            Assert.AreEqual(new[] {false, true, true, false}, m2.GetBoolArray("bool[]_value"));
            var n = m2.GetSubMetadata("nested");
            Assert.AreEqual(42, n.GetInt("int_value"));
            Assert.AreEqual(new[] {"life", "universe", "everything"}, n.GetStringArray("string_array"));
            var n2 = m2.GetSubMetadataArray("nested_array");
            Assert.AreEqual(2, n2.Length);
            Assert.AreEqual(42, n2[0].GetInt("int_value"));
            Assert.AreEqual(new[] {"life", "universe", "everything"}, n2[0].GetStringArray("string_array"));
            Assert.AreEqual(43, n2[1].GetInt("int_value"));
            Assert.AreEqual(44, n2[1].GetInt("int_value2"));
        }

        [Test]
        public void TestTryGetMiss_WrongType()
        {
            var metadata = new SimulationMetadata();
            const string key = "key";
            const int val = 42;
            var outValStr = "This wasn't empty";
            var outValInt = 0;

            metadata.Add(key, val);
            Assert.IsFalse(metadata.TryGetValue(key, out outValStr));
            Assert.AreNotEqual("This wasn't empty", outValStr);
            Assert.IsTrue(metadata.TryGetValue(key, out outValInt));
            Assert.AreEqual(val, outValInt);
        }

        [UnityTest]
        public IEnumerator MetadataReporterTest()
        {
            SceneManager.LoadScene("Keypoint_Null_Check_On_Animator", LoadSceneMode.Additive);
            AddSceneForCleanup("Keypoint_Null_Check_On_Animator");
            yield return null;

            var cameraGo = new GameObject("camera");
            AddTestObjectForCleanup(cameraGo);
            var perceptionCamera = cameraGo.AddComponent<PerceptionCamera>();
            var labeler = new MetadataReporterLabeler();
            perceptionCamera.AddLabeler(labeler);

            const string tagName = "Player";

            var prefab = GameObject.Find("LabeledAndRandomized");
            prefab.tag = tagName;

            var lightGo = new GameObject("light");
            var light = lightGo.AddComponent<Light>();
            light.color = Color.white;
            AddTestObjectForCleanup(lightGo);

            var scenarioGo = new GameObject("scenario");
            AddTestObjectForCleanup(scenarioGo);
            var scenario = scenarioGo.AddComponent<FixedLengthScenario>();
            var randomizer = new KeyPointGroundTruthTests.DeleteAndRecreateForegroundRandomizer();

            var nameReportTag = prefab.AddComponent<LabelingNameMetadataTag>();
            var distanceToMainCameraReportTag = prefab.AddComponent<LabelingDistanceToMainCameraMetadataTag>();
            var tagReportTag = prefab.AddComponent<LabelingTagMetadataTag>();
            var lightReport = lightGo.AddComponent<LightMetadataTag>();

            prefab.SetActive(false);

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
                var builder = new JsonMessageBuilder();
                labeler.GenerateFrameData(builder);
                var json = builder.ToJson();

                Assert.AreEqual(json["instances"][0]["gameObjectName"]["name"].ToString(), $"{prefab.name}(Clone)");
                Assert.AreEqual((double)json["instances"][0]["SqrMagnitudeToMainCamera"]["sqrMagnitudeToMainCamera"], 0d, 0.1d);
                Assert.AreEqual((string)json["instances"][0]["gameObjectTag"]["unityTag"], tagName);


                Assert.AreEqual((int)json[$"SceneLight-{lightGo.name}"]["Color"][0], 255);
                Assert.AreEqual((double)json[$"SceneLight-{lightGo.name}"]["Intensity"], 1.0d, 0.1d);
            }

            //No error logs should be at this point
            Assert.AreEqual(0, 0);
        }
    }
}
