using System;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// An example scenario where each scenario iteration runs for exactly one frame
    /// </summary>
    [AddComponentMenu("Randomization/Scenarios/One Frame Scenario")]
    public class OneFrameScenario: Scenario<OneFrameScenario.Constants>
    {
        [Serializable]
        public struct Constants
        {
            public int startingIteration;
            public int totalIterations;
        }

        public override bool isIterationComplete => iterationFrameCount >= 1;

        public override bool isScenarioComplete => currentIteration == constants.totalIterations;

        public override void Initialize()
        {
            currentIteration = constants.startingIteration;
        }
    }
}
