using System;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// A class encapsulating the scenario constants fields required for Unity Simulation cloud execution
    /// </summary>
    [Serializable]
    public class UnitySimulationScenarioConstants : ScenarioConstants
    {
        /// <summary>
        /// The total number of iterations to run a scenario for. At the start of each iteration, the timings for all Perception Cameras will be reset.
        /// </summary>
        [Tooltip("The total number of iterations to run a scenario for. At the start of each iteration, the timings for all Perception Cameras will be reset.")]
        public int totalIterations = 100;

        /// <summary>
        /// The number of Unity Simulation instances assigned to execute this scenario. The total number of iterations (N) will be divided by the number of instances (M), so each instance will run for N/M iterations.
        /// </summary>
        [Tooltip("The number of Unity Simulation instances assigned to execute this scenario. The total number of iterations (N) will be divided by the number of instances (M), so each instance will run for N/M iterations.")]
        public int instanceCount = 1;

        /// <summary>
        /// The Unity Simulation instance index of the currently executing worker.
        /// </summary>
        [Tooltip("The Unity Simulation instance index of the currently executing worker.")]
        public int instanceIndex;
    }
}
