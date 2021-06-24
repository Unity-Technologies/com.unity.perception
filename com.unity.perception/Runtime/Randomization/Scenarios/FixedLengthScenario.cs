using System;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// A scenario that runs for a fixed number of frames during each iteration
    /// </summary>
    [AddComponentMenu("Perception/Scenarios/Fixed Length Scenario")]
    public class FixedLengthScenario: UnitySimulationScenario<FixedLengthScenario.Constants>
    {
        /// <summary>
        /// Constants describing the execution of this scenario
        /// </summary>
        [Serializable]
        public class Constants : UnitySimulationScenarioConstants
        {
            /// <summary>
            /// The number of frames to render per iteration.
            /// </summary>
            [Tooltip("The number of frames to render per iteration.")]
            public int framesPerIteration = 1;
        }

        /// <summary>
        /// Returns whether the current scenario iteration has completed
        /// </summary>
        protected override bool isIterationComplete => currentIterationFrame >= constants.framesPerIteration;
    }
}
