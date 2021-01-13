using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;

namespace UnityEngine.Experimental.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns random values according to a range and probability distribution denoted by a user provided AnimationCurve.
    /// The X axis of the AnimationCurve corresponds to the values this sampler will pick from,
    /// and the Y axis corresponds to the relative probability of the values.
    /// The relative probabilities (Y axis) do not need to max out at 1, as only the shape of the curve matters.
    /// The Y values cannot however be negative.
    /// </summary>
    [Serializable]
    public class AnimationCurveSampler : ISampler
    {
        /// <summary>
        /// The Animation Curve associated with this sampler
        /// </summary>
        [Tooltip("Probability distribution curve used for this sampler. The X axis corresponds to the values this sampler will pick from, and the Y axis corresponds to the relative probability of the values. The relative probabilities (Y axis) do not need to max out at 1 as only the shape of the curve matters. The Y values cannot however be negative.")]
        public AnimationCurve distributionCurve = new AnimationCurve(
            new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));

        /// <summary>
        /// Number of samples used for integrating over the provided AnimationCurve.
        /// The larger the number of samples, the more accurate the resulting probability distribution will be.
        /// </summary>
        [Tooltip("Number of internal samples used for integrating over the provided AnimationCurve. The larger the number of samples, the more accurately the resulting probability distribution will follow the provided AnimationCurve. Increase this if the default value proves insufficient.")]
        public int numOfSamplesForIntegration = 500;

        float[] m_IntegratedCurve;
        bool m_Initialized;
        float m_StartTime;
        float m_EndTime;
        float m_Interval;

        /// <summary>
        /// Generates one sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public float Sample()
        {
            Initialize();
            var rng = new Unity.Mathematics.Random(ScenarioBase.activeScenario.NextRandomState());
            return SamplerUtility.AnimationCurveSample(
                m_IntegratedCurve, rng.NextFloat(), m_Interval, m_StartTime, m_EndTime);
        }

        /// <summary>
        /// Validates that the sampler is configured properly
        /// </summary>
        /// <exception cref="SamplerValidationException"></exception>
        public void Validate()
        {
            if (distributionCurve.length == 0)
                throw new SamplerValidationException("The distribution curve provided is empty");
        }

        void Initialize()
        {
            if (m_Initialized)
                return;

            Validate();
            m_IntegratedCurve = new float[numOfSamplesForIntegration];
            SamplerUtility.IntegrateCurve(m_IntegratedCurve, distributionCurve);
            m_StartTime = distributionCurve.keys[0].time;
            m_EndTime = distributionCurve.keys[distributionCurve.length - 1].time;
            m_Interval = (m_EndTime - m_StartTime) / (numOfSamplesForIntegration - 1);
            m_Initialized = true;
        }
    }
}
