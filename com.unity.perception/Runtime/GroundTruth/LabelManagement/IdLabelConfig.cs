using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.LabelManagement
{
    /// <summary>
    /// A definition for how a <see cref="Labeling"/> should be resolved to a single label and id for ground truth generation.
    /// </summary>
    [CreateAssetMenu(fileName = "IdLabelConfig", menuName = "Perception/ID Label Config", order = 1)]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class IdLabelConfig : LabelConfig<IdLabelEntry>
    {
        /// <summary>
        /// Whether the inspector will auto-assign ids based on the id of the first element.
        /// </summary>
        public bool autoAssignIds = true;

        /// <summary>
        /// Whether the inspector will start label ids at zero or one when <see cref="autoAssignIds"/> is enabled.
        /// </summary>
        public StartingLabelId startingLabelId = StartingLabelId.One;

        LabelEntryMatchCache m_LabelEntryMatchCache;

        /// <summary>
        /// Attempts to find the label id for the given instance id.
        /// </summary>
        /// <param name="instanceId">The instanceId of the object for which the labelId should be found</param>
        /// <param name="labelEntry">The LabelEntry associated with the object. default if not found</param>
        /// <returns>True if a labelId is found for the given instanceId.</returns>
        public bool TryGetLabelEntryFromInstanceId(uint instanceId, out IdLabelEntry labelEntry)
        {
            return TryGetLabelEntryFromInstanceId(instanceId, out labelEntry, out var _);
        }

        /// <summary>
        /// Attempts to find the label id for the given instance id.
        /// </summary>
        /// <param name="instanceId">The instanceId of the object for which the labelId should be found</param>
        /// <param name="labelEntry">The LabelEntry associated with the object. default if not found</param>
        /// <param name="index">The index of the found LabelEntry in <see cref="LabelConfig{T}.labelEntries"/>. -1 if not found</param>
        /// <returns>True if a labelId is found for the given instanceId.</returns>
        public bool TryGetLabelEntryFromInstanceId(uint instanceId, out IdLabelEntry labelEntry, out int index)
        {
            EnsureInitLabelEntryMatchCache();

            return m_LabelEntryMatchCache.TryGetLabelEntryFromInstanceId(instanceId, out labelEntry, out index);
        }

        private void EnsureInitLabelEntryMatchCache()
        {
            if (m_LabelEntryMatchCache == null)
                m_LabelEntryMatchCache = new LabelEntryMatchCache(this, Allocator.Persistent);
        }

        /// <inheritdoc/>
        protected override void OnInit()
        {
            if (m_LabelEntryMatchCache != null)
            {
                throw new InvalidOperationException("Init may not be called after TryGetLabelEntryFromInstanceId has been called for the first time.");
            }
        }

        void OnDisable()
        {
            m_LabelEntryMatchCache?.Dispose();
            m_LabelEntryMatchCache = null;
        }

        /// <summary>
        /// A structure representing a label entry for writing out to datasets.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public struct LabelEntrySpec : IMessageProducer
        {
            /// <summary>
            /// The label id prepared for reporting in the annotation
            /// </summary>
            [UsedImplicitly]
            public int label_id;

            /// <summary>
            /// The label name prepared for reporting in the annotation
            /// </summary>
            [UsedImplicitly]
            public string label_name;

            /// <inheritdoc/>
            public void ToMessage(IMessageBuilder builder)
            {
                builder.AddInt("label_id", label_id);
                builder.AddString("label_name", label_name);
            }
        }

        /// <summary>
        /// Returns the label entries as structures suited for writing out to JSON datasets.
        /// </summary>
        /// <returns>The JSON ready label entries.</returns>
        public LabelEntrySpec[] GetAnnotationSpecification()
        {
            return labelEntries.Select((l) => new LabelEntrySpec()
            {
                label_id = l.id,
                label_name = l.label,
            }).ToArray();
        }

        /// <summary>
        /// Creates a LabelEntryMatchCache from the currently registered labeled objects, which can be used to look up
        /// labeling information in future frames, even after the objects have been destroyed. Due to timing of labeled
        /// object registration, if this is called during or before LateUpdate, this cache may become invalid.
        ///
        /// It is recommended to only use this method in rendering, as the cache is guaranteed to be in its final state
        /// for ground truth generation.
        /// </summary>
        /// <param name="allocator">The allocator for creating the cache.</param>
        /// <returns>The created cache.</returns>
        public LabelEntryMatchCache CreateLabelEntryMatchCache(Allocator allocator)
        {
            EnsureInitLabelEntryMatchCache();
            return m_LabelEntryMatchCache.CloneCurrentState(allocator);
        }
    }
}
