using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Bounding boxes for all of the labeled objects in a capture
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class BoundingBoxAnnotation : Annotation
    {
        /// <summary>
        /// The bounding boxes recorded by the annotator
        /// </summary>
        public List<BoundingBox> boxes { get; set; }

        /// <summary>
        /// Constructs a new BoundingBoxAnnotation.
        /// </summary>
        /// <param name="def">The bounding box annotation definition.</param>
        /// <param name="sensorId">The sensor id.</param>
        /// <param name="boxes">The list of captured bounding boxes.</param>
        public BoundingBoxAnnotation(BoundingBoxDefinition def, string sensorId, List<BoundingBox> boxes)
            : base(def, sensorId)
        {
            this.boxes = boxes;
        }

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
