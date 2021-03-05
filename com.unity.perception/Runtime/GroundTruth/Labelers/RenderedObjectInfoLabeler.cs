using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Rendering;
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
        ///<inheritdoc/>
        public override string description
        {
            get => "Produces label id, instance id, and visible pixel count in a single metric each frame for each object which takes up one or more pixels in the camera's frame, based on this labeler's associated label configuration.";
            protected set {}
        }

        // ReSharper disable InconsistentNaming
        struct RenderedObjectInfoValue
        {
            [UsedImplicitly]
            public int label_id;
            [UsedImplicitly]
            public uint instance_id;
            [UsedImplicitly]
            public Color32 instance_color;
            [UsedImplicitly]
            public int visible_pixels;

        }
        // ReSharper restore InconsistentNaming

        static ProfilerMarker s_ProduceRenderedObjectInfoMetric = new ProfilerMarker("ProduceRenderedObjectInfoMetric");

        /// <summary>
        /// The ID to use for visible pixels metrics in the resulting dataset
        /// </summary>
        public string objectInfoMetricId = "5ba92024-b3b7-41a7-9d3f-c03a6a8ddd01";

        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        [FormerlySerializedAs("labelingConfiguration")]
        public IdLabelConfig idLabelConfig;

        RenderedObjectInfoValue[] m_VisiblePixelsValues;
        Dictionary<int, AsyncMetric> m_ObjectInfoAsyncMetrics;
        MetricDefinition m_RenderedObjectInfoMetricDefinition;

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
        protected override bool supportsVisualization => true;

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

            visualizationEnabled = supportsVisualization;
        }

        /// <param name="scriptableRenderContext"></param>
        /// <inheritdoc/>
        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
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

                var visualize = visualizationEnabled;

                if (visualize)
                {
                    // Clear out all of the old entries...
                    hudPanel.RemoveEntries(this);
                }

                for (var i = 0; i < renderedObjectInfos.Length; i++)
                {
                    var objectInfo = renderedObjectInfos[i];
                    if (!TryGetLabelEntryFromInstanceId(objectInfo.instanceId, out var labelEntry))
                        continue;

                    m_VisiblePixelsValues[i] = new RenderedObjectInfoValue
                    {
                        label_id = labelEntry.id,
                        instance_id = objectInfo.instanceId,
                        visible_pixels = objectInfo.pixelCount,
                        instance_color = objectInfo.instanceColor
                    };

                    if (visualize)
                    {
                        var label = labelEntry.label + "_" + objectInfo.instanceId;
                        hudPanel.UpdateEntry(this, label, objectInfo.pixelCount.ToString());
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
        protected override void OnVisualizerEnabledChanged(bool enabled)
        {
            if (enabled) return;
            hudPanel.RemoveEntries(this);
        }
    }
}
