namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// The metric definition holds of the associated metadata for a metric.
    /// </summary>
    public class MetricDefinition : DataModelElement
    {
        string m_ModelType = "type.unity.com/unity.solo.GenericMetric";

        /// <inheritdoc />
        public override string modelType => m_ModelType;

        /// <summary>
        /// Creates a new metric definition.
        /// </summary>
        /// <param name="id">The ID of the metric definition.</param>
        /// <param name="description">The description of the metric.</param>
        public MetricDefinition(string id, string description) : base(id)
        {
            this.description = description;
        }

        /// <summary>
        /// Creates a new metric definition.
        /// </summary>
        /// <param name="modelType">The type of the metric.</param>
        /// <param name="id">The ID of the metric definition.</param>
        /// <param name="description">The description of the metric.</param>
        public MetricDefinition(string modelType, string id, string description) : base(id)
        {
            m_ModelType = modelType;
            this.description = description;
        }

        /// <summary>
        /// The description of the metric.
        /// </summary>
        public string description { get; }

        /// <inheritdoc />
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("description", description);
        }

        /// <inheritdoc />
        public override bool IsValid()
        {
            return base.IsValid() && !string.IsNullOrEmpty(description);
        }
    }
}
