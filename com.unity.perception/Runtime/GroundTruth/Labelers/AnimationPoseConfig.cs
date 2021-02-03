using System;
using System.Collections.Generic;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Record that maps a pose to a timestamp
    /// </summary>
    [Serializable]
    public class PoseTimestampRecord
    {
        /// <summary>
        /// The percentage within the clip that the pose starts, a value from 0 (beginning) to 1 (end)
        /// </summary>
        [Tooltip("The percentage within the clip that the pose starts, a value from 0 (beginning) to 1 (end)")]
        public float startOffsetPercent;
        /// <summary>
        /// The label to use for any captures inside of this time period
        /// </summary>
        public string poseLabel;
    }

    /// <summary>
    /// The animation pose config is a configuration file that maps a time range in an animation clip to a ground truth
    /// pose. The timestamp record is defined by a pose label and a start time. The timestamp records are order dependent
    /// and build on the previous entries.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationPoseConfig", menuName = "Perception/Animation Pose Config")]
    public class AnimationPoseConfig : ScriptableObject
    {
        /// <summary>
        /// The animation clip used for all of the timestamps
        /// </summary>
        public AnimationClip animationClip;
        /// <summary>
        /// The list of timestamps, order dependent
        /// </summary>
        public List<PoseTimestampRecord> timestamps;

        /// <summary>
        /// Retrieves the pose for the clip at the current time.
        /// </summary>
        /// <param name="time">The time in question</param>
        /// <returns>The pose for the passed in time</returns>
        public string GetPoseAtTime(float time)
        {
            if (time < 0 || time > 1) return "unset";

            var i = 1;
            for (i = 1; i < timestamps.Count; i++)
            {
                if (timestamps[i].startOffsetPercent > time) break;
            }

            return timestamps[i - 1].poseLabel;
        }
    }
}
