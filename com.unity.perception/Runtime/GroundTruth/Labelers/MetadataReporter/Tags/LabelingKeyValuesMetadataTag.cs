using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.MetadataReporter.Tags
{
    /// <summary>
    /// This tag allows to add any custom data set in the editor
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth.ReportMetadata")]
    public class LabelingKeyValuesMetadataTag : LabeledMetadataTag
    {
        /// <summary>
        /// Field to be set in Unity Editor
        /// </summary>
        public string reportKey = "Values";

        /// <summary>
        /// Field to be set in Unity Editor
        /// </summary>
        public string[] values;

        /// <inheritdoc />
        protected override string key => reportKey;

        /// <inheritdoc />
        protected override void GetReportedValues(IMessageBuilder builder)
        {
            builder.AddStringArray("Values", values);
        }
    }
}
