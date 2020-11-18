using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Defines a set of labels associated with the object and its descendants. A Labeling component will override any Labeling components on the object's ancestors.
    /// </summary>
    public class Labeling : MonoBehaviour
    {
        public static readonly string[] NameSeparators = {".", "-", "_"};
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
        public bool useAutoLabeling = false;


        /// <summary>
        /// The specific subtype of AssetLabelingScheme that this component is using, if useAutoLabeling is enabled.
        /// </summary>
        public string autoLabelingSchemeType = String.Empty;

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
            autoLabelingSchemeType = String.Empty;
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

        public static string GetAssetOrPrefabPath(Object gObj)
        {
            string assetPath = AssetDatabase.GetAssetPath(gObj);

            if (assetPath == String.Empty)
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

    public abstract class AssetLabelingScheme
    {
        protected Object m_TargetAsset;

        //public abstract string Title { get; protected set; }
        public abstract string Description { get; protected set; }
        public abstract string GenerateLabel(Object asset);
        public abstract bool IsCompatibleWithAsset(Object asset);
    }

    public class AssetNameLabelingScheme : AssetLabelingScheme
    {
        // public override string Title
        // {
        //     get => "Use Asset Name";
        //     protected set { }
        // }

        public override string Description
        {
            get => "Use asset name";
            protected set { }
        }

        public override bool IsCompatibleWithAsset(Object asset)
        {
            return true;
        }

        public override string GenerateLabel(Object asset)
        {
            return asset.name;
        }
    }

    public class AssetFileNameLabelingScheme : AssetLabelingScheme
    {
        // public override string Title
        // {
        //     get => "Use File Name with Extension";
        //     protected set { }
        // }

        public override string Description
        {
            //get => "Uses the full file name of the asset, including the extension.";
            get => "Use file name with extension";
            protected set { }
        }

        public override bool IsCompatibleWithAsset(Object asset)
        {
            string assetPath = Labeling.GetAssetOrPrefabPath(asset);
            return assetPath != null;
        }

        public override string GenerateLabel(Object asset)
        {
            string assetPath = Labeling.GetAssetOrPrefabPath(asset);
            var stringList = assetPath?.Split(Labeling.PathSeparators, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
            return stringList?.Last();
        }
    }

    public class CurrentOrParentsFolderNameLabelingScheme : AssetLabelingScheme
    {
        // public override string Title
        // {
        //     get => "Use folder name of asset or its ancestors";
        //     protected set { }
        // }

        public override string Description
        {
            get => "Use folder name of asset or its ancestors";
            protected set { }
        }

        public override bool IsCompatibleWithAsset(Object asset)
        {
            string assetPath = Labeling.GetAssetOrPrefabPath(asset);
            return assetPath != null;
        }

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
