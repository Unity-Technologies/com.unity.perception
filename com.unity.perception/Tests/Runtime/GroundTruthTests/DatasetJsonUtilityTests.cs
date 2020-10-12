using System.IO;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace GroundTruthTests
{
    public class DatasetJsonUtilityTests
    {
        [Test]
        [TestCase(-2f, 0f, 3f, @"[
  -2.0,
  0.0,
  3.0
]")]
        [TestCase(.01f, .02f, .03f, @"[
  0.01,
  0.02,
  0.03
]")]
        [TestCase(float.NegativeInfinity, float.NaN, float.PositiveInfinity, @"[
  ""-Infinity"",
  ""NaN"",
  ""Infinity""
]")]
        public void Vector3ToJToken_ReturnsArrayFormat(float x, float y, float z, string jsonExpected)
        {
            var jsonActual = DatasetJsonUtility.ToJToken(new Vector3(x, y, z));
            Assert.AreEqual(TestHelper.NormalizeJson(jsonExpected), TestHelper.NormalizeJson(jsonActual.ToString()));
        }

        [Test]
        [TestCase(-2f, 0f, 3f, 4f, @"[
  -2.0,
  0.0,
  3.0,
  4.0
]")]
        [TestCase(.01f, .02f, .03f, 0.04f, @"[
  0.01,
  0.02,
  0.03,
  0.04
]")]
        [TestCase(float.NegativeInfinity, float.NaN, float.PositiveInfinity, float.NaN, @"[
  ""-Infinity"",
  ""NaN"",
  ""Infinity"",
  ""NaN""
]")]
        public void QuaternionToJToken_ReturnsArrayFormat(float x, float y, float z, float w, string jsonExpected)
        {
            var jsonActual = DatasetJsonUtility.ToJToken(new Quaternion(x, y, z, w)).ToString();
            Assert.AreEqual(TestHelper.NormalizeJson(jsonExpected), TestHelper.NormalizeJson(jsonActual));
        }

        [Test]
        [TestCase(0.1f, 0.2f, 0.3f, 4f, 5f, 6f, 70f, 80f, 90f, @"[
  [
    0.1,
    4.0,
    70.0
  ],
  [
    0.2,
    5.0,
    80.0
  ],
  [
    0.3,
    6.0,
    90.0
  ]
]")]
        public void Float3x3ToJToken_ReturnsArrayFormat(float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22, string jsonExpected)
        {
            var jsonActual = DatasetJsonUtility.ToJToken(new float3x3(m00, m01, m02, m10, m11, m12, m20, m21, m22)).ToString();
            Assert.AreEqual(TestHelper.NormalizeJson(jsonExpected), TestHelper.NormalizeJson(jsonActual));
        }

        [TestCase(1, "1")]
        [TestCase(1u, "1")]
        [TestCase(1.0, "1")]
        [TestCase(1.0f, "1")]
        [TestCase("string", "\"string\"")]
        public void Primitive_ReturnsValue(object o, string jsonExpected)
        {
            var jsonActual = DatasetJsonUtility.ToJToken(o).ToString();
            Assert.AreEqual(TestHelper.NormalizeJson(jsonExpected), TestHelper.NormalizeJson(jsonActual));
        }

        [Test]
        [TestCase(1,2,3)]
        [TestCase(0,0,0)]
        [TestCase(1000, 13000,700)]
        [TestCase(1.23f, 4.56f, 7.89f)]
        [TestCase(-100, 27.33f, -501.78f)]
        public void Vector3Converter_ConvertsProperly(float x, float y, float z)
        {
            var converter = new SimulationState.Vector3Converter();
            var testCase = new Vector3(x,y,z);
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);

            var serializer = JsonSerializer.CreateDefault();
            
            converter.WriteJson(new JsonTextWriter(sw), testCase, serializer);
            var str = sb.ToString();
            var sr = new StringReader(str);
            
            var converted = converter.ReadJson(new JsonTextReader(sr), typeof(Vector3), Vector3.zero, false, serializer);
            Assert.AreEqual(testCase, converted);
        }
        
        [Test]
        [TestCase(1,2,3, 4)]
        [TestCase(0,0,0, 1)]
        [TestCase(0, 0.6502878f, 0, -0.7596879f)]
        [TestCase(-0.3670137f, -0.1835069f, 0.1223379f, -0.9036922f)]
        public void QuaternionConverter_ConvertsProperly(float x, float y, float z, float w)
        {
            var converter = new SimulationState.QuaternionConverter();
            var testCase = new Quaternion(x, y, z, w);
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);

            var serializer = JsonSerializer.CreateDefault();
            
            converter.WriteJson(new JsonTextWriter(sw), testCase, serializer);
            var str = sb.ToString();
            var sr = new StringReader(str);
            
            var converted = converter.ReadJson(new JsonTextReader(sr), typeof(Quaternion), Quaternion.identity, false, serializer);
            Assert.AreEqual(testCase, converted);
        }
    }
}
