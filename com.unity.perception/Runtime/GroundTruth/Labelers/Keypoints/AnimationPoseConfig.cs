using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Record that maps a pose to a timestamp
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
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
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class AnimationPoseConfig : ScriptableObject
    {
        const string k_Unset = "unset";

        /// <summary>
        /// The animation clip used for all of the timestamps
        /// </summary>
        public AnimationClip animationClip;

        /// <summary>
        /// The list of timestamps
        /// </summary>
        [SerializeField] List<PoseTimestampRecord> m_Timestamps = new List<PoseTimestampRecord>();

        /// <summary>
        /// The sorted list of timestamps
        /// </summary>
        public List<PoseTimestampRecord> timestamps
        {
            get => m_Timestamps;
            set
            {
                m_Timestamps = value;
                SortTimestamps();
            }
        }

        /// <summary>
        /// Retrieves the pose for the clip at the current time.
        /// </summary>
        /// <param name="time">The time in question</param>
        /// <returns>The pose for the passed in time</returns>
        public string GetPoseAtTime(float time)
        {
            if (time < 0f || time > 1f) return k_Unset;
            if (m_Timestamps == null || !m_Timestamps.Any()) return k_Unset;

            // Special case code if there is only 1 timestamp in the config
            if (m_Timestamps.Count == 1)
            {
                return time > m_Timestamps[0].startOffsetPercent ? m_Timestamps[0].poseLabel : k_Unset;
            }

            for (var i = 0; i < m_Timestamps.Count - 1; i++)
            {
                if (time >= m_Timestamps[i].startOffsetPercent && time <= m_Timestamps[i + 1].startOffsetPercent)
                    return m_Timestamps[i].poseLabel;
            }

            var last = m_Timestamps.Last();
            return time < last.startOffsetPercent ? k_Unset : last.poseLabel;
        }

        void OnEnable()
        {
            SortTimestamps();
        }

        void SortTimestamps()
        {
            m_Timestamps.Sort((stamp1, stamp2) => stamp1.startOffsetPercent.CompareTo(stamp2.startOffsetPercent));
        }
    }
}
