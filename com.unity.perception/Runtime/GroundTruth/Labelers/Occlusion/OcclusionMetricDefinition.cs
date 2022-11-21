using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    class OcclusionMetricDefinition : DataModel.MetricDefinition
    {
        const string k_MetricType = "type.unity.com/unity.solo.OcclusionMetric";

        public OcclusionMetricDefinition(string id, string description) : base(k_MetricType, id, description) {}
    }
}
