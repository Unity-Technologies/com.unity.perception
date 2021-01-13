﻿using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;

namespace UnityEngine.Experimental.Perception.Randomization.Samplers
{
    /// <summary>
    /// Returns random values according to a range and probability distribution denoted by a user provided AnimationCurve. The X axis of the AnimationCurve corresponds to the values this sampler will pick from, and the Y axis corresponds to the relative probability of the values. The relative probabilities (Y axis) do not need to max out at 1, as only the shape of the curve matters. The Y values cannot however be negative.
    /// </summary>
    [Serializable]
    public class AnimationCurveSampler : ISampler
    {
        /// <summary>
        /// The range field is not used by the AnimationCurve sampler.
        /// </summary>
        [field: NonSerialized]
        public FloatRange range { get; set; }

        /// <summary>
        /// The Animation Curve associated with this sampler
        /// </summary>
        [Tooltip("Probability distribution curve used for this sampler. The X axis corresponds to the values this sampler will pick from, and the Y axis corresponds to the relative probability of the values. The relative probabilities (Y axis) do not need to max out at 1 as only the shape of the curve matters. The Y values cannot however be negative.")]
        public AnimationCurve distributionCurve;

        float[] m_IntegratedCurve;
        bool m_CurveValid;
        float m_StartTime;
        float m_EndTime;
        float m_Interval;
        int m_NumOfSamples = 100;

        /// <summary>
        /// Constructs an Animation Curve Sampler
        /// </summary>
        public AnimationCurveSampler()
        {
            distributionCurve = new AnimationCurve();
            distributionCurve.AddKey(0, 0);
            distributionCurve.AddKey(0.5f, 1);
            distributionCurve.AddKey(1, 0);
        }

        /// <summary>
        /// Generates one sample
        /// </summary>
        /// <returns>The generated sample</returns>
        public float Sample()
        {
            Initialize();
            if (!m_CurveValid)
            {
                return 0;
            }

            var rng = new Unity.Mathematics.Random(ScenarioBase.activeScenario.NextRandomState());
            return SamplerUtility.AnimationCurveSample(m_IntegratedCurve, rng.NextFloat(), m_Interval, m_StartTime, m_EndTime);
        }

        void Initialize()
        {
            m_CurveValid = false;

            if (distributionCurve.length == 0)
            {
                Debug.LogError("The distribution curve provided for an Animation Curve sampler is empty.");
                return;
            }

            m_CurveValid = true;

            if (m_IntegratedCurve == null)
            {
                m_IntegratedCurve = new float[m_NumOfSamples];

                SamplerUtility.IntegrateCurve(m_IntegratedCurve, distributionCurve);

                m_StartTime = distributionCurve.keys[0].time;
                m_EndTime = distributionCurve.keys[distributionCurve.length - 1].time;
                m_Interval = (m_EndTime - m_StartTime) / (m_NumOfSamples - 1);
            }
        }
    }
}