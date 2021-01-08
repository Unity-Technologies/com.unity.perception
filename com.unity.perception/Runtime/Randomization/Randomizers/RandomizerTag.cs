using System;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Derive the RandomizerTag class to create new tag components.
    /// RandomizerTags are used to help randomizers query for a set of GameObjects to randomize.
    /// </summary>
    [Serializable]
    public abstract class RandomizerTag : MonoBehaviour
    {
        ScenarioBase scenario => ScenarioBase.activeScenario;

        void Awake()
        {
            if (scenario)
                scenario.tagManager.AddTag(GetType(), gameObject);
        }

        void OnDestroy()
        {
            if (scenario)
                scenario.tagManager.RemoveTag(GetType(), gameObject);
        }
    }
}
