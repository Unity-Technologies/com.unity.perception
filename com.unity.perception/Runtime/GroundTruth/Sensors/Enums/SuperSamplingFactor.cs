using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Sensors
{
    /// <summary>
    /// The supported super sampling factors.
    /// Determines how many samples to average per pixel to produce an anti-aliased result.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public enum SuperSamplingFactor
    {
        /// <summary>
        /// No super sampling is performed.
        /// </summary>
        None = 1,

        /// <summary>
        /// Sample double the width and height of the captured image during super sampling anti-aliasing.
        /// </summary>
        _2X = 2,

        /// <summary>
        /// Sample quadruple the width and height of the captured image during super sampling anti-aliasing.
        /// </summary>
        _4X = 4,

        /// <summary>
        /// Sample 8 times the width and height of the captured image during super sampling anti-aliasing.
        /// </summary>
        _8X = 8
    }
}
