namespace UnityEngine.Perception.Randomization.Scenarios
{
    /// <summary>
    /// An example scenario that runs for exactly one frame
    /// </summary>
    public class DefaultScenario : Scenario
    {
        bool m_RanForOneFrame;
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

        public override void Teardown()
        {
            m_RanForOneFrame = false;
        }
    }
}
