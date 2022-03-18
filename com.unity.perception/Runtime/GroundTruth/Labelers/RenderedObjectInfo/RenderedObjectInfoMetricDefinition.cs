namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Definition of the metric.
    /// </summary>
    public class RenderedObjectInfoMetricDefinition : DataModel.MetricDefinition
    {
        public const string metricType = "type.unity.com/unity.solo.RenderedObjectInfoMetric";

        /// <summary>
        /// Label config entries
        /// </summary>
        public IdLabelConfig.LabelEntrySpec[] spec { get; }

        internal RenderedObjectInfoMetricDefinition(string id, string description, IdLabelConfig.LabelEntrySpec[] spec)
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
