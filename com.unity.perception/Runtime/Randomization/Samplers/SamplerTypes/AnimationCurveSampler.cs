using System;
using Unity.Burst;
using Unity.Collections;
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

        NativeArray<float> m_IntegratedCurve;
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
            var rng = new Unity.Mathematics.Random(ScenarioBase.activeScenario.NextRandomState());
            return SamplerUtility.AnimationCurveSample(m_IntegratedCurve, rng.NextFloat(), m_Interval, m_StartTime, m_EndTime);
        }

        /// <summary>
        /// Schedules a job to generate an array of samples
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate</param>
        /// <param name="jobHandle">The handle of the scheduled job</param>
        /// <returns>A NativeArray of generated samples</returns>
        public NativeArray<float> Samples(int sampleCount, out JobHandle jobHandle)
        {
            var samples = new NativeArray<float>(
                sampleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            jobHandle = new SampleJob
            {
                integratedCurve = m_IntegratedCurve,
                interval = m_Interval,
                startTime = m_StartTime,
                endTime = m_EndTime,
                seed = ScenarioBase.activeScenario.NextRandomState(),
                curveValid = m_CurveValid,
                samples = samples
            }.ScheduleBatch(sampleCount, SamplerUtility.samplingBatchSize);
            return samples;
        }

        [BurstCompile]
        struct SampleJob : IJobParallelForBatch
        {
            public NativeArray<float> integratedCurve;
            public float interval;
            public float startTime;
            public float endTime;
            public uint seed;
            public NativeArray<float> samples;
            public bool curveValid;

            public void Execute(int startIndex, int count)
            {
                var endIndex = startIndex + count;
                var batchIndex = startIndex / SamplerUtility.samplingBatchSize;
                var rng = new Unity.Mathematics.Random(SamplerUtility.IterateSeed((uint)batchIndex, seed));
                if (!curveValid)
                {
                    Debug.LogError("The distribution curve provided for an Animation Curve sampler is empty.");
                    return;
                }
                for (var i = startIndex; i < endIndex; i++)
                    samples[i] = SamplerUtility.AnimationCurveSample(integratedCurve, rng.NextFloat(), interval, startTime, endTime);
            }
        }

        /// <summary>
        /// Used for performing sampler specific clean-up tasks (e.g. once the scenario is complete).
        /// </summary>
        public void Cleanup()
        {
            if (m_IntegratedCurve.IsCreated)
            {
                m_IntegratedCurve.Dispose();
            }
        }

        /// <summary>
        /// Used for performing sampler specific clean-up tasks (e.g. once the scenario is complete).
        /// </summary>
        public void Initialize()
        {
            m_CurveValid = false;

            if (distributionCurve.length == 0)
            {
                Debug.LogError("The distribution curve provided for an Animation Curve sampler is empty.");
                return;
            }

            m_CurveValid = true;

            if (!m_IntegratedCurve.IsCreated)
            {
                m_IntegratedCurve = new NativeArray<float>(m_NumOfSamples, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                SamplerUtility.IntegrateCurve(m_IntegratedCurve, distributionCurve);

                m_StartTime = distributionCurve.keys[0].time;
                m_EndTime = distributionCurve.keys[distributionCurve.length - 1].time;
                m_Interval = (m_EndTime - m_StartTime) / (m_NumOfSamples - 1);
            }
        }
    }
}
