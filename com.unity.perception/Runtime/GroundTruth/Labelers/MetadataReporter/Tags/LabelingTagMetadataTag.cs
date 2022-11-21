using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.MetadataReporter.Tags
{
    /// <summary>
    /// This tag allows to add GameObject tag to the report
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth.ReportMetadata")]
    public class LabelingTagMetadataTag : LabeledMetadataTag
    {
        /// <inheritdoc />
        protected override string key => "gameObjectTag";

        /// <inheritdoc />
        protected override void GetReportedValues(IMessageBuilder builder)
        {
            builder.AddString("unityTag", gameObject.tag);
        }
    }
}
