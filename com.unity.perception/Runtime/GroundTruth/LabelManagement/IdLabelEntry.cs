using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.LabelManagement
{
    /// <summary>
    /// This relationship defines how bounding boxes are calculated for this Labeled object's direct parent (if any).
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public enum HierarchyRelation
    {
        /// <summary>
        /// The parent's bounding boxes will be calculated without taking its child's bounding box into consideration.
        /// </summary>
        Independent = 0,
        /// <summary>
        /// The parent's bounding boxes will be enlarged to include its child's bounding box too.
        /// </summary>
        AddToParent = 1,
    }

    /// <summary>
    /// An entry for <see cref="IdLabelConfig"/> mapping a label to an integer id.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public struct IdLabelEntry : ILabelEntry, IEquatable<IdLabelEntry>
    {
        /// <summary>
        /// The label string to associate with the id.
        /// </summary>
        public string label;

        /// <summary>
        /// The id to associate with the label.
        /// </summary>
        public int id;

        /// <inheritdoc cref="HierarchyRelation" />
        public HierarchyRelation hierarchyRelation;

        string ILabelEntry.label => label;

        /// <summary>
        /// Override comparing with any other objects.
        /// Only typeof(IdLabelEntry) are accepted
        /// </summary>
        /// <param name="other">IdLabelEntry</param>
        /// <returns>True if object is IdLabelEntry and label and id are equal</returns>
        public bool Equals(IdLabelEntry other)
        {
            return label == other.label
                && id == other.id
                && hierarchyRelation == other.hierarchyRelation;
        }

        /// <summary>
        /// Override comparing with any other objects.
        /// Only typeof(IdLabelEntry) are accepted
        /// </summary>
        /// <param name="obj">Any object</param>
        /// <returns>True if object is IdLabelEntry and label and id are equal</returns>
        public override bool Equals(object obj)
        {
            return obj is IdLabelEntry other && Equals(other);
        }

        /// <summary>
        /// Override GetHashCode()
        /// </summary>
        /// <returns>Custom hash code as <see cref="int"/>></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((label != null ? label.GetHashCode() : 0) * 397) ^ id;
            }
        }
    }
}
