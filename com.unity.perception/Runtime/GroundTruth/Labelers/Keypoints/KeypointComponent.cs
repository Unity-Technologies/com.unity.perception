using System;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// KeypointComponent message producer.
    /// Keep all the information about keypoints and handles reporting.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public struct KeypointComponent : IMessageProducer
    {
        /// <summary>
        /// Creates a new, empty keypoint component
        /// </summary>
        /// <param name="inLabelId">The label ID</param>
        /// <param name="inInstanceId">The instance ID of the entity</param>
        /// <param name="inPose">The pose label</param>
        /// <param name="inLength">The length of keypoints array. All points will be initialized with an index set to the
        /// array location and a state of 0.</param>
        public KeypointComponent(int inLabelId, uint inInstanceId, string inPose, int inLength)
        {
            labelId = inLabelId;
            instanceId = inInstanceId;
            pose = inPose;
            keypoints = new KeypointValue[inLength];
            for (var i = 0; i < inLength; i++)
            {
                keypoints[i] = new KeypointValue { index = i, state = 0 };
            }
        }

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
        public KeypointValue[] keypoints;

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
