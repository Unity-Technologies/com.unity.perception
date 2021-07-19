using System;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// When attached to a model used by the <see cref="KeypointLabeler"/> overrides the distance values
    /// for each keypoint defined in <see cref="KeypointTemplate"/> by multiplying them by this overrideDistanceScale
    /// scalar. The values in <see cref="KeypointTemplate"/> are generally set for a typical adult model, which makes it
    /// so that these values do not meet the needs of models with different body types (i.e. children, different heights, different weights).
    /// Changing the value of the scalar will help to get keypoint occlusion working properly for these models. A value of 1.0
    /// will use the template values as is.
    /// </summary>
    public class KeypointOcclusionOverrides : MonoBehaviour
    {
        [Tooltip("Overrides the default occlusion distance values by a scalar. This is necessary for bodies with different body types (i.e. children should be less than one)")]
        [SerializeField]
        // ReSharper disable once InconsistentNaming, this needs to be human readable in the inspector
        float distanceScale = 1.0f;

        /// <summary>
        /// The value to use to scale the values for keypoints distances defined in <see cref="KeypointTemplate"/>
        /// </summary>
        public float overrideDistanceScale
        {
            get => distanceScale;
            internal set => distanceScale = value;
        }
    }
}
