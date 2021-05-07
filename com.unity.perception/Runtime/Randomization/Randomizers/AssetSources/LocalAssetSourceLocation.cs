using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEngine.Perception.Randomization
{
    /// <summary>
    /// A basic <see cref="AssetSourceLocation"/> for loading local project assets
    /// </summary>
    [Serializable]
    [DisplayName("Assets In Project")]
    public class LocalAssetSourceLocation : AssetSourceLocation
    {
        /// <summary>
        /// The list of local assets available from this source
        /// </summary>
        [SerializeField] public List<Object> assets;

        /// <inheritdoc/>
        public override int Count => assets.Count;

        /// <inheritdoc/>
        public override void Initialize<T>(Archetype<T> archetype) { }

        /// <inheritdoc/>
        public override void ReleaseAssets() { }

        /// <inheritdoc/>
        public override T LoadAsset<T>(int index)
        {
            return (T)assets[index];
        }
    }
}
