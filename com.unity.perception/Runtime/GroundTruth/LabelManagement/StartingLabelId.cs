using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.LabelManagement
{
    /// <summary>
    /// Selector for whether label ids should start at zero or one. <seealso cref="IdLabelConfig.startingLabelId"/>.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public enum StartingLabelId
    {
        /// <summary>
        /// Start label id assignment at 0
        /// </summary>
        [InspectorName("0")]
        Zero,
        /// <summary>
        /// Start label id assignment at 1
        /// </summary>
        [InspectorName("1")]
        One
    }
}
