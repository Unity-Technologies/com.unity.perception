using System;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Utilities
{
    /// <summary>
    /// A reference to a scene that works at both edit and runtime.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.Internal")]
    public class SceneReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        /// <summary>
        /// An in-editor reference to a scene asset.
        /// </summary>
        /// <remarks>
        /// We convert this reference to a simple path to a scene (sceneAsset) for runtime use.
        /// </remarks>
        public SceneAsset sceneAsset;
#endif
        [SerializeField]
        // ReSharper disable once InconsistentNaming
        string m_ScenePath;

        /// <summary>
        /// The path to the scene specified by (sceneAsset).
        /// </summary>
        public string scenePath => m_ScenePath;

        /// <summary>
        /// The path to the scene referenced by (sceneAsset).
        /// </summary>
        public Scene scene => SceneManager.GetSceneByPath(scenePath);

        /// <summary>
        /// Before serialization, we populate the <see cref="scenePath" /> with information from the sceneAsset
        /// </summary>
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            m_ScenePath = sceneAsset != null ? AssetDatabase.GetAssetOrScenePath(sceneAsset) : null;
#endif
        }

        /// <summary>
        /// After serialization function
        /// </summary>
        public void OnAfterDeserialize()
        {
        }
    }
}
