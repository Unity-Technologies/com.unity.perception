using System;
using UnityEngine.Perception.GroundTruth.LabelManagement;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.MetadataReporter
{
    /// <summary>
    /// Base class to link group output data by instance id
    /// </summary>
    [RequireComponent(typeof(Labeling))]
    [MovedFrom("UnityEngine.Perception.GroundTruth.ReportMetadata")]
    public abstract class LabeledMetadataTag : MetadataTag
    {
        /// <inheritdoc />
        internal sealed override string instanceId => GetComponent<Labeling>()?.instanceId.ToString();
    }
}

namespace UnityEngine.Perception.GroundTruth.ReportMetadata
{
    [Obsolete("(UnityUpgradable) -> UnityEngine.Perception.GroundTruth.MetadataReporter.LabeledMetadataTag", true)]
    public abstract class LabelMetadataTag {}
}
