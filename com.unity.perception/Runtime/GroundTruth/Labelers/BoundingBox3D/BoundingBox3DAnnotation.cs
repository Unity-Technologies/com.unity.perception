using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// The generated product of the labeler
    /// </summary>
    public class BoundingBox3DAnnotation : DataModel.Annotation
    {
        public BoundingBox3DAnnotation(BoundingBox3DDefinition def, string sensorId, List<BoundingBox3D> boxes)
            : base(def, sensorId)
        {
            this.boxes = boxes;
        }

        /// <summary>
        /// The bounding boxes recorded by the annotator
        /// </summary>
        public List<BoundingBox3D> boxes { get; }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            foreach (var e in boxes)
            {
                var nested = builder.AddNestedMessageToVector("values");
                e.ToMessage(nested);
            }
        }

        /// <inheritdoc/>
        public override bool IsValid() => true;
    }
}
