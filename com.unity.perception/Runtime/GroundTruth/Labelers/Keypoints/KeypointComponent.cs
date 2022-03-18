using System;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    public struct KeypointComponent : IMessageProducer
    {
        /// <summary>
        /// The label id of the entity
        /// </summary>
        public int labelId { get; set; }
        /// <summary>
        /// The instance id of the entity
        /// </summary>
        public uint instanceId { get; set; }
        /// <summary>
        /// Pose ground truth for the current set of keypoints
        /// </summary>
        public string pose { get; set; }

        /// <summary>
        /// Array of all of the keypoints
        /// </summary>
        public KeypointValue[] keypoints;// { get; set; }

        /// <inheritdoc/>
        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddInt("instanceId", (int)instanceId);
            builder.AddInt("labelId", labelId);
            builder.AddString("pose", pose);
            foreach (var keypoint in keypoints)
            {
                var nested = builder.AddNestedMessageToVector("keypoints");
                keypoint.ToMessage(nested);
            }
        }
    }
}
