using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// The specifics of each reported box
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public struct BoundingBox3D : IMessageProducer
    {
        /// <summary>
        /// Integer identifier of the label
        /// </summary>
        public int labelId { get; set; }
        /// <summary>
        /// String identifier of the label
        /// </summary>
        public string labelName { get; set; }
        /// <summary>
        /// UUID of the instance
        /// </summary>
        public uint instanceId { get; set; }
        /// <summary>
        /// 3d bounding box's center location in meters as center_x, center_y, center_z with respect to global coordinate system
        /// </summary>
        public Vector3 translation { get; set; }
        /// <summary>
        /// 3d bounding box size in meters as width, length, height
        /// </summary>
        public Vector3 size { get; set; }
        /// <summary>
        /// 3d bounding box orientation as quaternion: w, x, y, z
        /// </summary>
        public Quaternion rotation { get; set; }
        /// <summary>
        /// [optional]: 3d bounding box velocity in meters per second as v_x, v_y, v_z
        /// </summary>
        public Vector3 velocity { get; set; }
        /// <summary>
        /// [optional]: 3d bounding box acceleration in meters per second^2 as a_x, a_y, a_z
        /// </summary>
        public Vector3 acceleration { get; set; }

        /// <inheritdoc/>
        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddInt("instanceId", (int)instanceId);
            builder.AddInt("labelId", labelId);
            builder.AddString("labelName", labelName);
            builder.AddFloatArray("translation", MessageBuilderUtils.ToFloatVector(translation));
            builder.AddFloatArray("size", MessageBuilderUtils.ToFloatVector(size));
            builder.AddFloatArray("rotation", MessageBuilderUtils.ToFloatVector(rotation));
            builder.AddFloatArray("velocity", MessageBuilderUtils.ToFloatVector(velocity));
            builder.AddFloatArray("acceleration", MessageBuilderUtils.ToFloatVector(acceleration));
        }
    }
}
