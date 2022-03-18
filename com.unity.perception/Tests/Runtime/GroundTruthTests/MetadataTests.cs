using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace GroundTruthTests
{
    [TestFixture]
    public class MetadataTests
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

            var testInts = new [] { 8, 6, 7, 5, 3, 0, 9 };
            var testStrings = new [] { "and", "thanks", "for", "all", "the", "fish" };
            var testFloats = new[] { 1.1f, 2.2f, 3.3f, 4.4f };
            var testBools = new [] { true, false, false, true, false };


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
    }
}
