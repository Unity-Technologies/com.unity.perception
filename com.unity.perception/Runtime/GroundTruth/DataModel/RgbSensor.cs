using System;
using Unity.Mathematics;

namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// The concrete class for an RGB sensor.
    /// </summary>
    public class RgbSensor : Sensor
    {
        /// <summary>
        /// Constructs a <see cref="RgbSensor"/>.
        /// </summary>
        /// <param name="definition">The id of the sensor.</param>
        public RgbSensor(RgbSensorDefinition definition)
            : base(definition)
        {
            buffer = Array.Empty<byte>();
        }

        /// <summary>
        /// Create a new sensor
        /// </summary>
        /// <param name="definition">The id of the sensor</param>
        /// <param name="position">The position of the sensor</param>
        /// <param name="rotation">The rotation of the sensor</param>
        /// <param name="encodingFormat">The encoding format of the captured image.</param>
        public RgbSensor(RgbSensorDefinition definition, Vector3 position, Quaternion rotation,
                         ImageEncodingFormat encodingFormat = ImageEncodingFormat.Png)
            : base(definition, position, rotation)
        {
            imageEncodingFormat = encodingFormat;
        }

        /// <summary>
        /// Create a new sensor
        /// </summary>
        /// <param name="definition">The id of the sensor</param>
        /// <param name="position">The position of the sensor</param>
        /// <param name="rotation">The rotation of the sensor</param>
        /// <param name="velocity">The velocity of the sensor</param>
        /// <param name="acceleration">The acceleration of the sensor</param>
        /// <param name="encodingFormat">The encoding format of the captured image.</param>
        public RgbSensor(RgbSensorDefinition definition, Vector3 position, Quaternion rotation, Vector3 velocity,
                         Vector3 acceleration, ImageEncodingFormat encodingFormat = ImageEncodingFormat.Png)
            : base(definition, position, rotation, velocity, acceleration)
        {
            imageEncodingFormat = encodingFormat;
        }

        /// <summary>
        /// Create a new sensor
        /// </summary>
        /// <param name="definition">The id of the sensor</param>
        /// <param name="position">The position of the sensor</param>
        /// <param name="rotation">The rotation of the sensor</param>
        /// <param name="projection">The projection of the image</param>
        /// <param name="dimension">The pixel dimensions of the image</param>
        /// <param name="encodingFormat">The format of the image</param>
        public RgbSensor(RgbSensorDefinition definition, Vector3 position, Quaternion rotation,
                         string projection, Vector2 dimension, ImageEncodingFormat encodingFormat = ImageEncodingFormat.Png)
            : base(definition, position, rotation)
        {
            imageEncodingFormat = encodingFormat;
            this.projection = projection;
            this.dimension = dimension;
            buffer = Array.Empty<byte>();
        }

        /// <summary>
        /// Create a new sensor
        /// </summary>
        /// <param name="definition">The id of the sensor</param>
        /// <param name="position">The position of the sensor</param>
        /// <param name="rotation">The rotation of the sensor</param>
        /// <param name="velocity">The velocity of the sensor</param>
        /// <param name="acceleration">The acceleration of the sensor</param>
        /// <param name="projection">The projection of the image</param>
        /// <param name="dimension">The pixel dimensions of the image</param>
        /// <param name="encodingFormat">The format of the image</param>
        public RgbSensor(RgbSensorDefinition definition, Vector3 position, Quaternion rotation, Vector3 velocity,
                         Vector3 acceleration, string projection, Vector2 dimension,
                         ImageEncodingFormat encodingFormat = ImageEncodingFormat.Png)
            : base(definition, position, rotation, velocity, acceleration)
        {
            imageEncodingFormat = encodingFormat;
            this.projection = projection;
            this.dimension = dimension;
            buffer = Array.Empty<byte>();
        }

        /// <summary>
        /// The projection of the image.
        /// </summary>
        public string projection { get; set; } = "Perspective";

        /// <summary>
        /// The 3x3 image intrinsic matrix
        /// </summary>
        public float3x3 matrix { get; set; } = float3x3.identity;

        /// <summary>
        /// The image file type
        /// </summary>
        public ImageEncodingFormat imageEncodingFormat { get; set; } = ImageEncodingFormat.Png;

        /// <summary>
        /// The dimensions (width, height) of the image
        /// </summary>
        public Vector2 dimension { get; set; }

        /// <summary>
        /// The raw bytes of the image file
        /// </summary>
        public byte[] buffer { get; set; }

        /// <inheritdoc />
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddEncodedImage(id, imageEncodingFormat.ToString().ToLower(), buffer);
            builder.AddString("imageFormat", imageEncodingFormat.ToString());
            builder.AddFloatArray("dimension", MessageBuilderUtils.ToFloatVector(dimension));
            builder.AddString("projection", projection);
            builder.AddTensor("matrix", TensorBuilder.ToTensor(matrix));
        }

        /// <inheritdoc />
        public override bool IsValid()
        {
            return base.IsValid() && buffer != null;
        }
    }
}
