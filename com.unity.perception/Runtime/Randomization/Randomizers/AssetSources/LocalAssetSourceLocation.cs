using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEngine.Perception.Randomization
{
    [Serializable]
    [DisplayName("Local")]
    public class LocalAssetSourceLocation : AssetSourceLocation
    {
        [SerializeField] public List<Object> assets;

        public override int Count => assets.Count;

        public override void Initialize<T>(Archetype<T> archetype) { }

        public override void ReleaseAssets() { }

        public override T GetAsset<T>(int index)
        {
            return (T)assets[index];
        }
    }
}
