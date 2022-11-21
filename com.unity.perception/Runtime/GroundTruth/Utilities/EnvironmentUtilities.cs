using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.SceneManagement;

namespace UnityEngine.Perception.GroundTruth
{
    static class EnvironmentUtilities
    {
        /// <summary>
        /// Wrapper for <see cref="ParseSceneHierarchy" /> that starts the DFS with all root GameObjects in all scenes.
        /// Further, accepts a list of <see cref="RenderedObjectInfo" /> instead of instance ids for convenience.
        /// </summary>
        internal static SceneHierarchyInformation ParseHierarchyFromAllScenes(
            NativeList<RenderedObjectInfo> objectInfosToInclude
        )
        {
            var onscreenInstanceIds = new HashSet<uint>(objectInfosToInclude.Select(info => info.instanceId));
            return ParseHierarchyFromAllScenes(onscreenInstanceIds);
        }

        /// <summary>
        /// Wrapper for <see cref="ParseSceneHierarchy" /> that starts the DFS with all root GameObjects in all scenes.
        /// </summary>
        internal static SceneHierarchyInformation ParseHierarchyFromAllScenes(
            HashSet<uint> instanceIdsToInclude = null
        )
        {
            var allRootGameObjects = new List<GameObject>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                allRootGameObjects.AddRange(scene.GetRootGameObjects());
            }

            return ParseSceneHierarchy(
                allRootGameObjects,
                instanceIdsToInclude
            );
        }

        /// <summary>
        /// Parses and saves the current scene hierarchy for quick-referencing in <see cref="SceneHierarchyInformation" />
        /// </summary>
        /// <param name="rootGameObjects">A list of root GameObjects from where the DFS begins</param>
        /// <param name="instanceIdsToInclude">If not null, only GameObjects whose instance id exists in this will be processed.</param>
        /// <returns></returns>
        internal static SceneHierarchyInformation ParseSceneHierarchy(
            IEnumerable<GameObject> rootGameObjects,
            HashSet<uint> instanceIdsToInclude = null
        )
        {
            /*
             * A simple DFS that iterates over all nodes while keeping track of each node's parent.
             * Based on the parent and the current node, updates the dictionary which keeps track of
             *  | key: node, value: (parent, list of children) relationships. |
             */

            var nodeHierarchyMap = new SceneHierarchyInformation(5000);
            var queue = new Queue<(Transform node, uint? parentInstanceId)>();
            rootGameObjects.ToList().ForEach(x => queue.Enqueue((x.transform, null)));

            while (queue.Count > 0)
            {
                var(currentNode, parentInstanceId) = queue.Dequeue();
                // shouldn't happen but anyway
                if (currentNode == null)
                    continue;

                // exclude disabled game objects in the hierarchy, do not process children
                // Note: activeInHierarchy used instead of activeSelf as activeInHierarchy returns false
                // even when the object is active but one of its parents is inactive
                if (!currentNode.gameObject.activeInHierarchy)
                    continue;

                // if the current transform does not have a labeling component, still navigate to its children
                if (!currentNode.TryGetComponent<Labeling>(out var labelingComponent))
                {
                    for (var i = 0; i < currentNode.childCount; i++)
                    {
                        queue.Enqueue((currentNode.GetChild(i), parentInstanceId));
                    }
                    continue;
                }


                var instanceId = labelingComponent.instanceId;
                var labels = labelingComponent.labels;
                // if the current transform has a labeling component and the user wants to only include visible ids,
                // check whether the game object is visible
                if (instanceIdsToInclude != null && !instanceIdsToInclude.Contains(instanceId))
                    continue;

                if (nodeHierarchyMap.ContainsInstanceId(instanceId))
                {
                    Debug.LogError("This should never happen.");
                    // TODO: Take this out or check for this and throw exception
                }
                else
                {
                    // add entry to hierarchy connecting a node's instance id to it's parents instance id
                    nodeHierarchyMap.Add(instanceId, new SceneHierarchyNode(instanceId, labels, new HashSet<uint>(), parentInstanceId));
                }

                // if it has a parent, add the current node as the parents child
                if (parentInstanceId.HasValue)
                    nodeHierarchyMap.hierarchy[parentInstanceId.Value].childrenInstanceIds.Add(instanceId);

                // process children of current node with the parent set to the current node
                for (var i = 0; i < currentNode.childCount; i++)
                {
                    queue.Enqueue((currentNode.GetChild(i), instanceId));
                }
            }

            return nodeHierarchyMap;
        }
    }
}
