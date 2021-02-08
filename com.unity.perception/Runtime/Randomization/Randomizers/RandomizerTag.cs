using System;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// Derive the RandomizerTag class to create new tag components.
    /// RandomizerTags are used to help randomizers query for a set of GameObjects to randomize.
    /// </summary>
    [Serializable]
    public abstract class RandomizerTag : MonoBehaviour
    {
        RandomizerTagManager tagManager => RandomizerTagManager.singleton;

        /// <summary>
        /// Awake is called when this RandomizerTag is created or instantiated
        /// </summary>
        protected virtual void Awake()
        {
            Register();
        }

        /// <summary>
        /// OnDestroy is called when this RandomizerTag is destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            Unregister();
        }

        /// <summary>
        /// Registers this tag with the tagManager
        /// </summary>
        public void Register()
        {
            tagManager.AddTag(this);
        }

        /// <summary>
        /// Unregisters this tag with the tagManager
        /// </summary>
        public void Unregister()
        {
            tagManager.RemoveTag(this);
        }
    }
}
