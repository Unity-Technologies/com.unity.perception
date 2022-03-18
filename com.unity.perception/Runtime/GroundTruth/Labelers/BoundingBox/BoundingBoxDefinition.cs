using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// The annotation definition of a bounding box labeler
    /// </summary>
    public class BoundingBoxDefinition : AnnotationDefinition
    {
        internal const string labelerDescription = "Produces 2D bounding box annotations for all visible objects that bear a label defined in this labeler's associated label configuration.";

        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.BoundingBox2DAnnotation";

        /// <inheritdoc/>
        public override string description => labelerDescription;

        internal BoundingBoxDefinition(string id, IdLabelConfig.LabelEntrySpec[] spec)
            : base(id)
        {
            this.spec = spec;
        }

        /// <summary>
        /// The registered labels
        /// </summary>
        public IdLabelConfig.LabelEntrySpec[] spec { get; }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            foreach (var e in spec)
            {
                var nested = builder.AddNestedMessageToVector("spec");
                e.ToMessage(nested);
            }
        }
    }
}
