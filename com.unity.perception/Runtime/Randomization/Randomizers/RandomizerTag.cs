using System;

namespace UnityEngine.Experimental.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Derive the RandomizerTag class to create new tag components.
    /// RandomizerTags are used to help randomizers query for a set of GameObjects to randomize.
    /// </summary>
    [Serializable]
    public abstract class RandomizerTag : MonoBehaviour
    {
        RandomizerTagManager tagManager => RandomizerTagManager.singleton;

        void Awake()
        {
            tagManager.AddTag(this);
        }

        void OnDestroy()
        {
            tagManager.RemoveTag(this);
        }
    }
}
