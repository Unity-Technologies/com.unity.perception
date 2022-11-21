using UnityEngine;

namespace UnityEngine.Perception.Randomization
{
    /// <summary>
    /// Derive this class to load Unity assets from a specific location
    /// </summary>
    public abstract class AssetSourceLocation
    {
        /// <summary>
        /// The number of assets available at this location
        /// </summary>
        public abstract int count { get; }

        /// <summary>
        /// Execute setup steps before accessing assets at this location
        /// </summary>
        /// <param name="assetRole">The asset role that will be used to preprocess assets from this location</param>
        /// <typeparam name="T">The type of assets that will be loaded from this location</typeparam>
        public abstract void Initialize<T>(AssetRole<T> assetRole) where T : Object;

        /// <summary>
        /// Unload all assets loaded from this location
        /// </summary>
        public abstract void ReleaseAssets();

        /// <summary>
        /// Retrieves an asset from this location using the provided index
        /// </summary>
        /// <param name="index">The index to load the asset from</param>
        /// <typeparam name="T">The type of asset to load</typeparam>
        /// <returns>The loaded asset</returns>
        public abstract T LoadAsset<T>(int index) where T : Object;
    }
}
