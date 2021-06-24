using System;

namespace UnityEngine.Perception.GroundTruth {
    /// <summary>
    /// An entry for <see cref="IdLabelConfig"/> mapping a label to an integer id.
    /// </summary>
    [Serializable]
    public struct IdLabelEntry : ILabelEntry, IEquatable<IdLabelEntry>
    {
        string ILabelEntry.label => this.label;
        /// <summary>
        /// The label string to associate with the id.
        /// </summary>
        public string label;
        /// <summary>
        /// The id to associate with the label.
        /// </summary>
        public int id;

        /// <inheritdoc/>
        public bool Equals(IdLabelEntry other)
        {
            return label == other.label && id == other.id;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is IdLabelEntry other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((label != null ? label.GetHashCode() : 0) * 397) ^ id;
            }
        }
    }
}
