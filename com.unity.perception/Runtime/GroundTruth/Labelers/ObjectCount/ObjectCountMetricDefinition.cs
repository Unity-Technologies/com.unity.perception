using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// Metric definition for object counts
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class ObjectCountMetricDefinition : MetricDefinition
    {
        const string k_MetricType = "type.unity.com/unity.solo.ObjectCountMetric";

        /// <summary>
        /// Label config entries
        /// </summary>
        public IdLabelConfig.LabelEntrySpec[] spec { get; }

        internal ObjectCountMetricDefinition(string id, string description, IdLabelConfig.LabelEntrySpec[] spec)
            : base(k_MetricType, id, description)
        {
            this.spec = spec;
        }

        /// <inheritdoc/>
        public override bool IsValid()
        {
            return base.IsValid() && spec != null;
        }
    }
}
