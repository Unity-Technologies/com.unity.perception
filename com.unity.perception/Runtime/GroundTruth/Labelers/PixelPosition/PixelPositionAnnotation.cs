using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// The pixel position image recorded for a capture.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class PixelPositionAnnotation : Annotation
    {
        /// <summary>
        /// Get the image format of the output image
        /// </summary>
        public ImageEncodingFormat imageFormat { get; }
        /// <summary>
        /// Get the dimension (in pixels) of the output image
        /// </summary>
        public Vector2 dimension { get; }
        /// <summary>
        /// Get the byte buffer of the output image
        /// </summary>
        public byte[] buffer { get; }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("imageFormat", imageFormat.ToString());
            builder.AddFloatArray("dimension", new[] { dimension.x, dimension.y });
            var key = $"{sensorId}.{annotationId}";
            builder.AddEncodedImage(key, imageFormat.ToString().ToLower(), buffer);
        }

        /// <summary>
        /// Initialize a new instance of <see cref="PixelPositionAnnotation"/>.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="sensorId"></param>
        /// <param name="imageFormat"></param>
        /// <param name="dimension"></param>
        /// <param name="buffer"></param>
        public PixelPositionAnnotation(
            PixelPositionDefinition definition, string sensorId, ImageEncodingFormat imageFormat, Vector2 dimension, byte[] buffer)
            : base(definition, sensorId)
        {
            this.imageFormat = imageFormat;
            this.dimension = dimension;
            this.buffer = buffer;
        }
    }
}
