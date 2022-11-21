using System;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.LabelManagement
{
    /// <summary>
    /// Cache of instance id -> label entry index for a LabelConfig. This is not well optimized and is the source of
    /// a known memory leak for apps that create new instances frequently.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class LabelEntryMatchCache : IGroundTruthGenerator, IDisposable
    {
        // The initial size of the cache. Large enough to avoid resizing small lists multiple times
        const int k_StartingObjectCount = 1 << 8;
        NativeList<ushort> m_InstanceIdToLabelEntryIndexLookup;
        IdLabelConfig m_IdLabelConfig;
        private bool m_ReceiveUpdates;
        const ushort k_DefaultValue = ushort.MaxValue;

        internal LabelEntryMatchCache(IdLabelConfig idLabelConfig, Allocator allocator = Allocator.Persistent, bool receiveUpdates = true)
        {
            m_IdLabelConfig = idLabelConfig;
            m_InstanceIdToLabelEntryIndexLookup = new NativeList<ushort>(k_StartingObjectCount, allocator);
            m_ReceiveUpdates = receiveUpdates;
            if (receiveUpdates)
                LabelManager.singleton.Activate(this);
        }

        private LabelEntryMatchCache(LabelEntryMatchCache labelEntryMatchCache, Allocator allocator)
        {
            m_IdLabelConfig = labelEntryMatchCache.m_IdLabelConfig;
            m_InstanceIdToLabelEntryIndexLookup = new NativeList<ushort>(labelEntryMatchCache.m_InstanceIdToLabelEntryIndexLookup.Length, allocator);
            m_InstanceIdToLabelEntryIndexLookup.AddRange(labelEntryMatchCache.m_InstanceIdToLabelEntryIndexLookup.AsArray());
            m_ReceiveUpdates = false;
        }

        /// <summary>
        /// Retrieves the label entry for the given instance id.
        /// </summary>
        /// <param name="instanceId">The instance id to look up</param>
        /// <param name="labelEntry">The <see cref="IdLabelEntry"/> of the match if found. Otherwise returns <code>default(IdlabelEntry)</code>.</param>
        /// <param name="index">The index of the matched <see cref="IdLabelEntry"/> in the <see cref="IdLabelConfig"/> if found. Otherwise returns -1.</param>
        /// <returns>True if a the instance id was found in the cache. </returns>
        public bool TryGetLabelEntryFromInstanceId(uint instanceId, out IdLabelEntry labelEntry, out int index)
        {
            labelEntry = default;
            index = -1;
            if (m_InstanceIdToLabelEntryIndexLookup.Length <= instanceId || m_InstanceIdToLabelEntryIndexLookup[(int)instanceId] == k_DefaultValue)
                return false;

            index = m_InstanceIdToLabelEntryIndexLookup[(int)instanceId];
            labelEntry = m_IdLabelConfig.labelEntries[index];
            return true;
        }

        /// <inheritdoc/>
        void IGroundTruthGenerator.SetupMaterialProperties(
            MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, Material material, uint instanceId)
        {
            if (m_IdLabelConfig.TryGetMatchingConfigurationEntry(labeling, out _, out var index))
            {
                Debug.Assert(index < k_DefaultValue, "Too many entries in the label config");

                if (m_InstanceIdToLabelEntryIndexLookup.Length <= instanceId)
                {
                    var oldLength = m_InstanceIdToLabelEntryIndexLookup.Length;
                    m_InstanceIdToLabelEntryIndexLookup.Resize((int)instanceId + 1, NativeArrayOptions.ClearMemory);

                    for (var i = oldLength; i < instanceId; i++)
                        m_InstanceIdToLabelEntryIndexLookup[i] = k_DefaultValue;
                }
                m_InstanceIdToLabelEntryIndexLookup[(int)instanceId] = (ushort)index;
            }
            else if (m_InstanceIdToLabelEntryIndexLookup.Length > instanceId)
            {
                m_InstanceIdToLabelEntryIndexLookup[(int)instanceId] = k_DefaultValue;
            }
        }

        /// <inheritdoc/>
        void IGroundTruthGenerator.ClearMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId)
        {
            if (m_InstanceIdToLabelEntryIndexLookup.Length > instanceId)
            {
                m_InstanceIdToLabelEntryIndexLookup[(int)instanceId] = k_DefaultValue;
            }
        }

        /// <summary>
        /// Disposes cache
        /// </summary>
        public void Dispose()
        {
            if (m_ReceiveUpdates)
                LabelManager.singleton.Deactivate(this);

            m_InstanceIdToLabelEntryIndexLookup.Dispose();
        }

        internal LabelEntryMatchCache CloneCurrentState(Allocator allocator)
        {
            var clone = new LabelEntryMatchCache(this, Allocator.Persistent);
            return clone;
        }
    }
}
