using System;
using Newtonsoft.Json.Linq;

namespace UnityEngine.Perception.Randomization.Curriculum
{
    [CurriculumMetaData("Random Sampling")]
    public class RandomSamplingCurriculum : CurriculumBase
    {
        public int totalIterations = 100;
        public override bool Complete => CurrentIteration == totalIterations;

        public override bool FinishedIteration => true;

        public override void Iterate()
        {
            m_CurrentIteration++;
        }

        public override JObject Serialize()
        {
            return new JObject { ["totalIterations"] = totalIterations };
        }

        public override void Deserialize(JObject token)
        {
            totalIterations = token["totalIterations"].Value<int>();
        }
    }
}
