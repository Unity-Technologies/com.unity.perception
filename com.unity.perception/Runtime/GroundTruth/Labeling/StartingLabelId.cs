using System;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Selector for whether label ids should start at zero or one. <seealso cref="LabelingConfiguration.StartingLabelId"/>.
    /// </summary>
    public enum StartingLabelId
    {
        /// <summary>
        /// Start label id assignment at 0
        /// </summary>
        Zero,
        /// <summary>
        /// Start label id assignment at 1
        /// </summary>
        One
    }
}
