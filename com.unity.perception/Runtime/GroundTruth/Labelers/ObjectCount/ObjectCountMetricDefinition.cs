using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Metric definition for object counts
    /// </summary>
    public class ObjectCountMetricDefinition : MetricDefinition
    {
        public const string metricType = "type.unity.com/unity.solo.ObjectCountMetric";

        /// <summary>
        /// Label config entries
        /// </summary>
        public IdLabelConfig.LabelEntrySpec[] spec { get; }

        internal ObjectCountMetricDefinition(string id, string description, IdLabelConfig.LabelEntrySpec[] spec)
            : base(metricType, id, description)
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
