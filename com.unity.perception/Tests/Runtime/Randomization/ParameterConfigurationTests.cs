using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Configuration;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.TestTools;

namespace RandomizationTests
{
    [TestFixture]
    public class ParameterConfigurationTests
    {
        GameObject m_TestObject;

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        [UnityTest]
        public IEnumerator CheckForParametersWithSameNameTest()
        {
            var config = m_TestObject.AddComponent<ParameterConfiguration>();
            var param1 = config.AddParameter<FloatParameter>();
            var param2 = config.AddParameter<BooleanParameter>();
            param1.parameterName = "SameName";
            param2.parameterName = "SameName";
            Assert.Throws<ParameterConfigurationException>(() => config.ValidateParameters());
            yield return null;
        }

        [UnityTest]
        public IEnumerator AddingNonParameterTypesTest()
        {
            var config = m_TestObject.AddComponent<ParameterConfiguration>();
            Assert.DoesNotThrow(() => config.AddParameter(typeof(FloatParameter)));
            Assert.Throws<ParameterConfigurationException>(() => config.AddParameter(typeof(Rigidbody)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CleansUpParametersOnDestroy()
        {
            var config = m_TestObject.AddComponent<ParameterConfiguration>();
            var parameter1 = config.AddParameter<FloatParameter>();
            var parameter2 = config.AddParameter<FloatParameter>();
            Object.DestroyImmediate(config);

            // Wait for next frame, at which point OnDestroy() has been called for both parameters
            yield return null;

            Assert.True(config == null);
            Assert.True(parameter1 == null);
            Assert.True(parameter2 == null);
        }
    }
}
