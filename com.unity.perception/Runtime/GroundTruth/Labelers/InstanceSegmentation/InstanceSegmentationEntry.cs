using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// An instance segmentation entry
    /// </summary>
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
        public Color32 rgba { get; set; }

        /// <inheritdoc/>
        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddInt("instanceId", instanceId);
            builder.AddInt("labelId", labelId);
            builder.AddString("labelName", labelName);
            builder.AddIntArray("color", MessageBuilderUtils.ToIntVector(rgba));
        }
    }
}
