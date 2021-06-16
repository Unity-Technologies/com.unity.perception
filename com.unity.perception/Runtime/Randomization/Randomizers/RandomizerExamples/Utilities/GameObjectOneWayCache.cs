using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Randomizers.Utilities
{
    /// <summary>
    /// Facilitates object pooling for a pre-specified collection of prefabs with the caveat that objects can be fetched
    /// from the cache but not returned. Every frame, the cache needs to be reset, which will return all objects to the pool
    /// </summary>
    public class GameObjectOneWayCache
    {
        static ProfilerMarker s_ResetAllObjectsMarker = new ProfilerMarker("ResetAllObjects");

        GameObject[] m_Prefabs;
        UniformSampler m_Sampler = new UniformSampler();
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
            m_Prefabs = prefabs;
            m_CacheParent = parent;
            m_InstanceIdToIndex = new Dictionary<int, int>();
            m_InstantiatedObjects = new List<CachedObjectData>[prefabs.Length];
            m_NumObjectsActive = new int[prefabs.Length];

            var index = 0;
            foreach (var prefab in prefabs)
            {
                if (!IsPrefab(prefab))
                {
                    prefab.transform.parent = parent;
                    prefab.SetActive(false);
                }
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
            newObject.SetActive(true);
            ++m_NumObjectsActive[index];
            m_InstantiatedObjects[index].Add(new CachedObjectData(newObject));
            return newObject;
        }

        /// <summary>
        /// Retrieves an existing instance of the given prefab from the cache if available.
        /// Otherwise, instantiate a new instance of the given prefab.
        /// </summary>
        /// <param name="index">The index of the prefab to instantiate</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public GameObject GetOrInstantiate(int index)
        {
            var prefab = m_Prefabs[index];
            return GetOrInstantiate(prefab);
        }

        /// <summary>
        /// Retrieves an existing instance of a random prefab from the cache if available.
        /// Otherwise, instantiate a new instance of the random prefab.
        /// </summary>
        /// <returns></returns>
        public GameObject GetOrInstantiateRandomPrefab()
        {
            return GetOrInstantiate(m_Prefabs[(int)(m_Sampler.Sample() * m_Prefabs.Length)]);
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
                        ResetObjectState(cachedObjectData);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the given cache object back to an inactive state
        /// </summary>
        /// <param name="gameObject">The object to make inactive</param>
        /// <exception cref="ArgumentException">Thrown when gameObject is not an active cached object.</exception>
        public void ResetObject(GameObject gameObject)
        {
            for (var i = 0; i < m_InstantiatedObjects.Length; ++i)
            {
                var instantiatedObjectList = m_InstantiatedObjects[i];
                int indexFound = -1;
                for (var j = 0; j < instantiatedObjectList.Count && indexFound < 0; j++)
                {
                    if (instantiatedObjectList[j].instance == gameObject)
                        indexFound = j;
                }

                if (indexFound >= 0)
                {
                    ResetObjectState(instantiatedObjectList[indexFound]);
                    instantiatedObjectList.RemoveAt(indexFound);
                    m_NumObjectsActive[i]--;
                    return;
                }
            }

            throw new ArgumentException("Passed GameObject is not an active object in the cache.");
        }

        private static void ResetObjectState(CachedObjectData cachedObjectData)
        {
            // Position outside the frame
            cachedObjectData.instance.transform.localPosition = new Vector3(10000, 0, 0);
            foreach (var tag in cachedObjectData.randomizerTags)
                tag.Unregister();
        }

        static bool IsPrefab(GameObject obj)
        {
            return obj.scene.rootCount == 0;
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
