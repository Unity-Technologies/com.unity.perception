using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;

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
    /// pose. The timestamp record is defined by a pose label and a start time. The timestamp records are order dependent.
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

        SortedList<float, string> sortedTimestamps;
        void OnEnable()
        {
            sortedTimestamps = new SortedList<float, string>(timestamps.Count);
            foreach (var ts in timestamps)
            {
                sortedTimestamps.Add(ts.startOffsetPercent, ts.poseLabel);
            }
        }

        const string k_Unset = "unset";

        /// <summary>
        /// Retrieves the pose for the clip at the current time.
        /// </summary>
        /// <param name="time">The time in question</param>
        /// <returns>The pose for the passed in time</returns>
        public string GetPoseAtTime(float time)
        {
            if (time < 0 || time > 1) return k_Unset;
            if (timestamps == null || !timestamps.Any()) return k_Unset;

            // Special case code if there is only 1 timestamp in the config
            if (sortedTimestamps.Keys.Count == 1)
            {
                return time > sortedTimestamps.Keys[0] ? sortedTimestamps.Values[0] : k_Unset;
            }

            for (var i = 0; i < sortedTimestamps.Keys.Count - 1; i++)
            {
                if (time >= sortedTimestamps.Keys[i] && time <= sortedTimestamps.Keys[i + 1]) return sortedTimestamps.Values[i];
            }

            return time < sortedTimestamps.Keys.Last() ? k_Unset : sortedTimestamps.Values.Last();
        }
    }
}
