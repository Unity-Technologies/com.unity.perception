using System;

namespace UnityEngine.Experimental.Perception.Randomization.Scenarios
{
    /// <summary>
    /// A scenario that runs for a fixed number of frames during each iteration
    /// </summary>
    [AddComponentMenu("Perception/Randomization/Scenarios/Fixed Length Scenario")]
    public class FixedLengthScenario: UnitySimulationScenario<FixedLengthScenario.Constants>
    {
        /// <summary>
        /// Constants describing the execution of this scenario
        /// </summary>
        [Serializable]
        public class Constants : UnitySimulationConstants
        {
            /// <summary>
            /// The number of frames to generate per iteration
            /// </summary>
            public int framesPerIteration = 1;
        }

        /// <summary>
        /// Returns whether the current scenario iteration has completed
        /// </summary>
        public override bool isIterationComplete => currentIterationFrame >= constants.framesPerIteration;
    }
}
