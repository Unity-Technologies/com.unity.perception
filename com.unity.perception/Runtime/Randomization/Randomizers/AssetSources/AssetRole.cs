namespace UnityEngine.Perception.Randomization
{
    /// <summary>
    /// Derive this class to create a typed asset role.
    /// Typed asset roles are used to apply preprocessing steps to assets loaded from an <see cref="AssetSource{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of asset to preprocess</typeparam>
    public abstract class AssetRole<T> : AssetRoleBase where T : Object
    {
        /// <summary>
        /// Perform preprocessing operations on an asset loaded from an <see cref="AssetSource{T}"/>.
        /// </summary>
        /// <param name="asset">The asset to preprocess</param>
        public abstract void Preprocess(T asset);
    }
}
