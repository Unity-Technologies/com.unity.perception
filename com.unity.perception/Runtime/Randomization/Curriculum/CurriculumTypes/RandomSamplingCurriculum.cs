using System;
using Newtonsoft.Json.Linq;

namespace UnityEngine.Perception.Randomization.Curriculum
{
    [CurriculumMetaData("Random Sampling")]
    public class RandomSamplingCurriculum : CurriculumBase
    {
        public int startingIteration;
        public int totalIterations = 100;
        public override bool Complete => CurrentIteration == totalIterations;

        public override bool FinishedIteration => true;

        public override void Initialize()
        {
            m_CurrentIteration = startingIteration;
        }

        public override void Iterate()
        {
            m_CurrentIteration++;
        }

        public override JObject Serialize()
        {
            return new JObject { ["startingIteration"] = startingIteration, ["totalIterations"] = totalIterations };
        }

        public override void Deserialize(JObject token)
        {
            totalIterations = token["totalIterations"].Value<int>();
            startingIteration = token["startingIteration"].Value<int>();
        }
    }
}
