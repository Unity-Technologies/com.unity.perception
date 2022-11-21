using System;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Annotation definition for the <see cref="PixelPositionLabeler" />.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class PixelPositionDefinition : AnnotationDefinition
    {
        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.PixelPositionAnnotation";
        internal const string labelerDescription = "Generates a pixelized position image where RGB channels denote the " +
            "XYZ components of the vector from the camera to the object at a pixel respectively.";

        /// <inheritdoc/>
        public override string description => labelerDescription;

        internal PixelPositionDefinition(string id) : base(id) {}

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            builder.AddString("description", description);
        }
    }
}
