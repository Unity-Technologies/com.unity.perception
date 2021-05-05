namespace UnityEngine.Perception.Randomization
{
    /// <summary>
    /// Derive this class to create a typed Archetype.
    /// Typed Archetypes are used to apply preprocessing steps to assets loaded from an <see cref="AssetSource{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of asset to preprocess</typeparam>
    public abstract class Archetype<T> : ArchetypeBase where T : Object
    {
        /// <summary>
        /// Perform preprocessing operations on an asset loaded from an <see cref="AssetSource{T}"/>.
        /// </summary>
        /// <param name="asset">The asset to preprocess</param>
        public abstract void Preprocess(T asset);
    }
}
