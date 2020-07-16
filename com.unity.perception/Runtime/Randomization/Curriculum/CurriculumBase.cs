using System;
using UnityEngine.Perception.Randomization.Configuration;

namespace UnityEngine.Perception.Randomization.Curriculum
{
    public abstract class CurriculumBase : MonoBehaviour
    {
        [HideInInspector] public ParameterConfiguration parameterConfiguration;
        protected int m_CurrentIteration;
        public int CurrentIteration => m_CurrentIteration;
        public abstract bool Complete { get; }
        public abstract bool FinishedIteration { get; }

        public abstract void Iterate();
    }
}
