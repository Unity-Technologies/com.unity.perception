#if HDRP_PRESENT
using System.Collections;
using System.Linq;
using NUnit.Framework;
using RandomizerTests.Internal;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace RandomizerTests
{
    [TestFixture]
    public class LightRandomizerTests
    {
        FixedLengthScenario m_Scenario;
        LightRandomizer m_Randomizer;
        LightRandomizerTag m_Tag;

        [SetUp]
        public void Setup()
        {
            TestUtils.SetupRandomizerTestScene(ref m_Scenario, ref m_Randomizer, ref m_Tag);
        }

        [TearDown]
        public void Teardown()
        {
            TestUtils.ResetScene();
        }

        [UnityTest]
        public IEnumerator NullSetting_DoesNotRandomize()
        {
            var light = m_Tag.gameObject.GetComponent<Light>();
            light.useColorTemperature = true;
            var lightData = m_Tag.gameObject.GetComponent<HDAdditionalLightData>();

            // Randomly choose values for testing
            var chosenIntensity = Random.Range(1000f, 10000f);
            var chosenTemperature = Random.Range(1500f, 6500f);
            var chosenColor = Random.ColorHSV();

            // Setup a tag
            m_Tag.specifyIntensityAsList = true;
            m_Tag.intensityList = new CategoricalParameter<float>();
            m_Tag.specifyTemperatureAsList = true;
            m_Tag.temperatureList = new CategoricalParameter<float>();
            m_Tag.specifyColorAsList = true;
            m_Tag.colorList = new CategoricalParameter<Color>();

            lightData.intensity = chosenIntensity;
            light.useColorTemperature = true;
            light.colorTemperature = chosenTemperature;
            light.color = chosenColor;

            yield return null;
            yield return null;

            Assert.AreEqual(chosenIntensity, lightData.intensity);
            Assert.AreEqual(chosenTemperature, light.colorTemperature);
            Assert.AreEqual(chosenColor, light.color);
        }

        [UnityTest]
        public IEnumerator LightStateZero_DoesNotRandomize()
        {
            var light = m_Tag.gameObject.GetComponent<Light>();
            light.useColorTemperature = true;
            var lightData = m_Tag.gameObject.GetComponent<HDAdditionalLightData>();

            // Randomly choose values for testing
            var chosenIntensity = Random.Range(1000f, 10000f);
            var chosenTemperature = Random.Range(1500f, 6500f);
            var chosenColor = Random.ColorHSV();

            lightData.intensity = chosenIntensity;
            light.colorTemperature = chosenTemperature;
            lightData.color = chosenColor;

            // Setup a tag
            m_Tag.specifyIntensityAsList = false;
            m_Tag.intensity = new FloatParameter() { value = new UniformSampler(1000f, 9000f) };
            m_Tag.specifyTemperatureAsList = false;
            m_Tag.temperature = new FloatParameter() { value = new UniformSampler(4000f, 6000f) };
            m_Tag.specifyColorAsList = false;
            m_Tag.color = new ColorRgbParameter();

            // Disable light
            m_Tag.state = new BooleanParameter() { value = new ConstantSampler(0), threshold = 1f };

            yield return null;
            yield return null;

            // Do they remain the same (i.e. are not randomized)
            light.enabled = true;
            Assert.AreEqual(chosenIntensity, lightData.intensity);
            Assert.AreEqual(chosenTemperature, light.colorTemperature);
            Assert.AreEqual(chosenColor, light.color);
        }

        [UnityTest]
        public IEnumerator AllOptions_AsList_RandomizeProperly()
        {
            var light = m_Tag.gameObject.GetComponent<Light>();
            light.useColorTemperature = true;
            var lightData = m_Tag.gameObject.GetComponent<HDAdditionalLightData>();

            // Config
            var intensities = new[] { 4001f, 5001f, 6001f };
            var temperatures = new[] { 4002f, 5002f, 6002f };
            var colors = new[] { Color.red, Color.black, Color.cyan };

            // Setup tag
            m_Tag.specifyIntensityAsList = true;
            m_Tag.intensityList = new CategoricalParameter<float>();
            m_Tag.intensityList.SetOptions(TestUtils.OptionsFromArray(intensities));

            m_Tag.specifyTemperatureAsList = true;
            m_Tag.temperatureList = new CategoricalParameter<float>();
            m_Tag.temperatureList.SetOptions(TestUtils.OptionsFromArray(temperatures));

            m_Tag.specifyColorAsList = true;
            m_Tag.colorList = new CategoricalParameter<Color>();
            m_Tag.colorList.SetOptions(TestUtils.OptionsFromArray(colors));

            // Skip a frame
            yield return null;
            yield return null;

            Assert.IsTrue(intensities.Contains(lightData.intensity));
            Assert.IsTrue(temperatures.Contains(light.colorTemperature));
            Assert.IsTrue(colors.Contains(light.color));
        }

        [UnityTest]
        public IEnumerator AllOptions_AsNumericParameters_RandomizeProperly()
        {
            var light = m_Tag.gameObject.GetComponent<Light>();
            light.useColorTemperature = true;
            var lightData = m_Tag.gameObject.GetComponent<HDAdditionalLightData>();

            // Make sure values do not start off valid
            light.colorTemperature = 1000f;
            lightData.intensity = 9000f;
            light.color = Color.green;

            // Setup tag
            m_Tag.specifyIntensityAsList = false;
            m_Tag.intensity = new FloatParameter()
            {
                value = new UniformSampler(3001f, 4001f)
            };

            m_Tag.specifyTemperatureAsList = false;
            m_Tag.temperature = new FloatParameter()
            {
                value = new UniformSampler(3002f, 4002f)
            };

            m_Tag.specifyColorAsList = false;
            m_Tag.color = new ColorRgbParameter()
            {
                blue = new ConstantSampler(0.5f),
                red = new ConstantSampler(0.5f),
                green = new ConstantSampler(0.5f),
                alpha = new ConstantSampler(1f)
            };

            // Skip a frame
            yield return null;
            yield return null;

            Assert.IsTrue(lightData.intensity >= 3001f && lightData.intensity <= 4001f);
            Assert.IsTrue(light.colorTemperature >= 3002f && light.colorTemperature <= 4002f);
            Assert.AreEqual("RGBA(0.500, 0.500, 0.500, 1.000)", light.color.ToString());
        }
    }
}
#endif
