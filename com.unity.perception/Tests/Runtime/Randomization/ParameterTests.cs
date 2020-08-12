using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.TestTools;

namespace RandomizationTests
{
    [TestFixture]
    public class ParameterTests
    {
        GameObject m_TestObject;

        Parameter[] m_Parameters;

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
            m_Parameters = new Parameter[]
            {
                m_TestObject.AddComponent<BooleanParameter>(),
                m_TestObject.AddComponent<IntegerParameter>(),
                m_TestObject.AddComponent<FloatParameter>(),
                m_TestObject.AddComponent<Vector2Parameter>(),
                m_TestObject.AddComponent<Vector3Parameter>(),
                m_TestObject.AddComponent<Vector4Parameter>(),
                m_TestObject.AddComponent<ColorHsvaParameter>(),
                m_TestObject.AddComponent<StringParameter>(),
                m_TestObject.AddComponent<MaterialParameter>()
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        [Test]
        public void NullSamplersTest()
        {
            foreach (var parameter in m_Parameters)
            {
                foreach (var sampler in parameter.Samplers)
                    Assert.NotNull(sampler);
            }
        }
    }
}
