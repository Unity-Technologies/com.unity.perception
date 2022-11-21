using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.MetadataReporter.Tags
{
    /// <summary>
    /// This tag allows to add child GameObject name to the main Labeling Object report
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth.ReportMetadata")]
    public class LabelingChildNameMetadataTag : MetadataTag
    {
        /// <summary>
        /// Parent object to link with
        /// </summary>
        public Labeling labelingObject;

        /// <inheritdoc />
        protected override string key => "childGameObjectName";

        /// <inheritdoc />
        internal override string instanceId => labelingObject != null ? labelingObject.instanceId.ToString() : string.Empty;

        /// <inheritdoc />
        protected override void GetReportedValues(IMessageBuilder builder)
        {
            builder.AddString("name", gameObject.name);
        }
    }
}
