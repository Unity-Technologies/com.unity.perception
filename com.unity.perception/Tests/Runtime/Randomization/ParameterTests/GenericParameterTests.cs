using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;

namespace RandomizationTests.ParameterTests
{
    [TestFixture]
    public class GenericParameterTests
    {
        GameObject m_TestObject;

        Parameter[] m_Parameters;

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
            m_Parameters = new Parameter[]
            {
                new BooleanParameter(),
                new IntegerParameter(),
                new FloatParameter(),
                new Vector2Parameter(),
                new Vector3Parameter(),
                new Vector4Parameter(),
                new ColorHsvaParameter(),
                new CategoricalParameter<string>(),
                new CategoricalParameter<Material>()
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
                foreach (var sampler in parameter.samplers)
                    Assert.NotNull(sampler);
            }
        }
    }
}
