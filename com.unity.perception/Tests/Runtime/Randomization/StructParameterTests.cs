using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RandomizationTests
{
    [TestFixture]
    public class StructParameterTests
    {
        GameObject m_TestObject;
        BaseStructParameterTest[] m_Tests;

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
            m_Tests = new BaseStructParameterTest[]
            {
                new StructParameterTest<bool>(m_TestObject.AddComponent<BooleanParameter>()),
                new StructParameterTest<int>(m_TestObject.AddComponent<IntegerParameter>()),
                new StructParameterTest<float>(m_TestObject.AddComponent<FloatParameter>()),
                new StructParameterTest<Vector2>(m_TestObject.AddComponent<Vector2Parameter>()),
                new StructParameterTest<Vector3>(m_TestObject.AddComponent<Vector3Parameter>()),
                new StructParameterTest<Vector4>(m_TestObject.AddComponent<Vector4Parameter>()),
                new StructParameterTest<Color>(m_TestObject.AddComponent<ColorHsvaParameter>()),
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        [Test]
        public void EquivalentManagedAndNativeSamples()
        {
            foreach (var test in m_Tests)
                test.GeneratesNativeSamples();
        }
    }

    public abstract class BaseStructParameterTest
    {
        public abstract void GeneratesNativeSamples();
    }

    public class StructParameterTest<T> : BaseStructParameterTest where T : struct
    {
        StructParameter<T> m_Parameter;

        public StructParameterTest(StructParameter<T> parameter)
        {
            m_Parameter = parameter;
        }

        public override void GeneratesNativeSamples()
        {
            var nativeSamples = m_Parameter.Samples(
                TestValues.ScenarioIteration, TestValues.TestSampleCount, out var handle);
            handle.Complete();
            Assert.AreEqual(nativeSamples.Length, TestValues.TestSampleCount);
            nativeSamples.Dispose();
        }
    }
}
