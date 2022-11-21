using System;

namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// Abstract class that holds the common data found in all
    /// metrics. Concrete instances of this class will add
    /// data for their specific metric type.
    /// </summary>
    public abstract class Metric : DataModelElement
    {
        MetricDefinition m_Definition;

        /// <inheritdoc />
        public override string modelType => m_Definition.modelType;

        /// <summary>
        /// The sensor ID that this metric is associated with, this value can be null or empty if the metric is
        /// not associated with any particular sensor.
        /// </summary>
        public string sensorId { get; internal set; }

        /// <summary>
        /// The annotation ID that this metric is associated with. If the value is none ("")
        /// then the metric is capture wide, and not associated with a specific annotation.
        /// </summary>
        public string annotationId { get; internal set; }

        /// <summary>
        /// Creates a new metric.
        /// </summary>
        /// <param name="definition">The metric definition</param>
        /// <param name="sensorId">The sensor ID of the sensor associated with this metric, this value can be empty</param>
        /// <param name="annotationId">The annotation ID of the annotation associated with this metric, this value can be empty</param>
        protected Metric(MetricDefinition definition, string sensorId = default, string annotationId = default) : base(definition.id)
        {
            m_Definition = definition;
            this.sensorId = sensorId;
            this.annotationId = annotationId;
        }

        /// <summary>
        /// Retrieves an array of the metric values.
        /// </summary>
        /// <typeparam name="T">Any metric value based on <see cref="IMessageProducer"/> </typeparam>
        /// <returns>Array of requested metrics</returns>
        public abstract T[] GetValues<T>();

        /// <inheritdoc />
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("sensorId", sensorId ?? string.Empty);
            builder.AddString("annotationId", annotationId ?? string.Empty);
            builder.AddString("description", m_Definition.description ?? string.Empty);
        }
    }
}
