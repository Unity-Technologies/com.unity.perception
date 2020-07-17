using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Labeler which produces label id, instance id, and visible pixel count in a single metric each frame for
    /// each object which takes up one or more pixels in the camera's frame.
    /// </summary>
    [Serializable]
    public sealed class RenderedObjectInfoLabeler : CameraLabeler
    {
        // ReSharper disable InconsistentNaming
        struct RenderedObjectInfoValue
        {
            [UsedImplicitly]
            public int label_id;
            [UsedImplicitly]
            public uint instance_id;
            [UsedImplicitly]
            public int visible_pixels;
        }
        // ReSharper restore InconsistentNaming

        static ProfilerMarker s_ProduceRenderedObjectInfoMetric = new ProfilerMarker("ProduceRenderedObjectInfoMetric");

        /// <summary>
        /// The ID to use for visible pixels metrics in the resulting dataset
        /// </summary>
        public string objectInfoMetricId = "5BA92024-B3B7-41A7-9D3F-C03A6A8DDD01";

        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        [FormerlySerializedAs("labelingConfiguration")]
        public IdLabelConfig idLabelConfig;

        RenderedObjectInfoValue[] m_VisiblePixelsValues;
        Dictionary<int, AsyncMetric> m_ObjectInfoAsyncMetrics;
        MetricDefinition m_RenderedObjectInfoMetricDefinition;

        private HUDPanel hud = null;

        Dictionary<string, string> entryToLabelMap = null;

        /// <summary>
        /// Creates a new RenderedObjectInfoLabeler. Be sure to assign <see cref="idLabelConfig"/> before adding to a <see cref="PerceptionCamera"/>.
        /// </summary>
        public RenderedObjectInfoLabeler()
        {
        }
        /// <summary>
        /// Creates a new RenderedObjectInfoLabeler with an <see cref="IdLabelConfig"/>.
        /// </summary>
        /// <param name="idLabelConfig">The <see cref="IdLabelConfig"/> which associates objects with labels. </param>
        public RenderedObjectInfoLabeler(IdLabelConfig idLabelConfig)
        {
            this.idLabelConfig = idLabelConfig;
        }

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("RenderedObjectInfoLabeler's idLabelConfig field must be assigned");

            m_ObjectInfoAsyncMetrics = new Dictionary<int, AsyncMetric>();

            perceptionCamera.RenderedObjectInfosCalculated += (frameCount, objectInfo) =>
            {
                ProduceRenderedObjectInfoMetric(objectInfo, frameCount);
            };

            supportsVisualization = true;
            EnableVisualization(supportsVisualization);
        }

        /// <inheritdoc/>
        protected override void OnBeginRendering()
        {
            if (m_RenderedObjectInfoMetricDefinition.Equals(default))
            {
                m_RenderedObjectInfoMetricDefinition = DatasetCapture.RegisterMetricDefinition(
                    "rendered object info",
                    idLabelConfig.GetAnnotationSpecification(),
                    "Information about each labeled object visible to the sensor",
                    id: new Guid(objectInfoMetricId));
            }

            m_ObjectInfoAsyncMetrics[Time.frameCount] = perceptionCamera.SensorHandle.ReportMetricAsync(m_RenderedObjectInfoMetricDefinition);
        }

        void ProduceRenderedObjectInfoMetric(NativeArray<RenderedObjectInfo> renderedObjectInfos, int frameCount)
        {
            using (s_ProduceRenderedObjectInfoMetric.Auto())
            {
                if (!m_ObjectInfoAsyncMetrics.TryGetValue(frameCount, out var metric))
                    return;

                m_ObjectInfoAsyncMetrics.Remove(frameCount);

                if (m_VisiblePixelsValues == null || m_VisiblePixelsValues.Length != renderedObjectInfos.Length)
                    m_VisiblePixelsValues = new RenderedObjectInfoValue[renderedObjectInfos.Length];

                for (var i = 0; i < renderedObjectInfos.Length; i++)
                {
                    var objectInfo = renderedObjectInfos[i];
                    if (!TryGetLabelEntryFromInstanceId(objectInfo.instanceId, out var labelEntry))
                        continue;

                    m_VisiblePixelsValues[i] = new RenderedObjectInfoValue
                    {
                        label_id = labelEntry.id,
                        instance_id = objectInfo.instanceId,
                        visible_pixels = objectInfo.pixelCount
                    };

                    if (IsVisualizationEnabled() && hud != null)
                    {
                        if (entryToLabelMap == null) entryToLabelMap = new Dictionary<string, string>();

                        if (!entryToLabelMap.ContainsKey(labelEntry.label)) entryToLabelMap[labelEntry.label] = labelEntry.label + " Pixels";
                        
                        hud.UpdateEntry(entryToLabelMap[labelEntry.label], objectInfo.pixelCount.ToString());
                    }
                }

                metric.ReportValues(m_VisiblePixelsValues);
            }
        }

        bool TryGetLabelEntryFromInstanceId(uint instanceId, out IdLabelEntry labelEntry)
        {
            return idLabelConfig.TryGetLabelEntryFromInstanceId(instanceId, out labelEntry);
        }

        /// <inheritdoc/>
        protected override void SetupVisualizationPanel(GameObject panel)
        {
            var toggle  = GameObject.Instantiate(Resources.Load<GameObject>("GenericToggle"));
            toggle.transform.SetParent(panel.transform);
            toggle.GetComponentInChildren<Text>().text = "Pixel Counts";
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
