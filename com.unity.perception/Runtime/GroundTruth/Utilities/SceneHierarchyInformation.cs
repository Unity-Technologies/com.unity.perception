using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.GroundTruth.LabelManagement;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Data structure used to store the labeling hierarchy
    /// </summary>
    public sealed class SceneHierarchyInformation
    {
        Dictionary<uint, SceneHierarchyNode> m_InternalHierarchy;

        // number of perception cameras waiting for a callback containing this hierarchy information instance
        // used to ensure all perception cameras receive this before it is destroyed
        internal uint subscribers = 0;

        internal SceneHierarchyInformation(int capacity = 100)
        {
            m_InternalHierarchy = new Dictionary<uint, SceneHierarchyNode>(capacity);
        }

        /// <summary>
        /// Read-only representation of the hierarchy
        /// </summary>
        public IReadOnlyDictionary<uint, SceneHierarchyNode> hierarchy
            => m_InternalHierarchy;

        /// <summary>
        /// Determines whether the current instance contains an entry with the specified instance id.
        /// </summary>
        /// <param name="instanceId">Instance ID of a <see cref="Labeling"/> component.</param>
        /// <returns>True when the key exists</returns>
        public bool ContainsInstanceId(uint instanceId) => m_InternalHierarchy.ContainsKey(instanceId);

        internal void Add(uint instanceId, SceneHierarchyNode node)
        {
            m_InternalHierarchy[instanceId] = node;
        }

        /// <summary>
        /// Retrieves an enumerable of all nodes present in the scene hierarchy.
        /// </summary>
        /// <returns>A list of <see cref="SceneHierarchyNode"/></returns>
        public List<SceneHierarchyNode> GetAllNodes() => m_InternalHierarchy.Values.ToList();

        /// <summary>
        /// Gets the <see cref="SceneHierarchyNode" /> representation of a node in the scene hierarchy given the
        /// instance id of a labeled object.
        /// </summary>
        /// <param name="instanceId">The instance id of a labeled object.</param>
        /// <param name="node">A <see cref="SceneHierarchyNode"/> for the instance id instanceId</param>
        /// <returns>Whether a node exists for the given instance id</returns>
        public bool TryGetNodeForInstanceId(uint instanceId, out SceneHierarchyNode node)
        {
            if (!m_InternalHierarchy.ContainsKey(instanceId))
            {
                node = null;
                return false;
            }

            var entry = m_InternalHierarchy[instanceId];
            node = new SceneHierarchyNode(instanceId, entry.labels, entry.childrenInstanceIds, entry.parentInstanceId);
            return true;
        }

        /// <summary>
        /// The number of nodes inside the scene hierarchy.
        /// </summary>
        /// <returns>The number of nodes</returns>
        public int GetNodeCount() => m_InternalHierarchy.Count;

        /// <summary>
        /// Creates a copy of the SceneHierarchy information but with only the objects whose instance ids
        /// is included in idsToKeep
        /// </summary>
        /// <param name="idsToKeep">Ids to Keep</param>
        /// <returns>new SceneHierarchyInformation</returns>
        internal SceneHierarchyInformation FilteredClone(HashSet<uint> idsToKeep)
        {
            // create an empty hashmap the same size as the length of the ids we want to keep
            var hierarchyClone = new SceneHierarchyInformation(idsToKeep.Count);

            // if we don't include an object with an instance id N, we have to:
            //    1. not include it as a key in the hierarchy mapping
            //    2. remove N as a child of its parent
            // so we keep track of which parent -> child relationships we want to remove
            var parentChildrenRelationshipToRemove = new List<(uint parent, uint child)>();
            foreach (var kvp in m_InternalHierarchy)
            {
                // if we want to keep the object, add it to the hierarchy map
                if (idsToKeep.Contains(kvp.Key))
                {
                    hierarchyClone.Add(kvp.Key, kvp.Value);
                }
                // if not, if it has a parent, queue it to be removed from its parent
                else if (kvp.Value.parentInstanceId.HasValue)
                {
                    parentChildrenRelationshipToRemove.Add((kvp.Value.parentInstanceId.Value, kvp.Key));
                }
            }

            // for all objects that need to be removed from their parent, remove them
            foreach (var(parent, child) in parentChildrenRelationshipToRemove)
            {
                // check if the hierarchy contains the parent (as the parent might've been removed too)
                // check if the parent contains the child. if so, remove the child
                if (hierarchyClone.hierarchy.ContainsKey(parent) && hierarchyClone.hierarchy[parent].childrenInstanceIds.Contains(child))
                    hierarchyClone.m_InternalHierarchy[parent].childrenInstanceIds.Remove(child);
            }

            // return the clone
            return hierarchyClone;
        }
    }
}
