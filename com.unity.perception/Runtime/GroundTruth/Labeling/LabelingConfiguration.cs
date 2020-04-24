using System;
using System.Collections.Generic;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A definition for how a <see cref="Labeling"/> should be resolved to a single label and id for ground truth generation.
    /// </summary>
    [CreateAssetMenu(fileName = "LabelingConfiguration", menuName = "Perception/Labeling Configuration", order = 1)]
    public class LabelingConfiguration : ScriptableObject
    {
        /// <summary>
        /// A sequence of <see cref="LabelingConfigurationEntry"/> which defines the labels relevant for this configuration and their values.
        /// </summary>
        [SerializeField]
        public List<LabelingConfigurationEntry> LabelingConfigurations = new List<LabelingConfigurationEntry>();

        /// <summary>
        /// Attempts to find the matching index in <see cref="LabelingConfigurations"/> for the given <see cref="Labeling"/>.
        /// </summary>
        /// <remarks>
        /// The matching index is the first class name in the given Labeling which matches an entry in <see cref="LabelingConfigurations"/>.
        /// </remarks>
        /// <param name="labeling">The <see cref="Labeling"/> to match </param>
        /// <param name="index">When this method returns, contains the index of the matching <see cref="LabelingConfigurationEntry"/>, or -1 if no match was found.</param>
        /// <returns>Returns true if a match was found. False if not.</returns>
        public bool TryGetMatchingConfigurationIndex(Labeling labeling, out int index)
        {
            foreach (var labelingClass in labeling.labels)
            {
                for (var i = 0; i < LabelingConfigurations.Count; i++)
                {
                    var configuration = LabelingConfigurations[i];
                    if (string.Equals(configuration.label, labelingClass))
                    {
                        index = i;
                        return true;
                    }
                }
            }

            index = -1;
            return false;
        }
    }

    /// <summary>
    /// Structure defining a label configuration for <see cref="LabelingConfiguration"/>.
    /// </summary>
    [Serializable]
    public struct LabelingConfigurationEntry
    {
        /// <summary>
        /// The label string
        /// </summary>
        public string label;
        /// <summary>
        /// The value to use when generating semantic segmentation images.
        /// </summary>
        public int value;
        /// <summary>
        /// Creates a new LabelingConfigurationEntry with the given values.
        /// </summary>
        /// <param name="label">The label string.</param>
        /// <param name="value">The value to use when generating semantic segmentation images.</param>
        public LabelingConfigurationEntry(string label, int value)
        {
            this.label = label;
            this.value = value;
        }
    }
}
