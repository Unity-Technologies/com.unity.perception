using System;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// An example scenario where each scenario iteration runs for exactly one frame
    /// </summary>
    [AddComponentMenu("Randomization/Scenarios/Fixed Frame Length Scenario")]
    public class FixedFrameLengthScenario: Scenario<FixedFrameLengthScenario.Constants>
    {
        [Serializable]
        public class Constants
        {
            public int iterationFrameCount = 1;
            public int startingIteration;
            public int totalIterations = 1000;
        }

        public override bool isIterationComplete => iterationFrameCount >= constants.iterationFrameCount;

        public override bool isScenarioComplete => currentIteration >= constants.totalIterations;

        public override void Initialize()
        {
            currentIteration = constants.startingIteration;
        }
    }
}
