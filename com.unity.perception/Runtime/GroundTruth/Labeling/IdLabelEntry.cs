using System;

namespace UnityEngine.Perception.GroundTruth {
    /// <summary>
    /// An entry for <see cref="IdLabelConfig"/> mapping a label to an integer id.
    /// </summary>
    [Serializable]
    public struct IdLabelEntry : ILabelEntry
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
    }
}
