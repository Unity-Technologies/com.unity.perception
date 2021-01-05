using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;
using Object = UnityEngine.Object;

namespace RandomizationTests.ParameterTests
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
            m_TestObject.AddComponent<FixedLengthScenario>();
            m_Tests = new BaseStructParameterTest[]
            {
                new NumericParameterTest<bool>(new BooleanParameter()),
                new NumericParameterTest<int>(new IntegerParameter()),
                new NumericParameterTest<float>(new FloatParameter()),
                new NumericParameterTest<Vector2>(new Vector2Parameter()),
                new NumericParameterTest<Vector3>(new Vector3Parameter()),
                new NumericParameterTest<Vector4>(new Vector4Parameter()),
                new NumericParameterTest<Color>(new ColorHsvaParameter()),
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        [Test]
        public void CorrectNumberOfNativeSamplesAreGenerated()
        {
            foreach (var test in m_Tests)
                test.GeneratesNativeSamples();
        }
    }

    public abstract class BaseStructParameterTest
    {
        public abstract void GeneratesNativeSamples();
    }

    public class NumericParameterTest<T> : BaseStructParameterTest where T : struct
    {
        NumericParameter<T> m_Parameter;

        public NumericParameterTest(NumericParameter<T> parameter)
        {
            m_Parameter = parameter;
        }

        public override void GeneratesNativeSamples()
        {
            var nativeSamples = m_Parameter.Samples(TestValues.TestSampleCount, out var handle);
            handle.Complete();
            Assert.AreEqual(nativeSamples.Length, TestValues.TestSampleCount);
            nativeSamples.Dispose();
        }
    }
}
