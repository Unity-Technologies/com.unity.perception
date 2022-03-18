using System;
using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// The product of the keypoint labeler
    /// </summary>
    public class KeypointAnnotation : Annotation
    {
        internal KeypointAnnotation(AnnotationDefinition def, string sensorId, string templateId, List<KeypointComponent> entries)
            : base(def, sensorId)
        {
            this.templateId = templateId;
            this.entries = entries;
        }

        /// <summary>
        /// The template that the points are based on
        /// </summary>
        public string templateId { get; set; }
        public IEnumerable<KeypointComponent> entries { get; set; }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("templateId", templateId);
            foreach (var entry in entries)
            {
                var nested = builder.AddNestedMessageToVector("values");
                entry.ToMessage(nested);
            }
        }
    }
}
