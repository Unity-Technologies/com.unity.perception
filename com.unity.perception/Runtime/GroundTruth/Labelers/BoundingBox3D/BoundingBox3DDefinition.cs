using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// The annotation definition of a 3D bounding box labeler
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class BoundingBox3DDefinition : AnnotationDefinition
    {
        internal const string labelerDescription = "Produces 3D bounding box ground truth data for all visible objects that bear a label defined in this labeler's associated label configuration.";

        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.BoundingBox3DAnnotation";

        /// <inheritdoc/>
        public override string description => labelerDescription;

        /// <summary>
        /// Public constructor for BoundingBox3DDefinition
        /// </summary>
        /// <param name="id">Definition id</param>
        /// <param name="spec">Set of labels for the definition</param>
        public BoundingBox3DDefinition(string id, IdLabelConfig.LabelEntrySpec[] spec)
            : base(id)
        {
            this.spec = spec;
        }

        /// <summary>
        /// The current set of labels
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
