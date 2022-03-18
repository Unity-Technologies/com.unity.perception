using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Annotation definition for semantic segmentation
    /// </summary>
    public class SemanticSegmentationDefinition : AnnotationDefinition
    {
        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.SemanticSegmentationAnnotation";

        internal const string labelerDescription = "Generates a semantic segmentation image for each captured frame. " +
            "Each object is rendered to the semantic segmentation image using the color associated with it based on " +
            "this labeler's associated semantic segmentation label configuration. Semantic segmentation images are saved " +
            "to the dataset in PNG format. Please note that only one SemanticSegmentationLabeler can render at once across all cameras.";

        /// <inheritdoc/>
        public override string description => labelerDescription;

        public IEnumerable<SemanticSegmentationDefinitionEntry> spec;

        internal SemanticSegmentationDefinition(string id, IEnumerable<SemanticSegmentationDefinitionEntry> spec)
            : base(id)
        {
            this.spec = spec;
        }
    }
}
