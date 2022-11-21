using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.MetadataReporter.Tags
{
    /// <summary>
    /// This tag allows to add transform information to the report
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth.ReportMetadata")]
    public class LabelingTransformDataMetadataTag : LabeledMetadataTag
    {
        /// <summary>
        /// Field to be set in Unity Editor. Once enabled - transform's rotation will be included to the report
        /// </summary>
        public bool rotationQuaternion;

        /// <summary>
        /// Field to be set in Unity Editor. Once enabled - transform's rotation will be included to the report
        /// </summary>
        public bool rotationEuler;

        /// <summary>
        /// Field to be set in Unity Editor. Once enabled - transform's position will be included to the report
        /// </summary>
        public bool position;

        /// <summary>
        /// Field to be set in Unity Editor. Once enabled - transform's parent name will be included to the report
        /// </summary>
        public bool parentName;

        /// <summary>
        /// Field to be set in Unity Editor. Once enabled - transform's Forward Vector will be included to the report
        /// </summary>
        public bool forwardVector;

        /// <summary>
        /// Field to be set in Unity Editor. Once enabled - transform's scale will be included to the report
        /// </summary>
        public bool scale;

        /// <inheritdoc />
        protected override string key => "transformRecord";

        /// <inheritdoc />
        protected override void GetReportedValues(IMessageBuilder builder)
        {
            if (rotationQuaternion)
            {
                builder.AddFloatArray("rotationQuaternion", MessageBuilderUtils.ToFloatVector(gameObject.transform.rotation));
                builder.AddFloatArray("localRotationQuaternion", MessageBuilderUtils.ToFloatVector(gameObject.transform.localRotation));
            }

            if (rotationEuler)
            {
                builder.AddFloatArray("rotationEuler", MessageBuilderUtils.ToFloatVector(gameObject.transform.eulerAngles));
                builder.AddFloatArray("localRotationEuler", MessageBuilderUtils.ToFloatVector(gameObject.transform.localEulerAngles));
            }

            if (position)
            {
                builder.AddFloatArray("position", MessageBuilderUtils.ToFloatVector(gameObject.transform.position));
                builder.AddFloatArray("positionLocal", MessageBuilderUtils.ToFloatVector(gameObject.transform.localPosition));
            }

            if (parentName)
            {
                builder.AddString("parentName", (gameObject.transform.parent == null ? "none" : transform.parent.name));
            }

            if (forwardVector)
            {
                builder.AddFloatArray("forwardVector", MessageBuilderUtils.ToFloatVector(gameObject.transform.forward));
            }

            if (scale)
            {
                builder.AddFloatArray("lossyScale", MessageBuilderUtils.ToFloatVector(gameObject.transform.lossyScale));
                builder.AddFloatArray("localScale", MessageBuilderUtils.ToFloatVector(gameObject.transform.localScale));
            }
        }
    }
}
