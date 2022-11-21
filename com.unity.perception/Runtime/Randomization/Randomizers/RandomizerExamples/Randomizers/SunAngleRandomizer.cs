using System;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.Tags;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Randomizes the rotation of directional lights tagged with a SunAngleRandomizerTag
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/Sun Angle Randomizer")]
    [MovedFrom("UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers")]
    public class SunAngleRandomizer : Randomizer
    {
        /// <summary>
        /// The range of hours in a day (default is 0 to 24)
        /// </summary>
        [Tooltip("The range of hours in a day (default is 0 to 24).")]
        public FloatParameter hour = new FloatParameter { value = new UniformSampler(0, 24)};

        /// <summary>
        /// The range of days in a year with 0 being Jan 1st and 364 being December 31st (default is 0 to 364)
        /// </summary>
        [Tooltip("The range of days in a year with 0 being Jan 1st and 364 being December 31st (default is 0 to 364).")]
        public FloatParameter dayOfTheYear = new FloatParameter { value = new UniformSampler(0, 364)};

        /// <summary>
        /// The range of latitudes. A latitude of -90 is the south pole, 0 is the equator, and +90 is the north pole (default is -90 to 90).
        /// </summary>
        [Tooltip("The range of latitudes. A latitude of -90 is the south pole, 0 is the equator, and +90 is the north pole (default is -90 to 90).")]
        public FloatParameter latitude = new FloatParameter { value = new UniformSampler(-90, 90)};

        /// <summary>
        /// Randomizes the rotation of tagged directional lights at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var tags = tagManager.Query<SunAngleRandomizerTag>();
            foreach (var tag in tags)
            {
                var earthSpin = Quaternion.AngleAxis((hour.Sample() + 12f) / 24f * 360f, Vector3.down);
                var timeOfYearRads = dayOfTheYear.Sample() / 365f * Mathf.PI * 2f;
                var earthTilt = Quaternion.Euler(Mathf.Cos(timeOfYearRads) * 23.5f, 0, Mathf.Sin(timeOfYearRads) * 23.5f);
                var earthLat = Quaternion.AngleAxis(latitude.Sample(), Vector3.right);
                var lightRotation = earthTilt * earthSpin * earthLat;
                tag.transform.rotation = Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(lightRotation);
            }
        }
    }
}
