using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.MetadataReporter.Tags
{
    /// <summary>
    /// This tag allows to add GameObject name to the report
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth.ReportMetadata")]
    public class LabelingNameMetadataTag : LabeledMetadataTag
    {
        /// <inheritdoc />
        protected override string key => "gameObjectName";

        /// <inheritdoc />
        protected override void GetReportedValues(IMessageBuilder builder)
        {
            builder.AddString("name", gameObject.name);
        }
    }
}
