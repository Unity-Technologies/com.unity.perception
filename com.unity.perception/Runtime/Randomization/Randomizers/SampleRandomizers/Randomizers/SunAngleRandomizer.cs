using System;
using UnityEngine.Experimental.Perception.Randomization.Parameters;
using UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers.Tags;
using UnityEngine.Experimental.Perception.Randomization.Samplers;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Randomizes the rotation of directional lights tagged with a SunAngleRandomizerTag
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Sun Angle Randomizer")]
    public class SunAngleRandomizer : Randomizer
    {
        /// <summary>
        /// The hour of the day (0 to 24)
        /// </summary>
        public FloatParameter hour = new FloatParameter { value = new UniformSampler(0, 24)};

        /// <summary>
        /// The day of the year (0 being Jan 1st and 364 being December 31st)
        /// </summary>
        public FloatParameter dayOfTheYear = new FloatParameter { value = new UniformSampler(0, 365)};

        /// <summary>
        /// The earth's latitude (-90 is the south pole, 0 is the equator, and +90 is the north pole)
        /// </summary>
        public FloatParameter latitude = new FloatParameter { value = new UniformSampler(-90, 90)};

        /// <summary>
        /// Randomizes the rotation of tagged directional lights at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var lightObjects = tagManager.Query<SunAngleRandomizerTag>();
            foreach (var lightObject in lightObjects)
            {
                var earthSpin = Quaternion.AngleAxis((hour.Sample() + 12f) / 24f * 360f, Vector3.down);
                var timeOfYearRads = dayOfTheYear.Sample() / 365f * Mathf.PI * 2f;
                var earthTilt = Quaternion.Euler(Mathf.Cos(timeOfYearRads) * 23.5f, 0, Mathf.Sin(timeOfYearRads) * 23.5f);
                var earthLat = Quaternion.AngleAxis(latitude.Sample(), Vector3.right);
                var lightRotation = earthTilt * earthSpin * earthLat;
                lightObject.transform.rotation = Quaternion.Euler(90,0,0) * Quaternion.Inverse(lightRotation);
            }
        }
    }
}
