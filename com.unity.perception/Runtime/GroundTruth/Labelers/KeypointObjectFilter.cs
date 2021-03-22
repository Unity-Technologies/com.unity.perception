﻿namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Keypoint filtering modes.
    /// </summary>
    public enum KeypointObjectFilter
    {
        /// <summary>
        /// Only include objects which are partially visible in the frame.
        /// </summary>
        [InspectorName("Visible objects")]
        Visible,
        /// <summary>
        /// Include visible objects and objects with keypoints in the frame.
        /// </summary>
        [InspectorName("Visible and occluded objects")]
        VisibleAndOccluded,
        /// <summary>
        /// Include all labeled objects containing matching skeletons.
        /// </summary>
        [InspectorName("All objects")]
        All
    }
}
