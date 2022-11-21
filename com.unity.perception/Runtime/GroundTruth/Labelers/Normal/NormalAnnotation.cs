using System;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// The vertex normal image recorded for a capture.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class NormalAnnotation : Annotation
    {
        /// <summary>
        /// gets or sets the image format of a normal image
        /// </summary>
        public ImageEncodingFormat imageFormat { get; set; }
        /// <summary>
        /// gets or sets the dimension of a normal image
        /// </summary>
        public Vector2 dimension { get; set; }
        /// <summary>
        /// gets or sets the bytes of a normal image
        /// </summary>
        public byte[] buffer { get; set; }

        /// <summary>
        /// Add image information about the normal image to message builder
        /// </summary>
        /// <param name="builder"></param>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("imageFormat", imageFormat.ToString());
            builder.AddFloatArray("dimension", new[] { dimension.x, dimension.y });
            var key = $"{sensorId}.{annotationId}";
            builder.AddEncodedImage(key, "exr", buffer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NormalAnnotation"/> class.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="sensorId"></param>
        /// <param name="imageFormat"></param>
        /// <param name="dimension"></param>
        /// <param name="buffer"></param>
        public NormalAnnotation(
            NormalDefinition definition, string sensorId, ImageEncodingFormat imageFormat, Vector2 dimension, byte[] buffer)
            : base(definition, sensorId)
        {
            this.imageFormat = imageFormat;
            this.dimension = dimension;
            this.buffer = buffer;
        }
    }
}
