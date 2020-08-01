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

        [UnityTest]
        public IEnumerator EquivalentManagedAndNativeSamples()
        {
            foreach (var test in m_Tests)
                test.EquivalentManagedAndNativeSamplesTest();
            yield return null;
        }
    }

    public abstract class BaseStructParameterTest
    {
        public abstract void EquivalentManagedAndNativeSamplesTest();
    }

    public class StructParameterTest<T> : BaseStructParameterTest where T : struct
    {
        StructParameter<T> m_Parameter;

        public StructParameterTest(StructParameter<T> parameter)
        {
            m_Parameter = parameter;
        }

        public override void EquivalentManagedAndNativeSamplesTest()
        {

            var managedSamples = m_Parameter.Samples(TestValues.ScenarioIteration, TestValues.TestSampleCount);
            var nativeSamples = m_Parameter.Samples(TestValues.ScenarioIteration, TestValues.TestSampleCount, out var handle);
            handle.Complete();

            Assert.AreEqual(managedSamples.Length, nativeSamples.Length);

            // The occasional float inside of color native samples sometimes has
            // one byte different from its managed counterpart. The Color class
            // equals operator (==) does an approximation check instead of the
            // exact float comparison done in the Color class's Equals() method.
            if (m_Parameter is ColorHsvaParameter)
            {
                for (var i = 0; i < managedSamples.Length; i++)
                {
                    var sample1 = (Color)(object)managedSamples[i];
                    var sample2 = (Color)(object)nativeSamples[i];
                    Assert.AreEqual(true, sample1 == sample2);
                }
            }
            else
            {
                for (var i = 0; i < managedSamples.Length; i++)
                    Assert.AreEqual(managedSamples[i], nativeSamples[i]);
            }

            nativeSamples.Dispose();
        }
    }
}
