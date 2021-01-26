using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Record that maps a pose to a timestamp
    /// </summary>
    [Serializable]
    public class PoseTimestampRecord
    {
        /// <summary>
        /// The duration to use for this timestamp in seconds
        /// </summary>
        public float duration;
        /// <summary>
        /// The label to use for any captures inside of this time period
        /// </summary>
        public string poseLabel;
    }

    /// <summary>
    /// The animation pose label is a mapping that file that maps a time range in an animation clip to a ground truth
    /// pose. The timestamp record is defined by a pose label and a duration. The timestamp records are order dependent
    /// and build on the previous entries. This means that if the first record has a duration of 5, then it will be the label
    /// for all points in the clip from 0 (the beginning) to the five second mark. The next record will then go from the end
    /// of the previous clip to its duration. If there is time left over in the flip, the final entry will be used.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationPoseTimestamp", menuName = "Perception/Animation Pose Timestamps")]
    public class AnimationPoseLabel : ScriptableObject
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
            var totalTime = 0f;

            // The amount of timestamps are expected to be small, so at the current time
            // no performance improvements are deemed necesssary
            foreach (var t in timestamps)
            {
                totalTime += t.duration;
                if (time < totalTime) return t.poseLabel;
            }

            return timestamps.Last().poseLabel;
        }
    }
}
