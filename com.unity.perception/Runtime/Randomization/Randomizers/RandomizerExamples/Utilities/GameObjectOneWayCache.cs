using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Perception.Randomization.Randomizers;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization.Utilities
{
    /// <summary>
    /// Facilitates object pooling for a pre-specified collection of prefabs with the caveat that objects can be fetched
    /// from the cache but not returned. Every frame, the cache needs to be reset, which will return all objects to the pool
    /// </summary>
    [MovedFrom("UnityEngine.Perception.Randomization.Randomizers.Utilities")]
    public class GameObjectOneWayCache
    {
        static ProfilerMarker s_ResetAllObjectsMarker = new ProfilerMarker("ResetAllObjects");

        GameObject[] m_GameObjects;
        UniformSampler m_Sampler = new UniformSampler();
        Transform m_CacheParent;
        Dictionary<int, int> m_InstanceIdToIndex;
        List<CachedObjectData>[] m_InstantiatedObjects;
        int[] m_NumObjectsActive;
        int numObjectsInCache { get; set; }

        /// <summary>
        /// The number of active cache objects in the scene
        /// </summary>
        public int ActiveCachedObjectsCount { get; private set; }

        /// <summary>
        /// Creates a new GameObjectOneWayCache
        /// </summary>
        /// <param name="parent">The parent object all cached instances will be parented under</param>
        /// <param name="gameObjects">The gameObjects to cache</param>
        /// <param name="randomizer">Randomizer that invoked the method</param>
        public GameObjectOneWayCache(Transform parent, GameObject[] gameObjects, Randomizer randomizer)
        {
            if (gameObjects.Length == 0)
                throw new ArgumentException(
                    "A non-empty array of GameObjects is required to initialize this GameObject cache");

            m_GameObjects = gameObjects;
            m_CacheParent = parent;
            m_InstanceIdToIndex = new Dictionary<int, int>();
            m_InstantiatedObjects = new List<CachedObjectData>[gameObjects.Length];
            m_NumObjectsActive = new int[gameObjects.Length];

            var index = 0;
            foreach (var obj in gameObjects)
            {
                if (!IsPrefab(obj))
                {
                    obj.transform.parent = parent;
                    obj.SetActive(false);
                }
                var instanceId = obj.GetInstanceID();
                if (m_InstanceIdToIndex.ContainsKey(instanceId))
                {
                    Debug.LogException(new Exception("Duplicated objects were added in the categories, the duplicated object will be ignored\n" +
                        "Randomizer: " + randomizer.GetType().Name +
                        "\nDuplicate objects: " + obj.name + "\n"));
                    continue;
                }
                m_InstanceIdToIndex.Add(instanceId, index);
                m_InstantiatedObjects[index] = new List<CachedObjectData>();
                m_NumObjectsActive[index] = 0;
                ++index;
            }
        }

        /// <summary>
        /// Retrieves an existing instance of the given gameObject from the cache if available.
        /// Otherwise, instantiate a new instance of the given gameObject.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public GameObject GetOrInstantiate(GameObject gameObject)
        {
            if (!m_InstanceIdToIndex.TryGetValue(gameObject.GetInstanceID(), out var index))
                throw new ArgumentException($"GameObject {gameObject.name} (ID: {gameObject.GetInstanceID()}) is not in cache.");

            ++ActiveCachedObjectsCount;
            if (m_NumObjectsActive[index] < m_InstantiatedObjects[index].Count)
            {
                var nextInCache = m_InstantiatedObjects[index][m_NumObjectsActive[index]];
                ++m_NumObjectsActive[index];
                foreach (var tag in nextInCache.randomizerTags)
                    tag.Register();
                return nextInCache.instance;
            }

            ++numObjectsInCache;
            var newObject = Object.Instantiate(gameObject, m_CacheParent);
            newObject.SetActive(true);
            ++m_NumObjectsActive[index];
            m_InstantiatedObjects[index].Add(new CachedObjectData(newObject));
            return newObject;
        }

        /// <summary>
        /// Retrieves an existing instance of the given gameObject from the cache if available.
        /// Otherwise, instantiate a new instance of the given gameObject.
        /// </summary>
        /// <param name="index">The index of the gameObject to instantiate</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public GameObject GetOrInstantiate(int index)
        {
            var gameObject = m_GameObjects[index];
            return GetOrInstantiate(gameObject);
        }

        /// <summary>
        /// Retrieves an existing instance of a random gameObject from the cache if available.
        /// Otherwise, instantiate a new instance of the random gameObject.
        /// </summary>
        /// <returns>A random cached GameObject</returns>
        public GameObject GetOrInstantiateRandomCachedObject()
        {
            return GetOrInstantiate(m_GameObjects[(int)(m_Sampler.Sample() * m_GameObjects.Length)]);
        }

        /// <summary>
        /// Return all active cache objects back to an inactive state
        /// </summary>
        public void ResetAllObjects()
        {
            using (s_ResetAllObjectsMarker.Auto())
            {
                ActiveCachedObjectsCount = 0;
                for (var i = 0; i < m_InstantiatedObjects.Length; ++i)
                {
                    m_NumObjectsActive[i] = 0;
                    if (m_InstantiatedObjects[i] != null)
                    {
                        foreach (var cachedObjectData in m_InstantiatedObjects[i])
                        {
                            ResetObjectState(cachedObjectData);
                        }
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
