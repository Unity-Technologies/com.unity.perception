using System;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// An instance segmentation entry
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public struct InstanceSegmentationEntry
    {
        /// <summary>
        /// The instance ID associated with a pixel color
        /// </summary>
        public int instanceId { get; set; }
        /// <summary>
        /// The label ID of the instance
        /// </summary>
        public int labelId { get; set; }
        /// <summary>
        /// The label name of the instance
        /// </summary>
        public string labelName { get; set; }
        /// <summary>
        /// The color (rgba) value
        /// </summary>
        [field: FormerlySerializedAs("rgba")]
        public Color32 color { get; set; }

        /// <inheritdoc/>
        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddInt("instanceId", instanceId);
            builder.AddInt("labelId", labelId);
            builder.AddString("labelName", labelName);
            builder.AddIntArray("color", MessageBuilderUtils.ToIntVector(color));
        }
    }
}
