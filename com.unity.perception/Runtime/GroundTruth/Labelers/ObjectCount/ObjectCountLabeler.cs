using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Perception.GroundTruth.Sensors.Channels;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Labeler which produces object counts for each label in the associated <see cref="IdLabelConfig"/> each frame.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public sealed class ObjectCountLabeler : CameraLabeler
    {
        struct ObjectCountRecord : IMessageProducer
        {
            public int labelId;
            public string labelName;
            public int count;

            public void ToMessage(IMessageBuilder builder)
            {
                builder.AddInt("labelId", labelId);
                builder.AddString("labelName", labelName);
                builder.AddInt("count", count);
            }
        }

        /// <summary>
        /// Metric Id, that can be set via Unity editor UI
        /// </summary>
        public string objectCountMetricId = "ObjectCount";

        /// <inheritdoc />
        public override string labelerId => objectCountMetricId;

        static readonly string k_Description = "Produces object counts for each label defined in this labeler's associated label configuration.";

        ///<inheritdoc/>
        public override string description => k_Description;

        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        public IdLabelConfig labelConfig;

        /// <summary>
        /// Fired when the object counts are computed for a frame.
        /// </summary>
        public event Action<int, NativeSlice<uint>, IReadOnlyList<IdLabelEntry>> ObjectCountsComputed;

        static ProfilerMarker s_ClassCountCallback = new ProfilerMarker("OnClassLabelsReceived");

        IMessageProducer[] m_ClassCountValues;

        Dictionary<int, (AsyncFuture<Metric> metric, LabelEntryMatchCache cache)> m_AsyncMetrics;
        MetricDefinition m_Definition;

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

            this.labelConfig = labelConfig;
        }

        /// <inheritdoc/>
        protected override bool supportsVisualization => true;

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (labelConfig == null)
                throw new InvalidOperationException("The ObjectCountLabeler idLabelConfig field must be assigned");

            m_AsyncMetrics =  new Dictionary<int, (AsyncFuture<Metric>, LabelEntryMatchCache)>();

            perceptionCamera.EnableChannel<InstanceIdChannel>();
            perceptionCamera.RenderedObjectInfosCalculated += (frameCount, objectInfo, _) =>
            {
                var entryCache = m_AsyncMetrics[frameCount].cache;
                var objectCounts = ComputeObjectCounts(objectInfo, entryCache);
                ObjectCountsComputed?.Invoke(frameCount, objectCounts, labelConfig.labelEntries);
                ProduceObjectCountMetric(objectCounts, labelConfig.labelEntries, frameCount);
            };

            m_Definition = new ObjectCountMetricDefinition(objectCountMetricId, k_Description, labelConfig.GetAnnotationSpecification());

            DatasetCapture.RegisterMetric(m_Definition);
            visualizationEnabled = supportsVisualization;
        }

        /// <inheritdoc/>
        protected override void OnBeginRendering(ScriptableRenderContext scriptableRenderContext)
        {
            m_AsyncMetrics[Time.frameCount] = (
                perceptionCamera.SensorHandle.ReportMetricAsync(m_Definition),
                labelConfig.CreateLabelEntryMatchCache(Allocator.Temp)
            );
        }

        NativeArray<uint> ComputeObjectCounts(NativeArray<RenderedObjectInfo> objectInfo, LabelEntryMatchCache cache)
        {
            var objectCounts = new NativeArray<uint>(labelConfig.labelEntries.Count, Allocator.Temp);
            foreach (var info in objectInfo)
            {
                if (!cache.TryGetLabelEntryFromInstanceId(info.instanceId, out _, out var labelIndex))
                    continue;

                objectCounts[labelIndex]++;
            }

            return objectCounts;
        }

        void ProduceObjectCountMetric(NativeSlice<uint> counts, IReadOnlyList<IdLabelEntry> entries, int frameCount)
        {
            using (s_ClassCountCallback.Auto())
            {
                if (!m_AsyncMetrics.TryGetValue(frameCount, out var classCountAsyncMetric))
                    return;

                m_AsyncMetrics.Remove(frameCount);

                if (m_ClassCountValues == null || m_ClassCountValues.Length != entries.Count)
                    m_ClassCountValues = new IMessageProducer[entries.Count];

                var visualize = visualizationEnabled;

                if (visualize)
                {
                    // Clear out all of the old entries...
                    hudPanel.RemoveEntries(this);
                }

                for (var i = 0; i < entries.Count; i++)
                {
                    m_ClassCountValues[i] = new ObjectCountRecord
                    {
                        labelId = entries[i].id,
                        labelName = entries[i].label,
                        count = (int)counts[i]
                    };

                    // Only display entries with a count greater than 0
                    if (visualize && counts[i] > 0)
                    {
                        var label = entries[i].label + " Counts";
                        hudPanel.UpdateEntry(this, label, counts[i].ToString());
                    }
                }

                var(seq, step) = DatasetCapture.GetSequenceAndStepFromFrame(frameCount);

                var payload = new GenericMetric(m_ClassCountValues, m_Definition, perceptionCamera.id);
                classCountAsyncMetric.metric.Report(payload);
                classCountAsyncMetric.cache.Dispose();
            }
        }

        /// <inheritdoc/>
        protected override void OnVisualizerEnabledChanged(bool isEnabled)
        {
            if (isEnabled) return;

            if (hudPanel != null)
            {
                hudPanel.RemoveEntries(this);
            }
        }
    }
}
