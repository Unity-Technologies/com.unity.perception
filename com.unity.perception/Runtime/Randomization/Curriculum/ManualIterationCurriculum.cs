namespace UnityEngine.Perception.Randomization.Curriculum
{
    public class ManualIterationCurriculum : CurriculumBase
    {
        bool m_FinishedIteration;

        public override bool Complete => false;

        public override bool FinishedIteration => m_FinishedIteration;

        public void ManuallyFinishIteration()
        {
            m_FinishedIteration = true;
        }

        public override void Iterate()
        {
            m_FinishedIteration = false;
            m_CurrentIteration++;
        }
    }
}
