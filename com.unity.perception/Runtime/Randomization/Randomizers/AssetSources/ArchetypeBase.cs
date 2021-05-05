using UnityEngine;

namespace UnityEngine.Perception.Randomization
{
    /// <summary>
    /// The base Archetype class. Derive from <see cref="AssetSource{T}"/> instead to create a new archetype.
    /// </summary>
    public abstract class ArchetypeBase
    {
        /// <summary>
        /// The string label uniquely associated with this archetype
        /// </summary>
        public abstract string label { get; }
    }
}
