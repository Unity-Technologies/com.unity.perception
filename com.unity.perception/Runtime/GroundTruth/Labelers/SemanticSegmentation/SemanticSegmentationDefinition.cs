using System.Collections.Generic;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Annotation definition for semantic segmentation
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
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

        /// <summary>
        /// The list of all color-to-string-label mappings in the dataset for this semantic segmentation definition.
        /// </summary>
        public IReadOnlyList<SemanticSegmentationDefinitionEntry> spec;

        internal SemanticSegmentationDefinition(string id, IReadOnlyList<SemanticSegmentationDefinitionEntry> spec)
            : base(id)
        {
            this.spec = spec;
        }
    }
}
