using System;

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
    }
}
