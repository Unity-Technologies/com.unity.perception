using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// The generated product of the labeler
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class BoundingBox3DAnnotation : Annotation
    {
        /// <summary>
        /// Public constructor for the BoundingBox3DAnnotation
        /// </summary>
        /// <param name="def">BoundingBox3DDefinition</param>
        /// <param name="sensorId">Sensor data was received from</param>
        /// <param name="boxes">List of BoundingBox on screen</param>
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
