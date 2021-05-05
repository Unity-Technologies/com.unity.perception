using System;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization
{
    /// <summary>
    /// AssetSources are used to load assets from a generically within a <see cref="Randomizers.Randomizer"/>
    /// </summary>
    /// <typeparam name="T">The type of asset to load</typeparam>
    [Serializable]
    public sealed class AssetSource<T> where T : Object
    {
        [SerializeReference] ArchetypeBase m_ArchetypeBase;

        /// <summary>
        /// The location to load assets from
        /// </summary>
        [SerializeReference] public AssetSourceLocation assetSourceLocation = new LocalAssetSourceLocation();

        bool m_Initialized;
        UniformSampler m_Sampler = new UniformSampler();

        /// <summary>
        /// The archetype used to preprocess assets from this source
        /// </summary>
        public Archetype<T> archetype
        {
            get => (Archetype<T>)m_ArchetypeBase;
            set => m_ArchetypeBase = value;
        }

        /// <summary>
        /// The number of assets available within this asset source
        /// </summary>
        public int Count => assetSourceLocation.Count;

        /// <summary>
        /// Execute setup steps for this AssetSource
        /// </summary>
        public void Initialize()
        {
            assetSourceLocation.Initialize(archetype);
            m_Initialized = true;
        }

        /// <summary>
        /// Returns the unprocessed asset loaded from the provided index
        /// </summary>
        /// <param name="index">The index of the asset to load</param>
        /// <returns>The asset loaded at the provided index</returns>
        public T LoadRawAsset(int index)
        {
            CheckIfInitialized();
            return assetSourceLocation.LoadAsset<T>(index);
        }

        /// <summary>
        /// Returns all unprocessed assets that can be loaded from this AssetSource
        /// </summary>
        /// <returns>All assets that can be loaded from this AssetSource</returns>
        public T[] LoadAllRawAssets()
        {
            var array = new T[Count];
            for (var i = 0; i < Count; i++)
            {
                array[i] = LoadRawAsset(i);
                archetype.Preprocess(array[i]);
            }
            return array;
        }

        /// <summary>
        /// Creates an instance of the asset loaded from the provided index and preprocesses it using the archetype
        /// assigned to this asset source
        /// </summary>
        /// <param name="index">The index of the asset to load</param>
        /// <returns>The instantiated instance</returns>
        public T CreateProcessedInstance(int index)
        {
            CheckIfInitialized();
            return CreateProcessedInstance(LoadRawAsset(index));
        }

        /// <summary>
        /// Instantiates, preprocesses, and returns all assets that can be loaded from this asset source
        /// </summary>
        /// <returns>Instantiated instances from every loadable asset</returns>
        public T[] CreateProcessedInstances()
        {
            var array = new T[Count];
            for (var i = 0; i < Count; i++)
                array[i] = CreateProcessedInstance(i);
            return array;
        }

        /// <summary>
        /// Returns a uniformly random sampled asset from this AssetSource
        /// </summary>
        /// <returns>The randomly sampled asset</returns>
        public T SampleAsset()
        {
            CheckIfInitialized();
            return assetSourceLocation.LoadAsset<T>((int)(m_Sampler.Sample() * Count));
        }

        /// <summary>
        /// Instantiates and preprocesses a uniformly random sampled asset from this AssetSource
        /// </summary>
        /// <returns>The generated random instance</returns>
        public T SampleInstance()
        {
            CheckIfInitialized();
            return CreateProcessedInstance(SampleAsset());
        }

        /// <summary>
        /// Unloads all assets that have been loaded from this AssetSource
        /// </summary>
        public void ReleaseAssets()
        {
            CheckIfInitialized();
            assetSourceLocation.ReleaseAssets();
        }

        void CheckIfInitialized()
        {
            if (!m_Initialized)
                Initialize();
        }

        T CreateProcessedInstance(T asset)
        {
            var instance = Object.Instantiate(asset);
            if (archetype != null)
                archetype.Preprocess(instance);
            return instance;
        }
    }
}
