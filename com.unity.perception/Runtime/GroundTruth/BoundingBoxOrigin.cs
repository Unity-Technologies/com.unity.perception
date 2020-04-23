using System;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// The origin to use for bounding box calculation
    /// </summary>
    public enum BoundingBoxOrigin
    {
        /// <summary>
        /// (0, 0) is located at the top-left of the image, with +y pointing down.
        /// </summary>
        TopLeft,
        /// <summary>
        /// (0, 0) is located at the bottom-left of the image, with +y pointing up.
        /// </summary>
        BottomLeft
    }
}
