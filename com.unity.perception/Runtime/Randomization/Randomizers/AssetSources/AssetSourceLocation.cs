using UnityEngine;

namespace UnityEngine.Perception.Randomization
{
    public abstract class AssetSourceLocation
    {
        public abstract int Count { get; }

        public abstract void Initialize<T>(Archetype<T> archetype) where T : Object;

        public abstract void ReleaseAssets();

        public abstract T GetAsset<T>(int index) where T : Object;
    }
}
