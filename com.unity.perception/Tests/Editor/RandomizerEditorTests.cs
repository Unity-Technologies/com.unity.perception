using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Perception.Randomization;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace EditorTests
{
    [TestFixture]
    public class RandomizerEditorTests
    {
        GameObject m_TestObject;
        FixedLengthScenario m_Scenario;

        static string dstAssetPath = "Assets/TestTemplate.cs";

        [SetUp]
        public void Setup()
        {
            m_TestObject = new GameObject();
            m_Scenario = m_TestObject.AddComponent<FixedLengthScenario>();
            if (File.Exists(dstAssetPath))
                File.Delete(dstAssetPath);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_TestObject);
            if (File.Exists(dstAssetPath))
                File.Delete(dstAssetPath);
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

        [UnityTest]
        public IEnumerator PlacementTemplateCompilesProperly()
        {
            File.Copy(RandomizerTemplateMenuItems.s_PlacementTemplatePath, dstAssetPath);
            AssetDatabase.Refresh();
            //this will throw if scripts fail to compile
            yield return new WaitForDomainReload();
            Assert.Pass();
        }

        [UnityTest]
        public IEnumerator RandomizerTagTemplateCompilesProperly()
        {
            File.Copy(RandomizerTemplateMenuItems.s_RandomizerTagTemplatePath, dstAssetPath);
            AssetDatabase.Refresh();
            //this will throw if scripts fail to compile
            yield return new WaitForDomainReload();
            Assert.Pass();
        }
    }

    [Serializable]
    [AddRandomizerMenu("")]
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
