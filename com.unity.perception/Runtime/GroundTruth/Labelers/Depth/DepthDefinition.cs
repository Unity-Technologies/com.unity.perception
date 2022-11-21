using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Define Depth labeler's model type and description
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class DepthDefinition : AnnotationDefinition
    {
        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.DepthAnnotation";

        internal const string labelerDescription = "Generates a 32-bit depth image in EXR format where each pixel contains the actual distance in Unity units (usually meters) from the camera to the object in the scene.";

        /// <inheritdoc/>
        public override string description => labelerDescription;

        internal DepthDefinition(string id)
            : base(id)
        {
        }
    }
}
