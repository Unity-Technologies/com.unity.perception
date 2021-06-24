using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Required interface for entries in a <see cref="LabelConfig{T}"/>. Exposes the string label which is the "key"
    /// for the entry.
    /// </summary>
    public interface ILabelEntry
    {
        /// <summary>
        /// The label to use as the key for the entry. This label will be matched with the labels in the GameObject's
        /// <see cref="Labeling"/> component.
        /// </summary>
        string label { get; }
    }
    /// <summary>
    /// A definition for how a <see cref="Labeling"/> should be resolved to a single label and id for ground truth generation.
    /// </summary>
    /// <typeparam name="T"> The entry type. Must derive from <see cref="ILabelEntry"/> </typeparam>
    public class LabelConfig<T> : ScriptableObject where T : ILabelEntry
    {
        /// <summary>
        /// The name of the serialized field for label entries.
        /// </summary>
        public const string labelEntriesFieldName = nameof(m_LabelEntries);

        /// <summary>
        /// List of LabelEntry items added to this label configuration
        /// </summary>
        [FormerlySerializedAs("LabelEntries")]
        [FormerlySerializedAs("LabelingConfigurations")]
        [SerializeField]
        protected List<T> m_LabelEntries = new List<T>();

        /// <summary>
        /// Name of the public accessor for the list of label entries, used for reflection purposes.
        /// </summary>
        public const string publicLabelEntriesFieldName = nameof(labelEntries);
        /// <summary>
        /// A sequence of <see cref="ILabelEntry"/> which defines the labels relevant for this configuration and their values.
        /// </summary>
        public IReadOnlyList<T> labelEntries => m_LabelEntries;

        /// <summary>
        /// Attempts to find the matching index in <see cref="m_LabelEntries"/> for the given <see cref="Labeling"/>.
        /// </summary>
        /// <remarks>
        /// The matching index is the first class name in the given Labeling which matches an entry in <see cref="m_LabelEntries"/>.
        /// </remarks>
        /// <param name="labeling">The <see cref="Labeling"/> to match </param>
        /// <param name="labelEntry">When this method returns, contains the matching <see cref="ILabelEntry"/>, or <code>default</code> if no match was found.</param>
        /// <returns>Returns true if a match was found. False if not.</returns>
        public bool TryGetMatchingConfigurationEntry(Labeling labeling, out T labelEntry)
        {
            return TryGetMatchingConfigurationEntry(labeling, out labelEntry, out int _);
        }

        /// <summary>
        /// Name of the function that checks whether a given string matches any of the label entries in this label configuration, used for reflection purposes.
        /// </summary>

        public const string DoesLabelMatchAnEntryName = nameof(DoesLabelMatchAnEntry);
        /// <summary>
        /// Does the given string match any of the label entries added to this label configuration.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public bool DoesLabelMatchAnEntry(string label)
        {
            return m_LabelEntries.Any(entry => string.Equals(entry.label, label));
        }

        /// <summary>
        /// Initialize the list of LabelEntries on this LabelingConfiguration. Should only be called immediately after instantiation.
        /// </summary>
        /// <param name="newLabelEntries">The LabelEntry values to associate with this LabelingConfiguration</param>
        /// <exception cref="InvalidOperationException">Thrown once the LabelConfig has been used at runtime.
        /// The specific timing of this depends on the LabelConfig implementation.</exception>
        public void Init(IEnumerable<T> newLabelEntries)
        {
            m_LabelEntries = new List<T>(newLabelEntries);
            OnInit();
        }

        /// <summary>
        /// Called when the labelEntries list is assigned using <see cref="Init"/>
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// Attempts to find the matching index in <see cref="m_LabelEntries"/> for the given <see cref="Labeling"/>.
        /// </summary>
        /// <remarks>
        /// The matching index is the first class name in the given Labeling which matches an entry in <see cref="m_LabelEntries"/>.
        /// </remarks>
        /// <param name="labeling">The <see cref="Labeling"/> to match </param>
        /// <param name="labelEntry">When this method returns, contains the matching <see cref="ILabelEntry"/>, or <code>default</code> if no match was found.</param>
        /// <param name="labelEntryIndex">When this method returns, contains the index of the matching <see cref="ILabelEntry"/>, or <code>-1</code> if no match was found.</param>
        /// <returns>Returns true if a match was found. False if not.</returns>
        public bool TryGetMatchingConfigurationEntry(Labeling labeling, out T labelEntry, out int labelEntryIndex)
        {
            foreach (var labelingClass in labeling.labels)
            {
                for (var i = 0; i < m_LabelEntries.Count; i++)
                {
                    var entry = m_LabelEntries[i];
                    if (string.Equals(entry.label, labelingClass))
                    {
                        labelEntry = m_LabelEntries[i];
                        labelEntryIndex = i;
                        return true;
                    }
                }
            }

            labelEntryIndex = -1;
            labelEntry = default;
            return false;
        }
    }
}
