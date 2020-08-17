using System;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// A scenario that runs for a fixed number of frames during each iteration
    /// </summary>
    [AddComponentMenu("Perception/Randomization/Scenarios/Fixed Length Scenario")]
    public class FixedLengthScenario: Scenario<FixedLengthScenario.Constants>
    {
        /// <summary>
        /// Constants describing the execution of this scenario
        /// </summary>
        [Serializable]
        public class Constants
        {
            /// <summary>
            /// The number of frames to generate per iteration
            /// </summary>
            public int framesPerIteration = 1;

            /// <summary>
            /// The iteration index begin the simulation on
            /// </summary>
            public int startingIteration;

            /// <summary>
            /// The total number of iterations to complete before the simulation terminates
            /// </summary>
            public int totalIterations = 1000;
        }

        /// <summary>
        /// Returns whether the current scenario iteration has completed
        /// </summary>
        public override bool isIterationComplete => currentIterationFrame >= constants.framesPerIteration;

        /// <summary>
        /// Returns whether the scenario has completed
        /// </summary>
        public override bool isScenarioComplete => currentIteration >= constants.totalIterations;

        /// <summary>
        /// Called before the scenario begins iterating
        /// </summary>
        public override void OnInitialize()
        {
            currentIteration = constants.startingIteration;
        }
    }
}
