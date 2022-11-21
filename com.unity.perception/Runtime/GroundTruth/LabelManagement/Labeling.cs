using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEngine.Perception.GroundTruth.LabelManagement
{
    /// <summary>
    /// Defines a set of labels associated with the object and its descendants. A Labeling component will override any Labeling components on the object's ancestors.
    /// </summary>
    [AddComponentMenu("Perception/Labeling/Labeling")]
    [DisallowMultipleComponent]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class Labeling : MonoBehaviour
    {
        static LabelManager labelManager => LabelManager.singleton;

        /// <summary>
        /// The label names to associate with the GameObject. Modifications to this list after the Update() step of the frame the object is created in are
        /// not guaranteed to be reflected by labelers.
        /// </summary>
        [FormerlySerializedAs("classes")]
        public List<string> labels = new List<string>();

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

        void Awake()
        {
            instanceId = LabelManager.singleton.GetNextInstanceId();
        }

        void OnDestroy()
        {
            labelManager.Unregister(this);
        }

        void OnEnable()
        {
            RefreshLabeling();
        }

        void OnDisable()
        {
            RefreshLabeling();
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
            labelManager.RefreshLabeling(this);
        }
    }
}
