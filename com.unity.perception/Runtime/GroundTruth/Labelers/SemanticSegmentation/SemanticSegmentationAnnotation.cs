using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// An annotation recording information pertaining to a semantic segmentation capture.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class SemanticSegmentationAnnotation : Annotation
    {
        /// <summary>
        /// The colors and their associated string labels that are present within this semantic segmentation capture.
        /// </summary>
        public IReadOnlyList<SemanticSegmentationDefinitionEntry> instances { get; set; }

        /// <summary>
        /// The image encoding format of the segmentation image.
        /// </summary>
        public ImageEncodingFormat imageFormat { get; set; }

        /// <summary>
        /// The width and height of the segmentation image.
        /// </summary>
        public Vector2 dimension { get; set; }

        /// <summary>
        /// The encoded semantic segmentation image data.
        /// </summary>
        public byte[] buffer { get; set; }

        /// <summary>
        /// Constructs a new SemanticSegmentationAnnotation.
        /// </summary>
        /// <param name="definition">The semantic segmentation annotation definition.</param>
        /// <param name="sensorId">The sensor id.</param>
        /// <param name="imageFormat">The image encoding format.</param>
        /// <param name="dimension">The image dimensions (width and height).</param>
        /// <param name="instances">The colors/labels present in the segmentation image.</param>
        /// <param name="buffer">The encoded semantic segmentation image data.</param>
        public SemanticSegmentationAnnotation(
            SemanticSegmentationDefinition definition, string sensorId, ImageEncodingFormat imageFormat, Vector2 dimension,
            IReadOnlyList<SemanticSegmentationDefinitionEntry> instances, byte[] buffer)
            : base(definition, sensorId)
        {
            this.imageFormat = imageFormat;
            this.dimension = dimension;
            this.instances = instances;
            this.buffer = buffer;
        }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("imageFormat", imageFormat.ToString());
            builder.AddFloatArray("dimension", new[] { dimension.x, dimension.y });
            var key = $"{sensorId}.{annotationId}";
            builder.AddEncodedImage(key, "png", buffer);
            foreach (var i in instances)
            {
                var nested = builder.AddNestedMessageToVector("instances");
                i.ToMessage(nested);
            }
        }
    }
}
