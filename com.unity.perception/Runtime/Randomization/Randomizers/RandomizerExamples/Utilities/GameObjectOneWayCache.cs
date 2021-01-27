using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Facilitates object pooling for a pre-specified collection of prefabs with the caveat that objects can be fetched
/// from the cache but not returned. Every frame, the cache needs to be reset, which will return all objects to the pool
/// </summary>
class GameObjectOneWayCache
{
    static ProfilerMarker s_ResetAllObjectsMarker = new ProfilerMarker("ResetAllObjects");
    
    // Objects will reset to this origin when not being used
    Transform m_CacheParent;
    Dictionary<int, int> m_InstanceIdToIndex;
    List<GameObject>[] m_InstantiatedObjects;
    int[] m_NumObjectsActive;
    int NumObjectsInCache { get; set; }
    public int NumObjectsActive { get; private set; }

    public GameObjectOneWayCache(Transform parent, GameObject[] prefabs)
    {
        m_CacheParent = parent;
        m_InstanceIdToIndex = new Dictionary<int, int>();
        m_InstantiatedObjects = new List<GameObject>[prefabs.Length];
        m_NumObjectsActive = new int[prefabs.Length];
        
        var index = 0;
        foreach (var prefab in prefabs)
        {
            var instanceId = prefab.GetInstanceID();
            m_InstanceIdToIndex.Add(instanceId, index);
            m_InstantiatedObjects[index] = new List<GameObject>();
            m_NumObjectsActive[index] = 0;
            ++index;
        }
    }

    public GameObject GetOrInstantiate(GameObject prefab)
    {
        if (!m_InstanceIdToIndex.TryGetValue(prefab.GetInstanceID(), out var index))
        {
            throw new ArgumentException($"Prefab {prefab.name} (ID: {prefab.GetInstanceID()}) is not in cache.");
        }

        ++NumObjectsActive;
        if (m_NumObjectsActive[index] < m_InstantiatedObjects[index].Count)
        {
            var nextInCache = m_InstantiatedObjects[index][m_NumObjectsActive[index]];
            ++m_NumObjectsActive[index];
            return nextInCache;
        }
        else
        {
            ++NumObjectsInCache;
            var newObject = Object.Instantiate(prefab, m_CacheParent);
            ++m_NumObjectsActive[index];
            m_InstantiatedObjects[index].Add(newObject);
            return newObject;
        }
    }

    public void ResetAllObjects()
    {
        using (s_ResetAllObjectsMarker.Auto())
        {
            NumObjectsActive = 0;
            for (var i = 0; i < m_InstantiatedObjects.Length; ++i)
            {
                m_NumObjectsActive[i] = 0;
                foreach (var obj in m_InstantiatedObjects[i])
                {
                    // Position outside the frame
                    obj.transform.localPosition = new Vector3(10000, 0, 0);
                }
            }
        }
    }
}
