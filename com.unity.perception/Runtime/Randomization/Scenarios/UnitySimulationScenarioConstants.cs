using System;

namespace UnityEngine.Experimental.Perception.Randomization.Scenarios
{
    /// <summary>
    /// A class encapsulating the scenario constants fields required for Unity Simulation cloud execution
    /// </summary>
    [Serializable]
    public class UnitySimulationScenarioConstants : ScenarioConstants
    {
        /// <summary>
        /// The total number of iterations to run a scenario for
        /// </summary>
        public int totalIterations = 100;

        /// <summary>
        /// The number of Unity Simulation instances assigned to executed this scenario
        /// </summary>
        public int instanceCount = 1;

        /// <summary>
        /// The Unity Simulation instance index of the currently executing worker
        /// </summary>
        public int instanceIndex;
    }
}
