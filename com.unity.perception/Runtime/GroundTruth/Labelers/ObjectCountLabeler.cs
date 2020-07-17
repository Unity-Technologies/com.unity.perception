using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Labeler which produces object counts for each label in the associated <see cref="IdLabelConfig"/> each frame.
    /// </summary>
    [Serializable]
    public sealed class ObjectCountLabeler : CameraLabeler
    {
        /// <summary>
        /// The ID to use for object count annotations in the resulting dataset
        /// </summary>
        public string objectCountMetricId = "51DA3C27-369D-4929-AEA6-D01614635CE2";

        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        public IdLabelConfig labelConfig => m_LabelConfig;

        /// <summary>
        /// Fired when the object counts are computed for a frame.
        /// </summary>
        public event Action<int, NativeSlice<uint>,IReadOnlyList<IdLabelEntry>> ObjectCountsComputed;

        [SerializeField]
        IdLabelConfig m_LabelConfig;

        static ProfilerMarker s_ClassCountCallback = new ProfilerMarker("OnClassLabelsReceived");

        ClassCountValue[] m_ClassCountValues;

        Dictionary<int, AsyncMetric> m_ObjectCountAsyncMetrics;
        MetricDefinition m_ObjectCountMetricDefinition;

        HUDPanel hud = null;

        Dictionary<string, string> entryToLabelMap = null;

        /// <summary>
        /// Creates a new ObjectCountLabeler. This constructor should only be used by serialization. For creation from
        /// user code, use <see cref="ObjectCountLabeler(IdLabelConfig)"/>.
        /// </summary>
        public ObjectCountLabeler()
        {
        }

        /// <summary>
        /// Creates a new ObjectCountLabeler with the given <see cref="IdLabelConfig"/>.
        /// </summary>
        /// <param name="labelConfig">The label config for resolving the label for each object.</param>
        public ObjectCountLabeler(IdLabelConfig labelConfig)
        {
            if (labelConfig == null)
                throw new ArgumentNullException(nameof(labelConfig));

            m_LabelConfig = labelConfig;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "NotAccessedField.Local")]
        struct ClassCountValue
        {
            public int label_id;
            public string label_name;
            public uint count;
        }

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (labelConfig == null)
                throw new InvalidOperationException("The ObjectCountLabeler idLabelConfig field must be assigned");

            m_ObjectCountAsyncMetrics =  new Dictionary<int, AsyncMetric>();

            perceptionCamera.RenderedObjectInfosCalculated += (frameCount, objectInfo) =>
            {
                NativeArray<uint> objectCounts = ComputeObjectCounts(objectInfo);
                ObjectCountsComputed?.Invoke(frameCount, objectCounts, labelConfig.labelEntries);
                ProduceObjectCountMetric(objectCounts, m_LabelConfig.labelEntries, frameCount);
            };

            supportsVisualization = true;
            EnableVisualization(supportsVisualization);
        }

        /// <inheritdoc/>
        protected override void OnBeginRendering()
        {
            if (m_ObjectCountMetricDefinition.Equals(default))
            {
                m_ObjectCountMetricDefinition = DatasetCapture.RegisterMetricDefinition("object count",
                    m_LabelConfig.GetAnnotationSpecification(),
                    "Counts of objects for each label in the sensor's view", id: new Guid(objectCountMetricId));
            }

            m_ObjectCountAsyncMetrics[Time.frameCount] = perceptionCamera.SensorHandle.ReportMetricAsync(m_ObjectCountMetricDefinition);
        }

        NativeArray<uint> ComputeObjectCounts(NativeArray<RenderedObjectInfo> objectInfo)
        {
            var objectCounts = new NativeArray<uint>(m_LabelConfig.labelEntries.Count, Allocator.Temp);
            foreach (var info in objectInfo)
            {
                if (!m_LabelConfig.TryGetLabelEntryFromInstanceId(info.instanceId, out _, out var labelIndex))
                    continue;

                objectCounts[labelIndex]++;
            }

            return objectCounts;
        }

        void ProduceObjectCountMetric(NativeSlice<uint> counts, IReadOnlyList<IdLabelEntry> entries, int frameCount)
        {
            using (s_ClassCountCallback.Auto())
            {
                if (!m_ObjectCountAsyncMetrics.TryGetValue(frameCount, out var classCountAsyncMetric))
                    return;

                m_ObjectCountAsyncMetrics.Remove(frameCount);

                if (m_ClassCountValues == null || m_ClassCountValues.Length != entries.Count)
                    m_ClassCountValues = new ClassCountValue[entries.Count];

                for (var i = 0; i < entries.Count; i++)
                {
                    m_ClassCountValues[i] = new ClassCountValue()
                    {
                        label_id = entries[i].id,
                        label_name = entries[i].label,
                        count = counts[i]
                    };

                    if (IsVisualizationEnabled() && hud != null)
                    {
                        if (entryToLabelMap == null) entryToLabelMap = new Dictionary<string, string>();

                        if (!entryToLabelMap.ContainsKey(entries[i].label)) entryToLabelMap[entries[i].label] = entries[i].label + " Counts";
                        
                        hud.UpdateEntry(entryToLabelMap[entries[i].label], counts[i].ToString());
                    }
                }

                classCountAsyncMetric.ReportValues(m_ClassCountValues);
            }
        }
        
        /// <inheritdoc/>
        protected override void SetupVisualizationPanel(GameObject panel)
        {
            var toggle  = GameObject.Instantiate(Resources.Load<GameObject>("GenericToggle"));
            toggle.transform.SetParent(panel.transform);
            toggle.GetComponentInChildren<Text>().text = "Object Counts";
            toggle.GetComponent<Toggle>().onValueChanged.AddListener(enabled => {
                EnableVisualization(enabled);
            });

            hud = GetHud();
        }

        /// <inheritdoc/>
        override protected void OnVisualizerEnabled(bool enabled)
        {
            if (!enabled)
            {
                foreach (var e in entryToLabelMap)
                {
                    GetHud().RemoveEntry(e.Value);
                }
                entryToLabelMap.Clear();
            } 
        }
    }
}
