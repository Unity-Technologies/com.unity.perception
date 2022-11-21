using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RandomizerTests.Internal;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace RandomizerTests
{
    [TestFixture]
    public class MaterialSwapperRandomizerTests
    {
        FixedLengthScenario m_Scenario;
        MaterialSwapperRandomizer m_Randomizer;
        MaterialSwapperRandomizerTag m_Tag;

        List<Material> m_TestMaterials;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_TestMaterials = new List<Material>
            {
                new Material(Shader.Find("HDRP/Lit"))
                {
                    name = "Test Material 1",
                    color = Color.red
                },
                new Material(Shader.Find("HDRP/Lit"))
                {
                    name = "Test Material 2",
                    color = Color.black
                },
                new Material(Shader.Find("HDRP/Lit"))
                {
                    name = "Test Material 3",
                    color = Color.green
                }
            };
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_TestMaterials.ForEach(Object.DestroyImmediate);
        }

        [SetUp]
        public void Setup()
        {
            TestUtils.SetupRandomizerTestScene<MaterialSwapperRandomizer, MaterialSwapperRandomizerTag>(
                ref m_Scenario, ref m_Randomizer, ref m_Tag
            );
        }

        [TearDown]
        public void Teardown()
        {
            TestUtils.ResetScene();
        }

        [UnityTest]
        public IEnumerator NthSubmesh_NoMaterials_DoesNotRandomize(
            [Values(0, 1, 2)]
            int targetMaterial
        )
        {
            m_Tag.targetedMaterialIndex = 0;
            var tagRenderer = m_Tag.Renderer;
            m_Tag.materials = new CategoricalParameter<Material>();
            var initialMaterial = tagRenderer.material;

            yield return null;
            Assert.AreEqual(initialMaterial, tagRenderer.material);
            yield return null;
            Assert.AreEqual(initialMaterial, tagRenderer.material);
            yield return null;
            Assert.AreEqual(initialMaterial, tagRenderer.material);
        }

        [UnityTest]
        public IEnumerator NthSubmesh_FewMaterials_RandomizesProperly(
            [Values(0, 1, 2)]
            int targetMaterial
        )
        {
            m_Tag.targetedMaterialIndex = 0;
            var tagRenderer = m_Tag.Renderer;

            m_Tag.materials = new CategoricalParameter<Material>();
            m_Tag.materials.SetOptions(m_TestMaterials);
            var initialMaterial = tagRenderer.material;

            yield return null;
            Assert.AreNotEqual(initialMaterial, tagRenderer.material);
            yield return null;
            Assert.AreNotEqual(initialMaterial, tagRenderer.material);
            yield return null;
            Assert.AreNotEqual(initialMaterial, tagRenderer.material);
        }
    }
}
