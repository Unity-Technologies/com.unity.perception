using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEngine.Perception.Randomization.Randomizers
{
    /// <summary>
    /// A randomizer which dynamically loads in scenes during a scenario run every few iterations.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.Internal")]
    [AddRandomizerMenu("Perception/Scene Randomizer")]
    public class SceneRandomizer : Randomizer
    {
        /// <summary>
        /// The number of iterations to spend in a scene before switching to a new one.
        /// </summary>
        [UsedImplicitly]
        [Tooltip("The number of iterations to spend in a scene before switching to a new one.")]
        public int iterationsPerScene = 20;
        /// <summary>
        /// When switching to a scene, the new scene will be picked from the options below.
        /// </summary>
        /// <remarks>We ensure that no two consecutive scenes are the same.</remarks>
        [FormerlySerializedAs("sceneReferenceParameter")]
        [UsedImplicitly]
        [Tooltip("When switching to a scene, the new scene will be picked from the options below. We ensure that no two consecutive scenes are the same.")]
        public CategoricalParameter<SceneReference> includedScenes = new CategoricalParameter<SceneReference>();

        SceneReference m_CurrentlyLoadedScene = null;
        List<AsyncOperation> m_OperationsWaitingOn = new List<AsyncOperation>();
        int m_IterationLastRandomized = -1;

        bool isSceneLoading
        {
            get
            {
                for (var i = m_OperationsWaitingOn.Count - 1; i >= 0; i--)
                {
                    if (!m_OperationsWaitingOn[i].isDone)
                        return true;

                    m_OperationsWaitingOn.RemoveAt(i);
                }

                return false;
            }
        }

        ///<inheritdoc />
        protected override void OnIterationStart()
        {
            if (iterationsPerScene <= 0)
                Debug.LogError($"[Scene Randomizer] Iterations per scene must be greater than zero.");

            // If things are still loading, delay.
            if (isSceneLoading)
            {
                scenario.DelayIteration();
                return;
            }

            if (
                // Make sure we are not trying to randomize an iteration we already queued for randomization
                (scenario.currentIteration != m_IterationLastRandomized) &&
                // Randomize every nth iteration (starting from zero)
                (scenario.currentIteration % iterationsPerScene == 0) &&
                // We have a scene to sample and load
                (includedScenes.Count > 0)
            )
            {
                m_IterationLastRandomized = scenario.currentIteration;
                RandomizeScene();
            }

            // Delay as things might be loading from the RandomizeScene call above.
            if (isSceneLoading)
                scenario.DelayIteration();
        }

        /// <summary>
        /// Queues two operations: (1) Unloading the <see cref="m_CurrentlyLoadedScene" /> and (2) Loading a new scene
        /// distinct from the last/current scene.
        /// </summary>
        void RandomizeScene()
        {
            SceneReference nextScene;
            if (includedScenes.Count <= 1 && m_CurrentlyLoadedScene != null)
                return;

            m_OperationsWaitingOn.Clear();

            // Unload currently loaded scene
            if (m_CurrentlyLoadedScene != null)
            {
                var unloadSceneAsync = SceneManager.UnloadSceneAsync(m_CurrentlyLoadedScene.scenePath);
                if (unloadSceneAsync != null)
                    m_OperationsWaitingOn.Add(unloadSceneAsync);
            }

            // Get new scene (different from the current scene, skip invalid scenes)
            do
            {
                nextScene = includedScenes.Sample();
            }
            while (nextScene == m_CurrentlyLoadedScene || nextScene?.scenePath == null);
            m_CurrentlyLoadedScene = nextScene;

            // Load new scene
            var loadSceneAsync = SceneManager.LoadSceneAsync(nextScene.scenePath, LoadSceneMode.Additive);
            if (loadSceneAsync != null)
                m_OperationsWaitingOn.Add(loadSceneAsync);
        }
    }
}
