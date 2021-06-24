using System;
using Unity.Simulation;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// A scenario must derive from this class to be compatible with the Run in
    /// Unity Simulation window. The iterations of this scenario will be executed in parallel across a user specified
    /// number of worker instances when run in Unity Simulation.
    /// </summary>
    /// <typeparam name="T">The type of scenario constants to serialize</typeparam>
    public abstract class UnitySimulationScenario<T> : PerceptionScenario<T>
        where T : UnitySimulationScenarioConstants, new()
    {
        /// <inheritdoc/>
        protected override bool isScenarioReadyToStart
        {
            get
            {
                if (!Configuration.Instance.IsSimulationRunningInCloud() && !m_SkippedFirstFrame)
                {
                    m_SkippedFirstFrame = true;
                    return false;
                }
                return true;
            }
        }

        /// <inheritdoc/>
        protected sealed override bool isScenarioComplete => currentIteration >= constants.totalIterations;

        /// <inheritdoc/>
        protected override void OnConfigurationImport()
        {
            if (Configuration.Instance.IsSimulationRunningInCloud())
            {
                DeserializeFromFile(new Uri(Configuration.Instance.SimulationConfig.app_param_uri).LocalPath);
                constants.instanceIndex = int.Parse(Configuration.Instance.GetInstanceId()) - 1;
            }
            else
                base.OnConfigurationImport();
            currentIteration = constants.instanceIndex;
        }

        /// <inheritdoc/>
        protected sealed override void IncrementIteration()
        {
            currentIteration += constants.instanceCount;
        }
    }
}
