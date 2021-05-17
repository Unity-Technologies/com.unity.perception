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
        public int count => assetSourceLocation.count;

        /// <summary>
        /// Execute setup steps for this AssetSource. It is often unnecessary to call this API directly since all other
        /// relevant APIs in this class will Initialize() this AssetSource if it hasn't been already.
        /// </summary>
        public void Initialize()
        {
            if (!m_Initialized)
            {
                assetSourceLocation.Initialize(archetype);
                m_Initialized = true;
            }
        }

        /// <summary>
        /// Returns the unprocessed asset loaded from the provided index
        /// </summary>
        /// <param name="index">The index of the asset to load</param>
        /// <returns>The asset loaded at the provided index</returns>
        public T LoadRawAsset(int index)
        {
            CheckIfInitialized();
            if (count == 0)
                return null;
            return assetSourceLocation.LoadAsset<T>(index);
        }

        /// <summary>
        /// Returns all unprocessed assets that can be loaded from this AssetSource
        /// </summary>
        /// <returns>All assets that can be loaded from this AssetSource</returns>
        public T[] LoadAllRawAssets()
        {
            CheckIfInitialized();
            var array = new T[count];
            for (var i = 0; i < count; i++)
                array[i] = LoadRawAsset(i);
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
            CheckIfInitialized();
            var array = new T[count];
            for (var i = 0; i < count; i++)
                array[i] = CreateProcessedInstance(i);
            return array;
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
            if (asset == null)
                return null;

            var instance = Object.Instantiate(asset);
            if (archetype != null)
                archetype.Preprocess(instance);
            return instance;
        }
    }
}
