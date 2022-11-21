using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Annotation definition for Normal
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class NormalDefinition : AnnotationDefinition
    {
        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.NormalAnnotation";

        internal const string labelerDescription = "Produces an image capturing the vertex normals of objects within the frame.";

        /// <inheritdoc/>
        public override string description => labelerDescription;

        internal NormalDefinition(string id)
            : base(id)
        {
        }
    }
}
