using System;
using System.IO;
using Unity.Simulation;
using UnityEngine.Perception.Randomization.Samplers;

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
        public override string defaultConfigFilePath =>
            Configuration.Instance.IsSimulationRunningInCloud()
                ? new Uri(Configuration.Instance.SimulationConfig.app_param_uri).LocalPath
                : base.defaultConfigFilePath;

        /// <inheritdoc/>
        protected sealed override void IncrementIteration()
        {
            currentIteration += constants.instanceCount;
        }

        /// <inheritdoc/>
        public sealed override void DeserializeFromFile(string configFilePath)
        {
            base.DeserializeFromFile(configFilePath);
            currentIteration = constants.instanceIndex * constants.instanceCount;
        }
    }
}
