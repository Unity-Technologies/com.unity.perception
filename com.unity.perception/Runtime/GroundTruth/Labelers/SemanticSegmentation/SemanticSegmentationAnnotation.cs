using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    public class SemanticSegmentationAnnotation : Annotation
    {
        public IEnumerable<SemanticSegmentationDefinitionEntry> instances { get; set; }
        public ImageEncodingFormat imageFormat { get; set; }
        public Vector2 dimension { get; set; }
        public byte[] buffer { get; set; }

        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("imageFormat", imageFormat.ToString());
            builder.AddFloatArray("dimension", new[] { dimension.x, dimension.y });
            var key = $"{sensorId}.{annotationId}";
            builder.AddByteArray(key, buffer);
            foreach (var i in instances)
            {
                var nested = builder.AddNestedMessageToVector("instances");
                i.ToMessage(nested);
            }
        }

        public SemanticSegmentationAnnotation(
            SemanticSegmentationDefinition definition, string sensorId, ImageEncodingFormat imageFormat, Vector2 dimension,
            IEnumerable<SemanticSegmentationDefinitionEntry> instances, byte[] buffer)
            : base(definition, sensorId)
        {
            this.imageFormat = imageFormat;
            this.dimension = dimension;
            this.instances = instances;
            this.buffer = buffer;
        }
    }
}
