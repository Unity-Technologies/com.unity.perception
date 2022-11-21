using Unity.Mathematics;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors
{
    /// <summary>
    /// An encapsulation of the common intrinsic properties of a <see cref="CameraSensor"/>.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public struct CameraSensorIntrinsics
    {
        /// <summary>
        /// The projection type of the <see cref="CameraSensor"/>.
        /// </summary>
        public string projection;

        /// <summary>
        /// The projection matrix of the <see cref="CameraSensor"/>.
        /// </summary>
        public float3x3 matrix;
    }
}
