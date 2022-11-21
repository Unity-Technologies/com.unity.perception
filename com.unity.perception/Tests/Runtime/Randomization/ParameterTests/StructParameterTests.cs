using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Scenarios;
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
        public void CorrectNumberOfSamplesAreGenerated()
        {
            foreach (var test in m_Tests)
                test.GeneratesSamples();
        }
    }

    public abstract class BaseStructParameterTest
    {
        public abstract void GeneratesSamples();
    }

    public class NumericParameterTest<T> : BaseStructParameterTest where T : struct
    {
        NumericParameter<T> m_Parameter;

        public NumericParameterTest(NumericParameter<T> parameter)
        {
            m_Parameter = parameter;
        }

        public override void GeneratesSamples()
        {
            var samples = new T[TestValues.TestSampleCount];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = m_Parameter.Sample();
            }

            Assert.AreEqual(samples.Length, TestValues.TestSampleCount);
        }
    }
}
