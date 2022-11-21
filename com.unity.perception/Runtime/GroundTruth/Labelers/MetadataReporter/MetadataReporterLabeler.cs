using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.MetadataReporter;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth.ReportMetadata")]
    class MetadataReporterLabeler : CameraLabeler
    {
        public override string description => "Metadata labeler";

        /// <summary>
        /// The GUID id to associate with the metadata produced by this labeler.
        /// </summary>
        public string annotationId = "metadata";

        /// <inheritdoc />
        public override string labelerId => annotationId;

        protected override bool supportsVisualization => false;

        static List<MetadataTag> m_RegisteredReporters = new List<MetadataTag>();

        MetricDefinition m_FrameMetadataReporter;

        protected override void Setup()
        {
            m_FrameMetadataReporter = new MetricDefinition(labelerId, description);
            DatasetCapture.RegisterMetric(m_FrameMetadataReporter);
        }

        internal static void RegisterTag(MetadataTag tag)
        {
            m_RegisteredReporters.Add(tag);
        }

        internal static void RemoveTag(MetadataTag tag)
        {
            m_RegisteredReporters.Remove(tag);
        }

        protected override void OnEndRendering(ScriptableRenderContext scriptableRenderContext)
        {
            base.OnEndRendering(scriptableRenderContext);
            Report();
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            m_RegisteredReporters.Clear();
        }

        void Report()
        {
            var builder = new InMemoryMessageBuilder();
            GenerateFrameData(builder);
            DatasetCapture.ReportMetric(m_FrameMetadataReporter, new GenericMetric(builder, m_FrameMetadataReporter, annotationId: labelerId));
        }

        internal void GenerateFrameData(IMessageBuilder builder)
        {
            var dict = new Dictionary<string, List<MetadataTag>>();
            foreach (var reportTag in m_RegisteredReporters.Where(reportTag => reportTag != null))
            {
                var sceneReportId = string.Empty;

                try
                {
                    // null or empty - means scene level report
                    sceneReportId = reportTag.instanceId ?? string.Empty;
                }
                catch (Exception e)
                {
                    Debug.LogError($"exception happened on object {reportTag.name} during instanceId request {e}");
                }

                if (dict.TryGetValue(sceneReportId, out var reportsForInstance))
                {
                    reportsForInstance.Add(reportTag);
                    continue;
                }

                dict[sceneReportId] = new List<MetadataTag> { reportTag };
            }

            foreach (var reportsPerInstance in dict)
            {
                // scene level report
                if (reportsPerInstance.Key == string.Empty)
                {
                    foreach (var report in reportsPerInstance.Value)
                    {
                        report.ToMessage(builder);
                    }
                    continue;
                }

                var nested = builder.AddNestedMessageToVector("instances");

                foreach (var report in reportsPerInstance.Value)
                {
                    report.ToMessage(nested);
                    nested.AddString("instanceId", reportsPerInstance.Key);
                }
            }
        }
    }
}
