using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Unity.Collections;

namespace UnityEngine.Perception.GroundTruth {
    /// <summary>
    /// A definition for how a <see cref="Labeling"/> should be resolved to a single label and id for ground truth generation.
    /// </summary>
    [CreateAssetMenu(fileName = "IdLabelConfig", menuName = "Perception/ID Label Config", order = 1)]
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
            if (m_LabelEntryMatchCache == null)
                m_LabelEntryMatchCache = new LabelEntryMatchCache(this);

            return m_LabelEntryMatchCache.TryGetLabelEntryFromInstanceId(instanceId, out labelEntry, out index);
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

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal struct LabelEntrySpec
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
        }

        internal LabelEntrySpec[] GetAnnotationSpecification()
        {
            return labelEntries.Select((l) => new LabelEntrySpec()
            {
                label_id = l.id,
                label_name = l.label,
            }).ToArray();
        }

        public static IdLabelMap GetIdLabelCache(Allocator allocator = Allocator.Temp)
        {
            return new IdLabelMap();
        }
    }

    public struct IdLabelMap : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
