using UnityEngine.Perception.Randomization.Parameters.MonoBehaviours;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Scenarios.Abstractions
{
    public abstract class Scenario : MonoBehaviour
    {
        [HideInInspector]
        public ParameterConfiguration parameterConfiguration;
        public abstract int FrameCount { get; }
        public virtual void Setup() { }
        public virtual void Teardown() { }
    }
}
