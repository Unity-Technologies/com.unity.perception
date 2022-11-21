namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// Abstract class that holds the common data found in all
    /// annotations. Concrete instances of this class will add
    /// data for their specific annotation type.
    /// </summary>
    public abstract class Annotation : DataModelElement
    {
        /// <summary>
        /// The annotation definition associated with this annotation.
        /// </summary>
        protected AnnotationDefinition m_Definition;

        /// <summary>
        /// The annotation ID.
        /// </summary>
        public string annotationId => m_Definition.id;

        /// <inheritdoc/>
        public override string modelType => m_Definition.modelType;

        /// <summary>
        /// The description of the annotation.
        /// </summary>
        public string description => m_Definition.description;

        /// <summary>
        /// The sensor that this annotation is associated with.
        /// </summary>
        public string sensorId { get; }

        /// <summary>
        /// Create a new annotation.
        /// </summary>
        /// <param name="definition">The definition of the annotation</param>
        /// <param name="sensorId">The ID of the sensor that recorded the image for this annotation</param>
        protected Annotation(AnnotationDefinition definition, string sensorId) : base(definition.id)
        {
            m_Definition = definition;
            this.sensorId = sensorId;
        }

        /// <inheritdoc />
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("sensorId", sensorId);
            builder.AddString("description", description);
        }

        /// <inheritdoc />
        public override bool IsValid()
        {
            return base.IsValid() && !string.IsNullOrEmpty(sensorId);
        }
    }
}
