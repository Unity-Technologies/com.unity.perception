using System;
using Unity.Simulation;

namespace UnityEngine.Experimental.Perception.Randomization.Scenarios
{
    /// <summary>
    /// Defines a scenario that is compatible with the Run in Unity Simulation window
    /// </summary>
    /// <typeparam name="T">The type of constants to serialize</typeparam>
    public abstract class UnitySimulationScenario<T> : Scenario<T> where T : UnitySimulationScenarioConstants, new()
    {
        /// <summary>
        /// Returns whether the entire scenario has completed
        /// </summary>
        public sealed override bool isScenarioComplete => currentIteration >= constants.totalIterations;

        /// <summary>
        /// Progresses the current scenario iteration
        /// </summary>
        protected sealed override void IncrementIteration()
        {
            currentIteration += constants.instanceCount;
        }

        /// <summary>
        /// Deserializes this scenario's constants from the Unity Simulation AppParams Json file
        /// </summary>
        public sealed override void Deserialize()
        {
            if (Configuration.Instance.IsSimulationRunningInCloud())
                constants = Configuration.Instance.GetAppParams<T>();
            else
                base.Deserialize();
            currentIteration = constants.instanceIndex;
        }
    }
}
