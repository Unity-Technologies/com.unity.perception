using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors
{
    /// <summary>
    /// An enumerated list of power-of-two resolutions compatible with Unity's cubemap texture format.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth.Internal")]
    public enum CubemapResolution
    {
        /// <summary>
        /// 32 pixel width resolution for each square cube face.
        /// </summary>
        _32x32 = 32,

        /// <summary>
        /// 64 pixel width resolution for each square cube face.
        /// </summary>
        _64x64 = 64,

        /// <summary>
        /// 128 pixel width resolution for each square cube face.
        /// </summary>
        _128x128 = 128,

        /// <summary>
        /// 256 pixel width resolution for each square cube face.
        /// </summary>
        _256x256 = 256,

        /// <summary>
        /// 512 pixel width resolution for each square cube face.
        /// </summary>
        _512x512 = 512,

        /// <summary>
        /// 1024 pixel width resolution for each square cube face.
        /// </summary>
        _1024x1024 = 1024,

        /// <summary>
        /// 2048 pixel width resolution for each square cube face.
        /// </summary>
        _2048x2048 = 2048
    }
}
