using System;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace GroundTruthTests
{
    [TestFixture]
    public class JsonMessageBuilderTests
    {
        void AddNestedToMessage(IMessageBuilder nestedBuilder)
        {
            nestedBuilder.AddBool("bool0", true);
            nestedBuilder.AddInt("int0", 42);
            nestedBuilder.AddStringArray("str0", new[] {"nested", "test"});
        }

        JToken CreateNestedMessageVerificationReult()
        {
            return new JObject
            {
                { "bool0", true },
                { "int0", 42 },
                { "str0", new JArray("nested", "test") }
            };
        }

        JToken CreateVerificationResults()
        {
            var results = new JObject
            {
                { "byte0", byte.MinValue },
                { "byte1", byte.MaxValue },
                { "char0", 't' },
                { "char1", 'e' },
                { "char2", 's' },
                { "char3", 't' },
                { "int0", int.MinValue },
                { "int1", int.MaxValue },
                { "int2", 42 },
                { "uint0", uint.MinValue },
                { "uint1", uint.MaxValue },
                { "uint2", 42u },
                { "long0", long.MinValue },
                { "long1", long.MaxValue },
                { "long2", 42L },
                { "float0", float.MinValue },
                { "float1", float.MaxValue },
                { "float2", 42.0f },
                { "double0", double.MinValue },
                { "double1", double.MaxValue },
                { "double2", 42.0d },
                { "string0", "Test" },
                { "string1", string.Empty },
                { "bool0", true },
                { "bool1", false },
                { "intarray", new JArray(int.MinValue, int.MaxValue) },
                { "uintarray", new JArray(uint.MinValue, uint.MaxValue) },
                { "longarray", new JArray(long.MinValue, long.MaxValue) },
                { "floatarray", new JArray(float.MinValue, float.MaxValue) },
                { "doublearray", new JArray(double.MinValue, double.MaxValue) },
                { "stringarray", new JArray("This", "is", "a", "test") },
                { "boolarray", new JArray(false, true) },
            };

            var n1 = CreateNestedMessageVerificationReult();
            n1["nestedmsg"] = CreateNestedMessageVerificationReult();
            results["nestedmsg"] = n1;

            results["nestedarray"] = new JArray(
                CreateNestedMessageVerificationReult(),
                CreateNestedMessageVerificationReult(),
                CreateNestedMessageVerificationReult()
            );

            return results;
        }

        [Test]
        public void TestCreatesCorrectJson()
        {
            var testBuilder = new JsonMessageBuilder();
            testBuilder.AddByte("byte0", byte.MinValue);

            testBuilder.AddByte("byte1", byte.MaxValue);

            testBuilder.AddChar("char0", 't');
            testBuilder.AddChar("char1", 'e');
            testBuilder.AddChar("char2", 's');
            testBuilder.AddChar("char3", 't');

            testBuilder.AddInt("int0", int.MinValue);
            testBuilder.AddInt("int1", int.MaxValue);
            testBuilder.AddInt("int2", 42);

            testBuilder.AddUInt("uint0", uint.MinValue);
            testBuilder.AddUInt("uint1", uint.MaxValue);
            testBuilder.AddUInt("uint2", 42u);

            testBuilder.AddLong("long0", long.MinValue);
            testBuilder.AddLong("long1", long.MaxValue);
            testBuilder.AddLong("long2", 42L);

            testBuilder.AddFloat("float0", float.MinValue);
            testBuilder.AddFloat("float1", float.MaxValue);
            testBuilder.AddFloat("float2", 42.0f);

            testBuilder.AddDouble("double0", double.MinValue);
            testBuilder.AddDouble("double1", double.MaxValue);
            testBuilder.AddDouble("double2", 42.0d);

            testBuilder.AddString("string0", "Test");
            testBuilder.AddString("string1", string.Empty);

            testBuilder.AddBool("bool0", true);
            testBuilder.AddBool("bool1", false);

            testBuilder.AddIntArray("intarray", new[] {int.MinValue, int.MaxValue});
            testBuilder.AddUIntArray("uintarray", new[] {uint.MinValue, uint.MaxValue});
            testBuilder.AddLongArray("longarray", new[] {long.MinValue, long.MaxValue});
            testBuilder.AddFloatArray("floatarray", new[] {float.MinValue, float.MaxValue});
            testBuilder.AddDoubleArray("doublearray", new[] {double.MinValue, double.MaxValue});
            testBuilder.AddStringArray("stringarray", new[] {"This", "is", "a", "test"});
            testBuilder.AddBoolArray("boolarray", new[] {false, true});

            var nmb = testBuilder.AddNestedMessage("nestedmsg");
            AddNestedToMessage(nmb);

            var nmb2 = nmb.AddNestedMessage("nestedmsg");
            AddNestedToMessage(nmb2);

            var na = testBuilder.AddNestedMessageToVector("nestedarray");
            AddNestedToMessage(na);
            na = testBuilder.AddNestedMessageToVector("nestedarray");
            AddNestedToMessage(na);
            na = testBuilder.AddNestedMessageToVector("nestedarray");
            AddNestedToMessage(na);

            var json = testBuilder.ToJson();

            Assert.IsTrue(JToken.DeepEquals(json, CreateVerificationResults()));
        }

        [Test]
        public void TestAddEncodeImageThrowsException()
        {
            var testBuilder = new JsonMessageBuilder();
            Assert.Throws<NotSupportedException>(() => testBuilder.AddEncodedImage("error", "png", new byte[] { 0, 0, 0 }));
        }

        [Test]
        public void TestAddTensorThrowsException()
        {
            var buffer = new byte[27];
            for (var i = 0; i < 27; i++)
            {
                buffer[i] = (byte)i;
            }

            var tensor = new Tensor(Tensor.ElementType.Byte, new[] { 3, 3, 3 }, buffer);

            var testBuilder = new JsonMessageBuilder();
            Assert.Throws<NotSupportedException>(() => testBuilder.AddTensor("error", tensor));
        }
    }
}
