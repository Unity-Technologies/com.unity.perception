using System;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// The value of an individual keypoint on a keypoint component.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class KeypointValue : IMessageProducer, ICloneable
    {
        /// <summary>
        /// The index of the keypoint in the template file
        /// </summary>
        public int index { get; set; }
        /// <summary>
        /// The location of the keypoint
        /// </summary>
        public Vector2 location;// { get; set; }
        /// <summary>
        /// The cartesian coordinate of the keypoint relative to the camera's coordinate system
        /// </summary>
        public Vector3 cameraCartesianLocation { get; set; }
        /// <summary>
        /// The state of the point,
        /// 0 = not present,
        /// 1 = keypoint is present but not visible,
        /// 2 = keypoint is present and visible
        /// </summary>
        public int state { get; set; }

        /// <inheritdoc/>
        public void ToMessage(IMessageBuilder builder)
        {
            builder.AddInt("index", index);
            builder.AddFloatArray("location", MessageBuilderUtils.ToFloatVector(location));
            builder.AddFloatArray("cameraCartesianLocation", MessageBuilderUtils.ToFloatVector(cameraCartesianLocation));
            builder.AddInt("state", state);
        }

        /// <summary>
        /// Creates a copy of <see cref="KeypointValue"/>
        /// </summary>
        /// <returns>new <see cref="KeypointValue"/></returns>
        public object Clone()
        {
            return new KeypointValue
            {
                index = index,
                location = location,
                cameraCartesianLocation = cameraCartesianLocation,
                state = state
            };
        }
    }
}
