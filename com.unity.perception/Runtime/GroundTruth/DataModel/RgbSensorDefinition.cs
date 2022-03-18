namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// Definition of an RGB Camera sensor.
    /// </summary>
    public class RgbSensorDefinition : SensorDefinition
    {
        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.RGBCamera";
        /// <inheritdoc/>
        public RgbSensorDefinition(string id, string modality, string description)
            : base(id, modality, description) { }
        /// <inheritdoc/>
        public RgbSensorDefinition(string id, CaptureTriggerMode captureTriggerMode, string description, float firstCaptureFrame, int framesBetweenCaptures, bool manualSensorsAffectTiming, string modality, float simulationDeltaTime)
            : base(id, captureTriggerMode, description, firstCaptureFrame, framesBetweenCaptures, manualSensorsAffectTiming, modality, simulationDeltaTime) { }
    }
}
