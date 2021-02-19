using System;
using Unity.Simulation;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// Defines a scenario that is compatible with the Run in Unity Simulation window
    /// </summary>
    /// <typeparam name="T">The type of constants to serialize</typeparam>
    public abstract class UnitySimulationScenario<T> : Scenario<T> where T : UnitySimulationScenarioConstants, new()
    {
        /// <inheritdoc/>
        public sealed override bool isScenarioComplete => currentIteration >= constants.totalIterations;

        /// <inheritdoc/>
        protected sealed override void IncrementIteration()
        {
            currentIteration += constants.instanceCount;
        }

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            // Don't skip the first frame if executing on Unity Simulation
            if (Configuration.Instance.IsSimulationRunningInCloud())
                m_SkipFrame = false;
        }

        /// <inheritdoc/>
        protected override void OnStart()
        {
            if (Configuration.Instance.IsSimulationRunningInCloud())
            {
                DeserializeFromFile(new Uri(Configuration.Instance.SimulationConfig.app_param_uri).LocalPath);
                constants.instanceIndex = int.Parse(Configuration.Instance.GetInstanceId()) - 1;
            }
            else
                base.OnStart();
            currentIteration = constants.instanceIndex;
        }
    }
}
