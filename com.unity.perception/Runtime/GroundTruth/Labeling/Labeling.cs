using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Defines a set of labels associated with the object and its descendants. A Labeling component will override any Labeling components on the object's ancestors.
    /// </summary>
    public class Labeling : MonoBehaviour
    {
        /// <summary>
        /// The label names to associate with the GameObject. Modifications to this list after the Update() step of the frame the object is created in are 
        /// not guaranteed to be reflected by labelers.
        /// </summary>
        [FormerlySerializedAs("classes")]
        public List<string> labels = new List<string>();

        public uint instanceId { get; private set; }

        Entity m_Entity;

        internal void SetInstanceId(uint instanceId)
        {
            this.instanceId = instanceId;
        }
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
}
