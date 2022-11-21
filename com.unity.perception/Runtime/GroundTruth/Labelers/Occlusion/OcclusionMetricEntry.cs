using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Occlusion Metric entry struct
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public struct OcclusionMetricEntry : IMessageProducer
    {
        /// <summary>
        /// The unique instance id of the visible object
        /// </summary>
        public uint instanceID;

        /// <summary>
        /// The proportion of the object that is visible in the image
        /// </summary>
        public float percentVisible;

        /// <summary>
        /// The proportion of the object that is not occluded by the camera frame.
        /// </summary>
        public float percentInFrame;

        /// <summary>
        /// The unoccluded portion of the part of an object that is in frame.
        /// </summary>
        public float visibilityInFrame;

        /// <inheritdoc/>
        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddUInt("instanceId", instanceID);
            builder.AddFloat("percentVisible", percentVisible);
            builder.AddFloat("percentInFrame", percentInFrame);
            builder.AddFloat("visibilityInFrame", visibilityInFrame);
        }
    }
}
