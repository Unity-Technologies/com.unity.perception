using UnityEngine;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Pose state ground truth information that can be applied to a gameobject. The typical use case
    /// is that this will be added to a labeled gameobject by a randomizer.
    /// </summary>
    public class PoseStateGroundTruthInfo : MonoBehaviour
    {
        /// <summary>
        /// The pose state
        /// </summary>
        public string poseState;
    }
}
