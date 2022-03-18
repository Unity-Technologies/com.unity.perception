using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Bounding boxes for all of the labeled objects in a capture
    /// </summary>
    [Serializable]
    public class BoundingBoxAnnotation : Annotation
    {
        public BoundingBoxAnnotation(BoundingBoxDefinition def, string sensorId, List<BoundingBox> boxes)
            : base(def, sensorId)
        {
            this.boxes = boxes;
        }

        /// <summary>
        /// The bounding boxes recorded by the annotator
        /// </summary>
        public List<BoundingBox> boxes { get; set; }

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
    }
}
