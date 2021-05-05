using System;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization
{
    [Serializable]
    public sealed class AssetSource<T> where T : Object
    {
        bool m_Initialized;
        UniformSampler m_Sampler = new UniformSampler();

        [SerializeReference] ArchetypeBase m_ArchetypeBase;
        [SerializeReference] public AssetSourceLocation assetSourceLocation = new LocalAssetSourceLocation();

        public Archetype<T> archetype
        {
            get => (Archetype<T>)m_ArchetypeBase;
            set => m_ArchetypeBase = value;
        }

        public int Count => assetSourceLocation.Count;

        public void Initialize()
        {
            assetSourceLocation.Initialize(archetype);
            m_Initialized = true;
        }

        public T GetAsset(int index)
        {
            CheckIfInitialized();
            return assetSourceLocation.GetAsset<T>(index);
        }

        public T[] GetAssets()
        {
            var array = new T[Count];
            for (var i = 0; i < Count; i++)
                array[i] = GetAsset(i);
            return array;
        }

        public T GetInstance(int index)
        {
            CheckIfInitialized();
            return CreateInstance(GetAsset(index));
        }

        public T[] GetInstances()
        {
            var array = new T[Count];
            for (var i = 0; i < Count; i++)
                array[i] = GetInstance(i);
            return array;
        }

        public T SampleAsset()
        {
            CheckIfInitialized();
            return assetSourceLocation.GetAsset<T>((int)(m_Sampler.Sample() * Count));
        }

        public T SampleInstance()
        {
            CheckIfInitialized();
            return CreateInstance(SampleAsset());
        }

        public void ReleaseAssets()
        {
            CheckIfInitialized();
            assetSourceLocation.ReleaseAssets();
        }

        void CheckIfInitialized()
        {
            if (!m_Initialized)
                throw new InvalidOperationException(
                    "Initialize() must be called on this AssetSource before executing this operation");
        }

        T CreateInstance(T asset)
        {
            var instance = Object.Instantiate(asset);
            if (archetype != null)
                archetype.Preprocess(instance);
            return instance;
        }
    }
}
