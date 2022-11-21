using System;
using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// The instance segmentation image recorded for a capture. This
    /// includes the data that associates a pixel color to an object.
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class InstanceSegmentationAnnotation : Annotation
    {
        internal InstanceSegmentationAnnotation(
            InstanceSegmentationDefinition def, string sensorId,
            IEnumerable<InstanceSegmentationEntry> instances,
            ImageEncodingFormat imageFormat,
            Vector2 dimension,
            byte[] buffer
        ) : base(def, sensorId)
        {
            this.instances = instances;
            this.imageFormat = imageFormat;
            this.dimension = dimension;
            this.buffer = buffer;
        }

        /// <summary>
        /// This instance to pixel map.
        /// </summary>
        public IEnumerable<InstanceSegmentationEntry> instances { get; private set; }

        /// <summary>
        /// The format of the image type.
        /// </summary>
        public ImageEncodingFormat imageFormat { get; private set; }

        /// <summary>
        /// The dimensions (width, height) of the image.
        /// </summary>
        public Vector2 dimension { get; private set; }

        /// <summary>
        /// The raw bytes of the image file.
        /// </summary>
        public byte[] buffer { get; private set; }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("imageFormat", imageFormat.ToString());
            builder.AddFloatArray("dimension", new[] { dimension.x, dimension.y });
            var key = $"{sensorId}.{annotationId}";
            builder.AddEncodedImage(key, "png", buffer);

            foreach (var e in instances)
            {
                var nested = builder.AddNestedMessageToVector("instances");
                e.ToMessage(nested);
            }
        }
    }
}
