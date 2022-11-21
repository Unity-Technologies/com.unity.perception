using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// The 2D bounding box information of a labeled object.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public struct BoundingBox : IMessageProducer
    {
        /// <summary>
        /// The instance ID of the object.
        /// </summary>
        public int instanceId { get; set; }

        /// <summary>
        /// The label id of the object.
        /// </summary>
        public int labelId { get; set; }

        /// <summary>
        /// The type of the object.
        /// </summary>
        public string labelName { get; set; }

        /// <summary>
        /// (xy) pixel location of the object's bounding box.
        /// </summary>
        public Vector2 origin { get; set; }

        /// <summary>
        /// (width/height) dimensions of the bounding box.
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

        /// <summary>
        /// Enlarges the current bounding box to encompass the <see cref="other" /> bounding box.
        /// The newly created bounding box will exactly fit both the old bounding box and the provided
        /// <see cref="other" /> bounding box.
        /// </summary>
        /// <param name="other"></param>
        public void Encapsulate(BoundingBox other)
        {
            var newOrigin = Vector2.Min(other.origin, origin);
            var newExtents = Vector2.Max(
                other.origin + other.dimension,
                origin + dimension
            );
            var newDimension = newExtents - newOrigin;

            origin = newOrigin;
            dimension = newDimension;
        }
    }
}
