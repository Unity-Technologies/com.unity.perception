using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// When attached to a model used by the <see cref="KeypointLabeler"/> overrides the distance values
    /// for each keypoint defined in <see cref="KeypointTemplate"/> by multiplying them by this overrideDistanceScale
    /// scalar. The values in <see cref="KeypointTemplate"/> are generally set for a typical adult model, which makes it
    /// so that these values do not meet the needs of models with different body types (i.e. children, different heights, different weights).
    /// Changing the value of the scalar will help to get keypoint occlusion working properly for these models. A value of 1.0
    /// will use the template values as is.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class KeypointOcclusionOverrides : MonoBehaviour
    {
        /// <summary> Overrides the default occlusion distance values by a scalar. This is necessary for bodies with different body types (i.e. children should be less than one) </summary>
        [Tooltip("Overrides the default occlusion distance values by a scalar. This is necessary for bodies with different body types (i.e. children should be less than one)")]
        public float distanceScale = 1.0f;
    }
}
