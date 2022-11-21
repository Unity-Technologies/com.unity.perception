using System.Collections.Generic;

namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// Abstract sensor class that holds all of the common information for a sensor.
    /// </summary>
    public abstract class Sensor : DataModelElement
    {
        /// <summary>
        /// Holds sensor definition data
        /// </summary>
        protected SensorDefinition m_Definition;

        /// <inheritdoc />
        public override string modelType => m_Definition.modelType;

        /// <summary>
        /// Constructs a new sensor.
        /// </summary>
        /// <param name="definition"></param>
        protected Sensor(SensorDefinition definition)
            : base(definition.id)
        {
            m_Definition = definition;
        }

        /// <summary>
        /// Create a new sensor
        /// </summary>
        /// <param name="definition">The definition of the sensor</param>
        /// <param name="position">The position of the sensor</param>
        /// <param name="rotation">The rotation of the sensor</param>
        protected Sensor(SensorDefinition definition, Vector3 position, Quaternion rotation)
            : base(definition.id)
        {
            m_Definition = definition;
            this.position = position;
            this.rotation = rotation;
            velocity = Vector3.zero;
            acceleration = Vector3.zero;
            annotations = new List<Annotation>();
        }

        /// <summary>
        /// Create a new sensor
        /// </summary>
        /// <param name="definition">The definition of the sensor</param>
        /// <param name="position">The position of the sensor</param>
        /// <param name="rotation">The rotation of the sensor</param>
        /// <param name="velocity">The velocity of the sensor</param>
        /// <param name="acceleration">The acceleration of the sensor</param>
        protected Sensor(SensorDefinition definition, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 acceleration)
            : base(definition.id)
        {
            m_Definition = definition;
            this.position = position;
            this.rotation = rotation;
            this.velocity = velocity;
            this.acceleration = acceleration;
            annotations = new List<Annotation>();
        }

        /// <summary>
        /// Description of the sensor.
        /// </summary>
        public string description => m_Definition.description;
        /// <summary>
        /// The position (xyz) of the sensor in the world.
        /// </summary>
        public Vector3 position { get; set; }
        /// <summary>
        /// The rotation in euler angles.
        /// </summary>
        public Quaternion rotation { get; set; }
        /// <summary>
        /// The current velocity (xyz) of the sensor.
        /// </summary>
        public Vector3 velocity { get; set; }
        /// <summary>
        /// The current acceleration (xyz) of the sensor.
        /// </summary>
        public Vector3 acceleration { get; set; }

        /// <summary>
        /// A list of all of the annotations recorded recorded for the frame.
        /// </summary>
        public List<Annotation> annotations { get; set; }

        /// <inheritdoc />
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("description", description);
            builder.AddFloatArray("position", MessageBuilderUtils.ToFloatVector(position));
            builder.AddFloatArray("rotation", MessageBuilderUtils.ToFloatVector(rotation));
            builder.AddFloatArray("velocity", MessageBuilderUtils.ToFloatVector(velocity));
            builder.AddFloatArray("acceleration", MessageBuilderUtils.ToFloatVector(acceleration));
            foreach (var annotation in annotations)
            {
                var nested = builder.AddNestedMessageToVector("annotations");
                annotation.ToMessage(nested);
            }
        }

        /// <inheritdoc/>
        public override bool IsValid()
        {
            return base.IsValid() && !string.IsNullOrEmpty(description);
        }
    }
}
