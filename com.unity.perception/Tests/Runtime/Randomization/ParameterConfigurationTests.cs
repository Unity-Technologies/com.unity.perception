﻿using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Perception.Randomization.Configuration;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
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

        [Test]
        public void CheckForParametersWithSameNameTest()
        {
            var config = m_TestObject.AddComponent<ParameterConfiguration>();
            var param1 = config.AddParameter<FloatParameter>();
            var param2 = config.AddParameter<BooleanParameter>();
            param1.name = "SameName";
            param2.name = "SameName";
            Assert.Throws<ParameterConfigurationException>(() => config.ValidateParameters());
        }

        [Test]
        public void AddingNonParameterTypesTest()
        {
            var config = m_TestObject.AddComponent<ParameterConfiguration>();
            Assert.DoesNotThrow(() => config.AddParameter(typeof(FloatParameter)));
            Assert.Throws<ParameterConfigurationException>(() => config.AddParameter(typeof(Rigidbody)));
        }
    }
}
