using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
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
        [FormerlySerializedAs("classes")] public List<string> labels = new List<string>();

        // /// <summary>
        // /// A list for backing up the asset's manually added labels, so that if the user switches to auto labeling and back, the previously added labels can be revived
        // /// </summary>
        // public List<string> manualLabelsBackup = new List<string>();

        /// <summary>
        /// Whether this labeling component is currently using an automatic labeling scheme. When this is enabled, the asset can have only one label (the automatic one) and the user cannot add more labels.
        /// </summary>
        public bool useAutoLabeling;


        /// <summary>
        /// The specific subtype of AssetLabelingScheme that this component is using, if useAutoLabeling is enabled.
        /// </summary>
        public string autoLabelingSchemeType = string.Empty;

        /// <summary>
        /// The unique id of this labeling component instance
        /// </summary>
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

        void Reset()
        {
            labels.Clear();
            useAutoLabeling = false;
            autoLabelingSchemeType = string.Empty;
#if UNITY_EDITOR
            EditorUtility.SetDirty(gameObject);
#endif
        }


        /// <summary>
        /// Refresh ground truth generation for the labeling of the attached GameObject. This is necessary when the
        /// list of labels changes or when renderers or materials change on objects in the hierarchy.
        /// </summary>
        public void RefreshLabeling()
        {
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GroundTruthLabelSetupSystem>()
                .RefreshLabeling(m_Entity);
        }
    }
}
