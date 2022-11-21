using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// A <see cref="SceneHierarchyNode" /> defines the parent-child relationship for a single labeled GameObject.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public sealed class SceneHierarchyNode : IMessageProducer
    {
        /// <summary>
        /// <inheritdoc cref="SceneHierarchyNode"/>
        /// </summary>
        /// <param name="nodeInstanceId">The instance id of a labeled GameObject</param>
        /// <param name="childrenInstanceIds">A list of instance ids of labeled children GameObject</param>
        /// <param name="parentInstanceId">The instance id of the labeled parent GameObject (if it exists)</param>
        /// <param name="labels">The labels on the <see cref="Labeling" /> component of the labeled GameObject.</param>
        internal SceneHierarchyNode(
            uint nodeInstanceId,  List<string> labels,
            HashSet<uint> childrenInstanceIds, uint? parentInstanceId
        )
        {
            this.childrenInstanceIds = childrenInstanceIds;
            this.nodeInstanceId = nodeInstanceId;
            this.parentInstanceId = parentInstanceId;
            this.labels = labels;
        }

        /// <summary>
        /// The instance id of a labeled GameObject
        /// </summary>
        public uint nodeInstanceId { get; }
        /// <summary>
        /// A list of instance ids of labeled children GameObject
        /// </summary>
        public uint? parentInstanceId { get; }
        /// <summary>
        /// The instance id of the labeled parent GameObject (if it exists)
        /// </summary>
        public HashSet<uint> childrenInstanceIds { get; }
        /// <summary>
        /// The labels on the <see cref="Labeling" /> component of the labeled GameObject.
        /// </summary>
        public List<string> labels { get; }

        /// <summary>
        /// Safely gets the parent of this node (if it exists).
        /// </summary>
        /// <param name="parentsInstanceId">The instance id of the parent if it exists. If the parent does not exist,
        /// this value will be set to uint <see cref="uint.MaxValue"/></param>
        /// <returns>A boolean indicating whether a parent exists for the current node</returns>
        public bool TryGetParentInstanceId(out uint parentsInstanceId)
        {
            if (parentInstanceId.HasValue)
            {
                parentsInstanceId = parentInstanceId.Value;
                return true;
            }

            parentsInstanceId = uint.MaxValue;
            return false;
        }

        /// <summary>
        /// Generates result message
        /// </summary>
        /// <param name="builder"></param>
        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddInt("parentInstanceId", parentInstanceId.HasValue ? (int)parentInstanceId.Value : -1);
            builder.AddUIntArray("childrenInstanceIds", childrenInstanceIds.ToArray());
            builder.AddStringArray("labels", labels.ToArray());
        }
    }
}
