using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Defines a set of classes associated with the object and its descendants. Classes can be overwritten 
/// </summary>
public class Labeling : MonoBehaviour
{
    public List<string> classes = new List<string>();

    Entity entity;

    // Start is called before the first frame update
    void Awake()
    {
        entity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
        World.DefaultGameObjectInjectionWorld.EntityManager.AddComponentObject(entity, this);
    }

    private void OnDestroy()
    {
        if (World.DefaultGameObjectInjectionWorld != null)
            World.DefaultGameObjectInjectionWorld.EntityManager.DestroyEntity(entity);
    }
}
