using System;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// The depth image recorded for a capture.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class DepthAnnotation : Annotation
    {
        /// <summary>
        /// The measurement strategy used to capture the depth image.
        /// </summary>
        public DepthMeasurementStrategy measurementStrategy { get; set; }

        /// <summary>
        /// The encoding format (png, exr, etc.) of the depth image.
        /// </summary>
        public ImageEncodingFormat imageFormat { get; set; }

        /// <summary>
        /// The range image's width and height in pixels.
        /// </summary>
        public Vector2 dimension { get; set; }

        /// <summary>
        /// The encoded range image data.
        /// </summary>
        public byte[] buffer { get; set; }

        /// <summary>
        /// Add image information about the depth image to message builder
        /// </summary>
        /// <param name="builder">The capture message to nest this annotation within.</param>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("measurementStrategy", measurementStrategy.ToString());
            builder.AddString("imageFormat", imageFormat.ToString());
            builder.AddFloatArray("dimension", new[] { dimension.x, dimension.y });
            var key = $"{sensorId}.{annotationId}";
            builder.AddEncodedImage(key, "exr", buffer);
        }

        /// <summary>
        /// Constructs a new <see cref="DepthAnnotation"/>.
        /// </summary>
        /// <param name="definition">The depth annotation definition.</param>
        /// <param name="sensorId">The sensor's string id.</param>
        /// <param name="measurementStrategy">The measurement strategy used to capture the depth image.</param>
        /// <param name="imageFormat">The encoding format of the depth image.</param>
        /// <param name="dimension">The width and height of the depth image in pixels.</param>
        /// <param name="buffer">The encoded range image data.</param>
        public DepthAnnotation(
            DepthDefinition definition,
            string sensorId,
            DepthMeasurementStrategy measurementStrategy,
            ImageEncodingFormat imageFormat,
            Vector2 dimension,
            byte[] buffer)
            : base(definition, sensorId)
        {
            this.measurementStrategy = measurementStrategy;
            this.imageFormat = imageFormat;
            this.dimension = dimension;
            this.buffer = buffer;
        }
    }
}
