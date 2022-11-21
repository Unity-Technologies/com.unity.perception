using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Definition of the metric.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class RenderedObjectInfoMetricDefinition : DataModel.MetricDefinition
    {
        const string k_MetricType = "type.unity.com/unity.solo.RenderedObjectInfoMetric";

        /// <summary>
        /// Label config entries
        /// </summary>
        public IdLabelConfig.LabelEntrySpec[] spec { get; }

        internal RenderedObjectInfoMetricDefinition(string id, string description, IdLabelConfig.LabelEntrySpec[] spec)
            : base(k_MetricType, id, description)
        {
            this.spec = spec;
        }

        /// <inheritdoc/>
        public override bool IsValid()
        {
            return base.IsValid() && spec != null;
        }
    }
}
