using System;
using System.Linq;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// Derive this class to configure perception data capture while coordinating a scenario
    /// </summary>
    /// <typeparam name="T">The type of scenario constants to serialize</typeparam>
    public abstract class PerceptionScenario<T> : Scenario<T> where T : ScenarioConstants, new()
    {
        /// <summary>
        /// The metric definition used to report the current scenario iteration
        /// </summary>
        MetricDefinition m_IterationMetricDefinition;

        MetricDefinition m_RandomSeedMetricDefinition;

        /// <inheritdoc/>
        protected override bool isScenarioReadyToStart => PerceptionCamera.captureFrameCount >= 0;

        /// <inheritdoc/>
        protected override void OnStart()
        {
            m_IterationMetricDefinition = new MetricDefinition("scenario_iteration", "Iteration information for dataset sequences");
            DatasetCapture.RegisterMetric(m_IterationMetricDefinition);

            m_RandomSeedMetricDefinition = new MetricDefinition("random-seed", "The random seed used to initialize the random state of the simulation. Only triggered once per simulation.");
            DatasetCapture.RegisterMetric(m_RandomSeedMetricDefinition);

            DatasetCapture.ReportMetadata("scenarioRandomSeed", genericConstants.randomSeed);
            DatasetCapture.ReportMetadata("scenarioActiveRandomizers", activeRandomizers.Select(r => r.GetType().Name).ToArray());
        }

        /// <inheritdoc/>
        protected override void OnIterationStart()
        {
            DatasetCapture.StartNewSequence();

            if (Application.isPlaying)
            {
                DatasetCapture.ReportMetric(m_RandomSeedMetricDefinition, new GenericMetric(genericConstants.randomSeed, m_RandomSeedMetricDefinition));
                DatasetCapture.ReportMetric(m_IterationMetricDefinition, new GenericMetric(currentIteration, m_IterationMetricDefinition));
            }
        }

        /// <inheritdoc/>
        protected override void OnComplete()
        {
            DatasetCapture.ResetSimulation();
            Quit();
        }
    }
}
