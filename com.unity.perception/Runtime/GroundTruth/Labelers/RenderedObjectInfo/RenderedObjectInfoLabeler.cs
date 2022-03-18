using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Labeler which produces label id, instance id, and visible pixel count in a single metric each frame for
    /// each object which takes up one or more pixels in the camera's frame.
    /// </summary>
    [Serializable]
    public sealed class RenderedObjectInfoLabeler : CameraLabeler
    {
        /// <summary>
        /// Rendered object info entry
        /// </summary>
        struct MetricEntry : IMessageProducer
        {
            public int labelID;
            public uint instanceID;
            public Color32 instanceColor;
            public int visiblePixels;

            /// <inheritdoc/>
            public void ToMessage(IMessageBuilder builder)
            {
                builder.AddInt("labelId", labelID);
                builder.AddUInt("instanceId", instanceID);
                builder.AddIntArray("color", MessageBuilderUtils.ToIntVector(instanceColor));
                builder.AddInt("visiblePixels", visiblePixels);
            }
        }

        /// <summary>
        /// The metric ID
        /// </summary>
        public string objectInfoMetricId = "RenderedObjectInfo";

        /// <inheritdoc />
        public override string labelerId => objectInfoMetricId;

        static readonly string k_Description = "Produces label id, instance id, and visible pixel count in a single metric each frame for each object which takes up one or more pixels in the camera's frame, based on this labeler's associated label configuration.";

        ///<inheritdoc/>
        public override string description => k_Description;

        static ProfilerMarker s_ProduceRenderedObjectInfoMetric = new ProfilerMarker("ProduceRenderedObjectInfoMetric");

        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        [FormerlySerializedAs("labelingConfiguration")]
        public IdLabelConfig idLabelConfig;

        IMessageProducer[] m_VisiblePixelsValues;

        Dictionary<int, AsyncFuture<Metric>> m_ObjectInfoAsyncMetrics;
        MetricDefinition m_Definition;



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
            if (idLabelConfig == null)
                throw new ArgumentNullException(nameof(idLabelConfig));

            this.idLabelConfig = idLabelConfig;
        }

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("RenderedObjectInfoLabeler's idLabelConfig field must be assigned");

            m_ObjectInfoAsyncMetrics = new Dictionary<int, AsyncFuture<Metric>>();

            perceptionCamera.RenderedObjectInfosCalculated += (frameCount, objectInfo) =>
            {
                ProduceRenderedObjectInfoMetric(objectInfo, frameCount);
            };

            m_Definition = new RenderedObjectInfoMetricDefinition(objectInfoMetricId, k_Description, idLabelConfig.GetAnnotationSpecification());

            DatasetCapture.RegisterMetric(m_Definition);

            visualizationEnabled = supportsVisualization;
        }

        /// <inheritdoc/>
        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            m_ObjectInfoAsyncMetrics[Time.frameCount] = perceptionCamera.SensorHandle.ReportMetricAsync(m_Definition);
        }

        void ProduceRenderedObjectInfoMetric(NativeArray<RenderedObjectInfo> renderedObjectInfos, int frameCount)
        {
            using (s_ProduceRenderedObjectInfoMetric.Auto())
            {
                if (!m_ObjectInfoAsyncMetrics.TryGetValue(frameCount, out var metric))
                    return;

                m_ObjectInfoAsyncMetrics.Remove(frameCount);

                if (m_VisiblePixelsValues == null || m_VisiblePixelsValues.Length != renderedObjectInfos.Length)
                    m_VisiblePixelsValues = new IMessageProducer[renderedObjectInfos.Length];

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

                    m_VisiblePixelsValues[i] = new MetricEntry
                    {
                        labelID = labelEntry.id,
                        instanceID = objectInfo.instanceId,
                        visiblePixels = objectInfo.pixelCount,
                        instanceColor = objectInfo.instanceColor
                    };

                    if (visualize)
                    {
                        var label = labelEntry.label + "_" + objectInfo.instanceId;
                        hudPanel.UpdateEntry(this, label, objectInfo.pixelCount.ToString());
                    }
                }

                var (seq, step) = DatasetCapture.GetSequenceAndStepFromFrame(frameCount);
                var payload = new GenericMetric(m_VisiblePixelsValues.Where(x => x != null).ToArray(), m_Definition, perceptionCamera.ID);
                metric.Report(payload);
            }
        }

        bool TryGetLabelEntryFromInstanceId(uint instanceId, out IdLabelEntry labelEntry)
        {
            return idLabelConfig.TryGetLabelEntryFromInstanceId(instanceId, out labelEntry);
        }

        /// <inheritdoc/>
        protected override void OnVisualizerEnabledChanged(bool isEnabled)
        {
            if (isEnabled) return;
            hudPanel.RemoveEntries(this);
        }
    }
}
