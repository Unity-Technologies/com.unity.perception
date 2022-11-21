using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.MetadataReporter.Tags
{
    /// <summary>
    /// This tag allows to add light information to the report
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth.ReportMetadata")]
    [RequireComponent(typeof(Light))]
    public class LightMetadataTag : MetadataTag
    {
        Light m_Light;

        void Awake()
        {
            m_Light = GetComponent<Light>();
        }

        /// <inheritdoc />
        protected override string key => $"SceneLight-{name}";

        /// <inheritdoc />
        protected override void GetReportedValues(IMessageBuilder builder)
        {
            builder.AddIntArray("Color", MessageBuilderUtils.ToIntVector(m_Light.color));
            builder.AddFloat("Intensity", m_Light.intensity);
        }
    }
}
