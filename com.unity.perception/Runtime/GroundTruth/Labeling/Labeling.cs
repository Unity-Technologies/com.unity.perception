using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Defines a set of labels associated with the object and its descendants. A Labeling component will override any Labeling components on the object's ancestors.
    /// </summary>
    public class Labeling : MonoBehaviour
    {
        /// <summary>
        /// List of separator characters used for parsing asset names for auto labeling or label suggestion purposes
        /// </summary>
        public static readonly string[] NameSeparators = {".", "-", "_"};
        /// <summary>
        /// List of separator characters used for parsing asset paths for auto labeling or label suggestion purposes
        /// </summary>
        public static readonly string[] PathSeparators = {"/"};

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

        /// <summary>
        /// Get the path of the given asset in the project, or get the path of the given Scene GameObject's source prefab if any
        /// </summary>
        /// <param name="gObj"></param>
        /// <returns></returns>
        public static string GetAssetOrPrefabPath(Object gObj)
        {
            string assetPath = AssetDatabase.GetAssetPath(gObj);

            if (assetPath == string.Empty)
            {
                //this indicates that gObj is a scene object and not a prefab directly selected from the Project tab
                var prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(gObj);
                if (prefabObject)
                {
                    assetPath = AssetDatabase.GetAssetPath(prefabObject);
                }
            }

            return assetPath;
        }
    }

    /// <summary>
    /// A labeling scheme based on which an automatic label can be produced for a given asset. E.g. based on asset name, asset path, etc.
    /// </summary>
    public abstract class AssetLabelingScheme
    {
        /// <summary>
        /// The description of how this scheme generates labels. Used in the dropdown menu in the UI.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Generate a label for the given asset
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public abstract string GenerateLabel(Object asset);
    }

    /// <summary>
    /// Asset labeling scheme that outputs the given asset's name as its automatic label
    /// </summary>
    public class AssetNameLabelingScheme : AssetLabelingScheme
    {
        ///<inheritdoc/>
        public override string Description => "Use asset name";

        ///<inheritdoc/>
        public override string GenerateLabel(Object asset)
        {
            return asset.name;
        }
    }


    /// <summary>
    /// Asset labeling scheme that outputs the given asset's file name, including extension, as its automatic label
    /// </summary>
    public class AssetFileNameLabelingScheme : AssetLabelingScheme
    {
        ///<inheritdoc/>
        public override string Description => "Use file name with extension";

        ///<inheritdoc/>
        public override string GenerateLabel(Object asset)
        {
            string assetPath = Labeling.GetAssetOrPrefabPath(asset);
            var stringList = assetPath?.Split(Labeling.PathSeparators, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
            return stringList?.Last();
        }
    }


    /// <summary>
    /// Asset labeling scheme that outputs the given asset's folder name as its automatic label
    /// </summary>
    public class CurrentOrParentsFolderNameLabelingScheme : AssetLabelingScheme
    {
        ///<inheritdoc/>
        public override string Description => "Use the asset's folder name";

        ///<inheritdoc/>
        public override string GenerateLabel(Object asset)
        {
            string assetPath = Labeling.GetAssetOrPrefabPath(asset);
            var stringList = assetPath?.Split(Labeling.PathSeparators, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            //if stringList is not null, it always has at least two members, the file's name and the parent folder
            return stringList?[stringList.Count-2];
        }
    }
}
