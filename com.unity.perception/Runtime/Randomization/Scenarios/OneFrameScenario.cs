using Newtonsoft.Json.Linq;

namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// An example scenario that runs for exactly one frame
    /// </summary>
    public class OneFrameScenario : Scenario
    {
        bool m_RanForOneFrame;
        int m_CurrentIteration;

        public int startingIteration;
        public int totalIterations;

        public override bool Running
        {
            get
            {
                if (m_RanForOneFrame)
                    return false;
                m_RanForOneFrame = true;
                return true;
            }
        }

        public override bool Complete => m_CurrentIteration == totalIterations;
        public override int CurrentIteration => m_CurrentIteration;
        public override void Iterate()
        {
            m_CurrentIteration++;
        }

        public override void Initialize()
        {
            m_CurrentIteration = startingIteration;
        }

        public override void Teardown()
        {
            m_RanForOneFrame = false;
        }

        public override JObject Serialize()
        {
            var jo = new JObject
            {
                ["startingIteration"] = startingIteration,
                ["totalIterations"] = totalIterations
            };
            return jo;
        }

        public override void Deserialize(JObject token)
        {
            startingIteration = token["startingIteration"].Value<int>();
            totalIterations = token["totalIterations"].Value<int>();
        }
    }
}
