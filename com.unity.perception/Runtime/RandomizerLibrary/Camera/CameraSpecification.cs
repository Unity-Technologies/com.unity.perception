using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.Randomization
{
    /// <summary>
    /// A ScriptableObject containing information about Camera Intrinsics.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.Internal")]
    [CreateAssetMenu(fileName = "CameraSpec", menuName = "Perception/Randomization/Camera Specification", order = 0)]
    public class CameraSpecification : ScriptableObject
    {
        /// <summary>
        /// An optional description of the specification.
        /// </summary>
        public string specificationDescription;

        /// <summary>
        /// The distance between the lens and the sensor. Larger values lead to smaller field of views.
        /// </summary>
        public float focalLength;

        /// <summary>
        /// The size of the camera sensor in millimeters.
        /// </summary>
        public Vector2 sensorSize;

        /// <summary>
        /// Offset from the camera sensor. Measured as multiples of the <see cref="sensorSize" />.
        /// </summary>
        public Vector2 lensShift;

        /// <summary>
        /// Determines how the rendered area (resolution gate) fits into the sensor area (film gate).
        /// </summary>
        public Camera.GateFitMode gateFitMode;
    }
}
