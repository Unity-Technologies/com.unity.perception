namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// Definition of an RGB Camera sensor.
    /// </summary>
    public class RgbSensorDefinition : SensorDefinition
    {
        public override string modelType => "type.unity.com/unity.solo.RGBCamera";

        public bool useAccumulation { get; set; }

        /// <inheritdoc/>
        public RgbSensorDefinition(string id, string modality, string description)
            : base(id, modality, description)
        {
            this.useAccumulation = false;
        }

        /// <inheritdoc/>
        public RgbSensorDefinition(string id, CaptureTriggerMode captureTriggerMode, string description, float firstCaptureFrame, int framesBetweenCaptures, bool manualSensorsAffectTiming, string modality, float simulationDeltaTime, bool useAccumulation = false)
            : base(id, captureTriggerMode, description, firstCaptureFrame, framesBetweenCaptures, manualSensorsAffectTiming, modality, simulationDeltaTime)
        {
            this.useAccumulation = useAccumulation;
        }
    }
}
