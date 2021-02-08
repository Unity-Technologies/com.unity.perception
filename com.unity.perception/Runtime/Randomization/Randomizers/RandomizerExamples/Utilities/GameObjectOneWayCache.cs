using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Randomizers.Utilities
{
    /// <summary>
    /// Facilitates object pooling for a pre-specified collection of prefabs with the caveat that objects can be fetched
    /// from the cache but not returned. Every frame, the cache needs to be reset, which will return all objects to the pool
    /// </summary>
    public class GameObjectOneWayCache
    {
        static ProfilerMarker s_ResetAllObjectsMarker = new ProfilerMarker("ResetAllObjects");

        Transform m_CacheParent;
        Dictionary<int, int> m_InstanceIdToIndex;
        List<CachedObjectData>[] m_InstantiatedObjects;
        int[] m_NumObjectsActive;
        int NumObjectsInCache { get; set; }

        /// <summary>
        /// The number of active cache objects in the scene
        /// </summary>
        public int NumObjectsActive { get; private set; }

        /// <summary>
        /// Creates a new GameObjectOneWayCache
        /// </summary>
        /// <param name="parent">The parent object all cached instances will be parented under</param>
        /// <param name="prefabs">The prefabs to cache</param>
        public GameObjectOneWayCache(Transform parent, GameObject[] prefabs)
        {
            m_CacheParent = parent;
            m_InstanceIdToIndex = new Dictionary<int, int>();
            m_InstantiatedObjects = new List<CachedObjectData>[prefabs.Length];
            m_NumObjectsActive = new int[prefabs.Length];

            var index = 0;
            foreach (var prefab in prefabs)
            {
                var instanceId = prefab.GetInstanceID();
                m_InstanceIdToIndex.Add(instanceId, index);
                m_InstantiatedObjects[index] = new List<CachedObjectData>();
                m_NumObjectsActive[index] = 0;
                ++index;
            }
        }

        /// <summary>
        /// Retrieves an existing instance of the given prefab from the cache if available.
        /// Otherwise, instantiate a new instance of the given prefab.
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public GameObject GetOrInstantiate(GameObject prefab)
        {
            if (!m_InstanceIdToIndex.TryGetValue(prefab.GetInstanceID(), out var index))
                throw new ArgumentException($"Prefab {prefab.name} (ID: {prefab.GetInstanceID()}) is not in cache.");

            ++NumObjectsActive;
            if (m_NumObjectsActive[index] < m_InstantiatedObjects[index].Count)
            {
                var nextInCache = m_InstantiatedObjects[index][m_NumObjectsActive[index]];
                ++m_NumObjectsActive[index];
                foreach (var tag in nextInCache.randomizerTags)
                    tag.Register();
                return nextInCache.instance;
            }

            ++NumObjectsInCache;
            var newObject = Object.Instantiate(prefab, m_CacheParent);
            ++m_NumObjectsActive[index];
            m_InstantiatedObjects[index].Add(new CachedObjectData(newObject));
            return newObject;
        }

        /// <summary>
        /// Return all active cache objects back to an inactive state
        /// </summary>
        public void ResetAllObjects()
        {
            using (s_ResetAllObjectsMarker.Auto())
            {
                NumObjectsActive = 0;
                for (var i = 0; i < m_InstantiatedObjects.Length; ++i)
                {
                    m_NumObjectsActive[i] = 0;
                    foreach (var cachedObjectData in m_InstantiatedObjects[i])
                    {
                        // Position outside the frame
                        cachedObjectData.instance.transform.localPosition = new Vector3(10000, 0, 0);
                        foreach (var tag in cachedObjectData.randomizerTags)
                            tag.Unregister();
                    }
                }
            }
        }

        struct CachedObjectData
        {
            public GameObject instance;
            public RandomizerTag[] randomizerTags;

            public CachedObjectData(GameObject instance)
            {
                this.instance = instance;
                randomizerTags = instance.GetComponents<RandomizerTag>();
            }
        }
    }
}
