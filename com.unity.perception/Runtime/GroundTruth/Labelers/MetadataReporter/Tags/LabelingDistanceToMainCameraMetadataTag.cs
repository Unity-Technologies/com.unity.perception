using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.MetadataReporter.Tags
{
    /// <summary>
    /// Tag that add SqrMagnitude from labeling object to the main camera
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth.ReportMetadata")]
    public class LabelingDistanceToMainCameraMetadataTag : LabeledMetadataTag
    {
        /// <inheritdoc />
        protected override string key => "SqrMagnitudeToMainCamera";

        /// <inheritdoc />
        protected override void GetReportedValues(IMessageBuilder builder)
        {
            var distance = 0f;
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                distance = Vector3.SqrMagnitude(gameObject.transform.position - mainCamera.transform.position);
            }

            builder.AddFloat("sqrMagnitudeToMainCamera", distance);
        }
    }
}
