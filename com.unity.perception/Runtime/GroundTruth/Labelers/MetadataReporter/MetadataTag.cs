using System;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.MetadataReporter
{
    /// <summary>
    /// Abstract class that represent any future metadata that should be added to the frame report
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth.ReportMetadata")]
    public abstract class MetadataTag : MonoBehaviour
    {
        /// <summary>
        /// Unity function called once object is enabled in ths scene hierarchy
        /// </summary>
        protected void OnEnable()
        {
            MetadataReporterLabeler.RegisterTag(this);
        }

        /// <summary>
        /// Unity function called once object is disabled in ths scene hierarchy
        /// </summary>
        protected void OnDisable()
        {
            RemoveReporter();
        }

        /// <summary>
        /// Unity function called once object is destroyed
        /// </summary>
        protected void OnDestroy()
        {
            RemoveReporter();
        }

        void RemoveReporter()
        {
            MetadataReporterLabeler.RemoveTag(this);
        }

        /// <summary>
        /// Object name in the JSON
        /// </summary>
        /// <code>
        /// {
        ///     "instanceId":"instanceId"
        ///     "key":{
        ///        // values added in GetReportedValues()
        ///     }
        /// }
        /// </code>
        protected abstract string key { get; }

        /// <summary>
        /// Report data should be added in this method
        /// </summary>
        /// <param name="builder"></param>
        protected abstract void GetReportedValues(IMessageBuilder builder);

        /// <summary>
        /// This variable should represent metadata relation with any of instanced objects.
        /// If it is null or empty - it will be considered as as scene related metadata.
        /// </summary>
        internal virtual string instanceId { get; } = string.Empty;

        /// <summary>
        /// JSON output will be generated here
        /// </summary>
        /// <param name="builder"></param>
        internal void ToMessage(IMessageBuilder builder)
        {
            var nested = builder.AddNestedMessage(key);

            try
            {
                GetReportedValues(nested);
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception happened during generating the report data on object {name} error = {e}");
            }
        }
    }
}
