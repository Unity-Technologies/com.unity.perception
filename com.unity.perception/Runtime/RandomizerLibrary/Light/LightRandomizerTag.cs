#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Supports the ability to randomize the light state (on/off), intensity (in the unit specified in the Light
    /// component), temperature (if using color temperature), and color.
    /// </summary>
    #region RequireComponent
#if HDRP_PRESENT
    [RequireComponent(typeof(HDAdditionalLightData))]
#endif
    #endregion
    [AddComponentMenu("Perception/RandomizerTags/Light Randomizer Tag")]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public class LightRandomizerTag : RandomizerTag
    {
        Light m_Light;
        Light Light => m_Light = m_Light ? m_Light : GetComponent<Light>();

        #region LightData
#if HDRP_PRESENT
        /// <summary>
        /// HDRP-specific light data
        /// </summary>
        HDAdditionalLightData m_LightData;
        /// <inheritdoc cref="m_LightData"/>
        HDAdditionalLightData LightData => m_LightData = m_LightData ? m_LightData : GetComponent<HDAdditionalLightData>();
#endif
        #endregion

        #region State
        /// <summary>
        /// A value between 0 and 1 will be randomly chosen. If that value is above the threshold, it will return true.
        /// Else it will return false.
        /// </summary>
        /// <remarks>At 0, light will always be enabled; At 1, always disabled; At 0.5, enabled half the time.</remarks>
        [Tooltip("The probability for the light to be off. At a threshold (probability) of 0, the light will always be on; at 1, always off and at 0.4, on about 60% of the time.")]
        public BooleanParameter state = new BooleanParameter()
        {
            threshold = 0.0f
        };
        #endregion

        #region Intensity
        /// <summary>
        /// When set to true, <see cref="intensityList" /> will be sampled to set the intensity of the Light component.
        /// When set to false, <see cref="intensity" /> will be used instead.
        /// </summary>
        [Tooltip("When set to true, values for intensity will be sampled from a list of values. When false, a range of values will be used instead.")]
        public bool specifyIntensityAsList = false;

        /// <summary>
        /// A value will be sampled based on the chosen Sampling method under Value and set as the Light component's
        /// Intensity. The unit for intensity will be the same as the one set under Emission in the Light component.
        /// </summary>
        [Tooltip("A value will be sampled based on the chosen Sampling method under Value. The unit for intensity will be the same as the one set under Emission. ")]
        public FloatParameter intensity = new FloatParameter()
        {
            value = new UniformSampler(0.5f, 8000f)
        };

        /// <summary>
        /// Randomly chooses an intensity from the options provided and assigns it to the Light component in the unit
        /// specified under Emission. The probability of each color being selected can be modified by disabling the
        /// Uniform flag and providing probability values manually.
        /// </summary>
        /// <remarks>
        /// If no values are provided, the light intensity will remain unchanged.
        /// </remarks>
        [Tooltip("Randomly chooses an intensity from the options provided and assigns it to the Light component in the unit specified under Emission. The probability of each color being selected can be modified by disabling the `Uniform flag and providing probability values manually.")]
        public CategoricalParameter<float> intensityList = new CategoricalParameter<float>();
        #endregion

        #region Temperature
        /// <summary>
        /// When set to true, <see cref="temperatureList" /> will be sampled to set the temperature of the Light component.
        /// When set to false, <see cref="temperature" /> will be used instead.
        /// </summary>
        [Tooltip("When set to true, values for temperature will be sampled from a list of values. When false, a range of values will be used instead.")]
        public bool specifyTemperatureAsList = false;
        /// <summary>
        /// A value will be sampled based on the chosen Sampling method under Value and set as the Light component's
        /// Temperature. Only utilized when using the Filter and Temperate mode for Light Appearance (under Emission for HDRP lights only).
        /// </summary>
        [Tooltip("A value will be sampled based on the chosen Sampling method under Value and set as the Light component's Temperature. This is only utilized when using the Filter and Temperate mode for Light Appearance (under Emission for HDRP lights only).")]
        public FloatParameter temperature = new FloatParameter()
        {
            value = new UniformSampler(2400f, 7200f)
        };
        /// <summary>
        /// Randomly chooses a temperature from the options provided and assigns it to the Light component if the Light
        /// Appearance mode (under Emission) is set to Filter and Temperature (HDRP only). The probability of each color being
        /// selected can be modified by disabling the Uniform flag and providing probability values manually.
        /// </summary>
        /// <remarks>
        /// If no values are provided, the light temperature will remain unchanged.
        /// </remarks>
        [Tooltip("Randomly chooses a temperature from the options provided and assigns it to the Light component if the Light Appearance mode (under Emission) is set to Filter and Temperature (HDRP only). The probability of each color being selected can be modified by disabling the Uniform flag and providing probability values manually.")]
        public CategoricalParameter<float> temperatureList = new CategoricalParameter<float>();
        #endregion

        #region Color
        /// <summary>
        /// When set to true, <see cref="colorList" /> will be sampled to set the color/filter of the Light component.
        /// When set to false, <see cref="color" /> will be used instead.
        /// </summary>
        [Tooltip("When set to true, values for color will be sampled from a list of values. When false, a range of values will be used instead.")]
        public bool specifyColorAsList = true;

        /// <summary>
        /// Values for each channel (range: 0 to 1) will be sampled based on the chosen Sampling method under Value
        /// and set as the Light component's color/filter. When using the Filter and Temperate mode for Light Appearance
        /// (under Emission for HDRP lights only), filter will be changed. Otherwise, color will be changed.
        /// </summary>
        [Tooltip("Values for each channel (range: 0 to 1) will be sampled based on the chosen Sampling method under Value and set as the Light component's color/filter. When using the Filter and Temperate mode for Light Appearance (under Emission for HDRP lights only), filter will be changed. Otherwise, color will be changed.")]
        public ColorRgbParameter color = new ColorRgbParameter()
        {
            red = new ConstantSampler(120),
            green = new ConstantSampler(120),
            blue = new ConstantSampler(120),
            alpha = new ConstantSampler(1)
        };

        /// <summary>
        /// Randomly chooses a color from the options provided and assigns it to the Light component based on the Light
        /// Appearance mode set for the component (under Emission for HDRP lights only). If the Filter and Temperature mode is used, the
        /// selected color will be applied as a filter to the light. Otherwise if the Color mode is used, the selected color
        /// will used as the light's emission color. The probability of each color being selected can be modified by
        /// disabling the Uniform flag and providing probability values manually.
        /// </summary>
        /// <remarks>
        /// If no colors are provided, the light color will remain unchanged.
        /// </remarks>
        [Tooltip("Randomly chooses a color from the options provided, if there exists any, and assigns it to the Light component based on the Light Appearance mode set for the component (under Emission for HDRP lights only). If the Filter and Temperature mode is used, the selected color will be applied as a filter to the light. Otherwise if the Color mode is used, the selected color will used as the light's emission color. The probability of each color being selected can be modified by disabling the Uniform flag and providing probability values manually.")]
        public CategoricalParameter<Color> colorList = new CategoricalParameter<Color>();
        #endregion

        /// <summary>
        /// Based on the configuration of the <see cref="LightRandomizerTag" />, randomize state, intensity,
        /// temperature, and color of the <see cref="Light" /> component.
        /// </summary>
        public void Randomize()
        {
            // Randomize state
            Light.enabled = state.Sample();
            if (!Light.enabled)
            {
                return;
            }
#if HDRP_PRESENT
            // Randomize intensity
            if (!specifyIntensityAsList)
            {
                LightData.SetIntensity(intensity.Sample(), LightData.lightUnit);
            }
            else
            {
                if (intensityList.Count > 0)
                {
                    LightData.SetIntensity(intensityList.Sample(), LightData.lightUnit);
                }
            }
#endif

            // Randomize temperature
            if (Light.useColorTemperature)
            {
                if (!specifyTemperatureAsList)
                {
                    Light.colorTemperature = temperature.Sample();
                }
                else
                {
                    if (temperatureList.Count > 0)
                    {
                        Light.colorTemperature = temperatureList.Sample();
                    }
                }
            }

            // Randomize color/filter
            if (!specifyColorAsList)
            {
                Light.color = color.Sample();
            }
            else
            {
                if (colorList.Count > 0)
                {
                    Light.color = colorList.Sample();
                }
            }
        }
    }
}
