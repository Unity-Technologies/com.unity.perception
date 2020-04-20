using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Defines a set of classes associated with the object and its descendants. Classes can be overwritten
/// </summary>
public class Labeling : MonoBehaviour
{
    /// <summary>
    /// The class names to associate with the GameObject.
    /// </summary>
    public List<string> classes = new List<string>();

    Entity m_Entity;
    void Awake()
    {
        m_Entity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
        World.DefaultGameObjectInjectionWorld.EntityManager.AddComponentObject(m_Entity, this);
    }

    void OnDestroy()
    {
        if (World.DefaultGameObjectInjectionWorld != null)
            World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(m_Entity);
    }
}
