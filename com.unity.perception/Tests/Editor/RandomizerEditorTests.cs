using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Scenarios;
using Object = UnityEngine.Object;

namespace EditorTests
{
    [TestFixture]
    public class RandomizerEditorTests
    {
        GameObject m_TestObject;
        FixedLengthScenario m_Scenario;

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
            m_Scenario = m_TestObject.AddComponent<FixedLengthScenario>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
        }

        [Test]
        public void RandomizerOnCreateMethodNotCalledInEditMode()
        {
            // TestRandomizer.OnCreate() should NOT be called here while in edit-mode
            // if ScenarioBase.CreateRandomizer<>() was coded correctly
            Assert.DoesNotThrow(() =>
            {
                m_Scenario.AddRandomizer(new ErrorsOnCreateTestRandomizer());
            });
        }
    }

    [Serializable]
    [AddRandomizerMenu("Test Randomizers/Errors OnCreate Test Randomizer")]
    class ErrorsOnCreateTestRandomizer : Randomizer
    {
        public GameObject testGameObject;

        protected override void OnAwake()
        {
            // This line should throw a NullReferenceException
            testGameObject.transform.position = Vector3.zero;
        }

        protected override void OnIterationStart()
        {
            testGameObject = new GameObject("Test");
        }
    }
}
