#if HDRP_PRESENT
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RandomizerTests.Internal;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.TestTools;
using FloatParameter = UnityEngine.Perception.Randomization.Parameters.FloatParameter;

namespace RandomizerTests
{
    [TestFixture]
    public class SkyboxRandomizerTests
    {
        FixedLengthScenario m_Scenario;
        SkyboxRandomizer m_Randomizer;
        SkyboxRandomizerTag m_Tag;
        HDRISky m_Sky;

        [SetUp]
        public void Setup()
        {
            TestUtils.SetupRandomizerTestScene<SkyboxRandomizer, SkyboxRandomizerTag>(
                ref m_Scenario, ref m_Randomizer, ref m_Tag,
                ((scenario, randomizer, tag) =>
                {
                    if (!tag.GetComponent<Volume>().profile.TryGet(out m_Sky))
                    {
                        m_Sky = tag.GetComponent<Volume>().profile.Add<HDRISky>();
                    }
                })
            );
        }

        [TearDown]
        public void Teardown()
        {
            TestUtils.ResetScene();
        }

        [UnityTest]
        public IEnumerator NullSetting_DoesNotRandomize()
        {
            m_Randomizer.skyboxes = new CategoricalParameter<Cubemap>();
            m_Randomizer.rotation = new FloatParameter()
            {
                value = new ConstantSampler(0)
            };

            var currentSkybox = m_Sky.hdriSky.value;

            yield return null;
            yield return null;

            Assert.AreEqual(currentSkybox, m_Sky.hdriSky.value);
            Assert.AreEqual(m_Sky.rotation.value, 0);
        }

        [UnityTest]
        public IEnumerator Setting_OnlyRotation_ProperlyRandomizes()
        {
            m_Randomizer.skyboxes = new CategoricalParameter<Cubemap>();
            m_Randomizer.rotation = new FloatParameter()
            {
                value = new UniformSampler(0, 360f)
            };

            var initialSkybox = m_Sky.hdriSky;
            var lastRotation = m_Sky.rotation.value;
            for (var i = 0; i < 5; i++)
            {
                yield return null;

                var newRotation = m_Sky.rotation.value;

                // the value for rotation changed
                Assert.AreNotEqual(lastRotation, newRotation);
                // the value for skybox did not change
                Assert.AreEqual(initialSkybox, m_Sky.hdriSky);

                lastRotation = newRotation;
            }
        }

        [UnityTest]
        public IEnumerator Setting_OnlySkybox_ProperlyRandomizes()
        {
            m_Randomizer.skyboxes = new CategoricalParameter<Cubemap>();
            var testCubemap = new List<Cubemap>();
            for (var i = 0; i < 3; i++)
            {
                testCubemap.Add(
                    new Cubemap(256, TextureFormat.RGBA32, 4){ name = $"Test Cubemap {i+1}" }
                );
            }

            var testCubemapNames = testCubemap.Select(x => x.name).ToList();

            m_Randomizer.skyboxes.SetOptions(testCubemap);
            m_Randomizer.rotation = new FloatParameter()
            {
                value = new ConstantSampler(0)
            };


            for (var i = 0; i < 5; i++)
            {
                yield return null;

                // the value for rotation did not change
                Assert.AreEqual(0f, m_Sky.rotation.value);
                // the value for skybox changed to one of the CubeMaps mentioned above
                Assert.IsTrue(testCubemapNames.Contains(m_Sky.hdriSky.value.name));
            }
        }
    }
}
#endif
