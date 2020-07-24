using Newtonsoft.Json.Linq;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// An example scenario where each scenario iteration runs for exactly one frame
    /// </summary>
    [AddComponentMenu("Randomization/Scenarios/One Frame Scenario")]
    public class OneFrameScenario : Scenario
    {
        public int startingIteration;
        public int totalIterations;

        public override bool isIterationComplete => iterationFrameCount >= 1;

        public override bool isScenarioComplete => currentIteration == totalIterations;

        public override void Initialize()
        {
            currentIteration = startingIteration;
        }

        public override JObject Serialize()
        {
            return new JObject
            {
                ["startingIteration"] = startingIteration,
                ["totalIterations"] = totalIterations
            };
        }

        public override void Deserialize(JObject token)
        {
            startingIteration = token["startingIteration"].Value<int>();
            totalIterations = token["totalIterations"].Value<int>();
        }
    }
}
