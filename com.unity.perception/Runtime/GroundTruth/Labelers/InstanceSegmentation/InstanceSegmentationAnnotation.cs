using System;
using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// The instance segmentation image recorded for a capture. This
    /// includes the data that associates a pixel color to an object.
    /// </summary>
    [Serializable]
    public class InstanceSegmentationAnnotation : Annotation
    {
        internal InstanceSegmentationAnnotation(InstanceSegmentationDefinition def, string sensorId, List<InstanceSegmentationEntry> instances)
            : base(def, sensorId)
        {
            this.instances = instances;
        }

        /// <summary>
        /// This instance to pixel map
        /// </summary>
        public List<InstanceSegmentationEntry> instances { get; set; }

        // The format of the image type
        public ImageEncodingFormat imageFormat { get; set; }

        // The dimensions (width, height) of the image
        public Vector2 dimension { get; set; }

        // The raw bytes of the image file
        public byte[] buffer { get; set; }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("imageFormat", imageFormat.ToString());
            builder.AddFloatArray("dimension", new[] { dimension.x, dimension.y });
            var key = $"{sensorId}.{annotationId}";
            builder.AddByteArray(key, buffer);

            foreach (var e in instances)
            {
                var nested = builder.AddNestedMessageToVector("instances");
                e.ToMessage(nested);
            }
        }
    }
}
