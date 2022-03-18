using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    public struct BoundingBox : IMessageProducer
    {
        // The instance ID of the object
        public int instanceId { get; set; }

        public int labelId { get; set; }

        // The type of the object
        public string labelName { get; set; }

        /// <summary>
        /// (xy) pixel location of the object's bounding box
        /// </summary>
        public Vector2 origin { get; set; }
        /// <summary>
        /// (width/height) dimensions of the bounding box
        /// </summary>
        public Vector2 dimension { get; set; }

        /// <inheritdoc/>
        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddInt("instanceId", instanceId);
            builder.AddInt("labelId", labelId);
            builder.AddString("labelName", labelName);
            builder.AddFloatArray("origin", new[] { origin.x, origin.y });
            builder.AddFloatArray("dimension", new[] { dimension.x, dimension.y });
        }
    }
}
