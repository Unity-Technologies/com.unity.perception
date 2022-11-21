using System;

namespace UnityEngine.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns a constant value when sampled
    /// </summary>
    [Serializable]
    public class ConstantSampler : ISampler
    {
        /// <summary>
        /// The value from which samples will be generated
        /// </summary>
        public float value;

        ///<inheritdoc/>
#if !SCENARIO_CONFIG_POWER_USER
        [field: HideInInspector]
#endif
        [field: SerializeField]
        public float minAllowed { get; set; }
        ///<inheritdoc/>
#if !SCENARIO_CONFIG_POWER_USER
        [field: HideInInspector]
#endif
        [field: SerializeField]
        public float maxAllowed { get; set; }
        ///<inheritdoc/>
#if !SCENARIO_CONFIG_POWER_USER
        [field: HideInInspector]
#endif
        [field: SerializeField]
        public bool shouldCheckValidRange { get; set; }

        /// <summary>
        /// Constructs a ConstantSampler
        /// </summary>
        public ConstantSampler()
        {
            value = 0f;
        }

        /// <summary>
        /// Constructs a new ConstantSampler
        /// </summary>
        /// <param name="value">The value from which samples will be generated</param>
        /// <param name="shouldCheckValidRange">Whether the provided <see cref="minAllowed"/> and <see cref="maxAllowed"/> values should be used to validate the <see cref="value"/> provided</param>
        /// <param name="minAllowed">The smallest min value allowed for this range</param>
        /// <param name="maxAllowed">The largest max value allowed for this range</param>
        public ConstantSampler(float value, bool shouldCheckValidRange = false, float minAllowed = 0, float maxAllowed = 0)
        {
            this.value = value;
            this.shouldCheckValidRange = shouldCheckValidRange;
            this.minAllowed = minAllowed;
            this.maxAllowed = maxAllowed;
        }

        /// <summary>
        /// Generates one sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public float Sample()
        {
            return value;
        }

        /// <summary>
        /// Validates that the sampler is configured properly
        /// </summary>
        public void Validate()
        {
            CheckAgainstValidRange();
        }

        /// <summary>
        /// Checks if range valid
        /// </summary>
        public void CheckAgainstValidRange()
        {
            if (shouldCheckValidRange && (value < minAllowed || value > maxAllowed))
            {
                Debug.LogError($"The value a {GetType().Name} exceeds the allowed valid range. Clamping to valid range.");
                value = Mathf.Clamp(value, minAllowed, maxAllowed);
            }
        }
    }
}
