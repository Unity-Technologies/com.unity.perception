using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.Consumers;

namespace GroundTruthTests
{
    [TestFixture]
    public class PerceptionJsonUtilityTests
    {
        StringBuilder m_StringBuilder;
        StringWriter m_StringWriter;
        JsonWriter m_Writer;
        JsonSerializer m_Serializer;

        [SetUp]
        public void Init()
        {
            m_StringBuilder = new StringBuilder();
            m_StringWriter = new StringWriter(m_StringBuilder);
            m_Writer = new JsonTextWriter(m_StringWriter);

            m_Serializer = new JsonSerializer
            {
                ContractResolver = new PerceptionResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
        }

        [TearDown]
        public void Cleanup() {}

        [Test]
        [TestCase(-2f, 0f, 3f, @"[-2.0,0.0,3.0]")]
        [TestCase(.01f, .02f, .03f, @"[0.01,0.02,0.03]")]
        [TestCase(float.NegativeInfinity, float.NaN, float.PositiveInfinity, @"[""-Infinity"",""NaN"",""Infinity""]")]
        public void Vector3ToJToken_ReturnsArrayFormat(float x, float y, float z, string jsonExpected)
        {
            PerceptionConverter.Instance.WriteJson(m_Writer, new Vector3(x, y, z), m_Serializer);
            Assert.AreEqual(TestHelper.NormalizeJson(jsonExpected), TestHelper.NormalizeJson(m_StringBuilder.ToString()));
        }

        [Test]
        [TestCase(-2f, 0f, 3f, 4f, @"[-2.0,0.0,3.0,4.0]")]
        [TestCase(.01f, .02f, .03f, 0.04f, @"[0.01,0.02,0.03,0.04]")]
        [TestCase(float.NegativeInfinity, float.NaN, float.PositiveInfinity, float.NaN, @"[""-Infinity"",""NaN"",""Infinity"",""NaN""]")]
        public void QuaternionToJToken_ReturnsArrayFormat(float x, float y, float z, float w, string jsonExpected)
        {
            PerceptionConverter.Instance.WriteJson(m_Writer, new Quaternion(x, y, z, w), m_Serializer);
            Assert.AreEqual(TestHelper.NormalizeJson(jsonExpected), TestHelper.NormalizeJson(m_StringBuilder.ToString()));
        }

        [Test]
        [TestCase(0.1f, 0.2f, 0.3f, 4f, 5f, 6f, 70f, 80f, 90f, @"[[0.1,4.0,70.0],[0.2,5.0,80.0],[0.3,6.0,90.0]]")]
        public void Float3x3ToJToken_ReturnsArrayFormat(float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22, string jsonExpected)
        {
            PerceptionConverter.Instance.WriteJson(m_Writer, new float3x3(m00, m01, m02, m10, m11, m12, m20, m21, m22), m_Serializer);
            Assert.AreEqual(TestHelper.NormalizeJson(jsonExpected), TestHelper.NormalizeJson(m_StringBuilder.ToString()));
        }

        [TestCase(1, "1")]
        [TestCase(1u, "1")]
        [TestCase(1.0, "1.0")]
        [TestCase(1.0f, "1.0")]
        [TestCase("string", "\"string\"")]
        public void Primitive_ReturnsValue(object o, string jsonExpected)
        {
            PerceptionConverter.Instance.WriteJson(m_Writer, o, m_Serializer);
            Assert.AreEqual(TestHelper.NormalizeJson(jsonExpected), TestHelper.NormalizeJson(m_StringBuilder.ToString()));
        }
    }
}
