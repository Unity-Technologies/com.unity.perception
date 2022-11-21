using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// The measurement strategies available to the depth labeler.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public enum DepthMeasurementStrategy
    {
        /// <summary>
        /// Captured depth values return the distance between the
        /// surface of the object and the forward plane of the camera.
        /// </summary>
        Depth,

        /// <summary>
        /// Captured range values return the line of sight distance between the
        /// surface of the object and the position of the camera.
        /// </summary>
        Range
    }
}
