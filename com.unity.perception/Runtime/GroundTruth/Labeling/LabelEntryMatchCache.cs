using System;
using Unity.Collections;
using Unity.Entities;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Cache of instance id -> label entry index for a LabelConfig. This is not well optimized and is the source of
    /// a known memory leak for apps that create new instances frequently
    /// </summary>
    class LabelEntryMatchCache : IGroundTruthGenerator, IDisposable
    {
        // The initial size of the cache. Large enough to avoid resizing small lists multiple times
        const int k_StartingObjectCount = 1 << 8;
        NativeList<ushort> m_InstanceIdToLabelEntryIndexLookup;
        IdLabelConfig m_IdLabelConfig;
        ushort m_DefaultValue = ushort.MaxValue;

        public LabelEntryMatchCache(IdLabelConfig idLabelConfig)
        {
            m_IdLabelConfig = idLabelConfig;
            m_InstanceIdToLabelEntryIndexLookup = new NativeList<ushort>(k_StartingObjectCount, Allocator.Persistent);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GroundTruthLabelSetupSystem>().Activate(this);
        }

        public bool TryGetLabelEntryFromInstanceId(uint instanceId, out IdLabelEntry labelEntry, out int index)
        {
            labelEntry = default;
            index = -1;
            if (m_InstanceIdToLabelEntryIndexLookup.Length <= instanceId || m_InstanceIdToLabelEntryIndexLookup[(int)instanceId] == m_DefaultValue)
                return false;

            index = m_InstanceIdToLabelEntryIndexLookup[(int)instanceId];
            labelEntry = m_IdLabelConfig.labelEntries[index];
            return true;
        }

        void IGroundTruthGenerator.SetupMaterialProperties(MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId)
        {
            if (m_IdLabelConfig.TryGetMatchingConfigurationEntry(labeling, out _, out var index))
            {
                Debug.Assert(index < m_DefaultValue, "Too many entries in the label config");
                if (m_InstanceIdToLabelEntryIndexLookup.Length <= instanceId)
                {
                    var oldLength = m_InstanceIdToLabelEntryIndexLookup.Length;
                    m_InstanceIdToLabelEntryIndexLookup.Resize((int)instanceId + 1, NativeArrayOptions.ClearMemory);

                    for (int i = oldLength; i < instanceId; i++)
                        m_InstanceIdToLabelEntryIndexLookup[i] = m_DefaultValue;
                }
                m_InstanceIdToLabelEntryIndexLookup[(int)instanceId] = (ushort)index;
            }
            else if (m_InstanceIdToLabelEntryIndexLookup.Length > (int)instanceId)
            {
                m_InstanceIdToLabelEntryIndexLookup[(int)instanceId] = m_DefaultValue;
            }
        }

        public void Dispose()
        {
            World.DefaultGameObjectInjectionWorld?.GetExistingSystem<GroundTruthLabelSetupSystem>()?.Deactivate(this);
            m_InstanceIdToLabelEntryIndexLookup.Dispose();
        }
    }
}
