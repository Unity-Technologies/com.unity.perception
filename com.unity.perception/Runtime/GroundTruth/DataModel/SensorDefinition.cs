namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// Capture trigger modes for sensors.
    /// </summary>
    public enum CaptureTriggerMode
    {
        /// <summary>
        /// Captures happen automatically based on a start frame and frame delta time.
        /// </summary>
        Scheduled,
        /// <summary>
        /// Captures should be triggered manually through calling the manual capture method of the sensor using this trigger
        /// mode.
        /// </summary>
        Manual
    }

    /// <summary>
    /// Definition class for sensors. <see cref="DataModel"/>
    /// </summary>
    public class SensorDefinition : DataModelElement
    {
        /// <inheritdoc />
        public override string modelType => "type.unity.com/unity.solo.SensorDefinition";

        /// <summary>
        /// The capture trigger model for the sensor. <see cref="captureTriggerMode"/>
        /// </summary>
        public CaptureTriggerMode captureTriggerMode;
        /// <summary>
        /// The description of the sensor.
        /// </summary>
        public string description;
        /// <summary>
        /// The first capture frame of the sensor.
        /// </summary>
        public float firstCaptureFrame;
        /// <summary>
        /// The number of frames between captures.
        /// </summary>
        public int framesBetweenCaptures;
        /// <summary>
        /// Does manual sensors affect timing?
        /// </summary>
        public bool manualSensorsAffectTiming;
        /// <summary>
        /// The mode of the sensor;
        /// </summary>
        public string modality;
        /// <summary>
        /// The simulation delta time.
        /// </summary>
        public float simulationDeltaTime;

        /// <summary>
        /// Constructor for the sensor definition.
        /// </summary>
        /// <param name="id">The id of the sensor</param>
        /// <param name="modality">The modality of the sensor</param>
        /// <param name="description">The description of the sensor</param>
        public SensorDefinition(string id, string modality, string description) : base(id)
        {
            this.modality = modality;
            this.description = description;
            firstCaptureFrame = 0;
            captureTriggerMode = CaptureTriggerMode.Scheduled;
            simulationDeltaTime = 0.0f;
            framesBetweenCaptures = 0;
            manualSensorsAffectTiming = false;
        }

        /// <summary>
        /// Constructor for the sensor definition.
        /// </summary>
        /// <param name="id">The id of the sensor</param>
        /// <param name="captureTriggerMode">The capture trigger mode of the sensor</param>
        /// <param name="description">The description of the sensor</param>
        /// <param name="firstCaptureFrame">The first capture frame</param>
        /// <param name="framesBetweenCaptures">Frames between captures</param>
        /// <param name="manualSensorsAffectTiming">Do manual sensors affect timing</param>
        /// <param name="modality">The modality of the sensor</param>
        /// <param name="simulationDeltaTime">The simulation delta time</param>
        public SensorDefinition(string id, CaptureTriggerMode captureTriggerMode, string description, float firstCaptureFrame, int framesBetweenCaptures, bool manualSensorsAffectTiming, string modality, float simulationDeltaTime)
            : base(id)
        {
            this.captureTriggerMode = captureTriggerMode;
            this.description = description;
            this.firstCaptureFrame = firstCaptureFrame;
            this.framesBetweenCaptures = framesBetweenCaptures;
            this.manualSensorsAffectTiming = manualSensorsAffectTiming;
            this.modality = modality;
            this.simulationDeltaTime = simulationDeltaTime;
        }

        /// <inheritdoc />
        public override void ToMessage(IMessageBuilder builder)
        {
            builder.AddString("@type", modelType);
            builder.AddString("id", id);
            builder.AddString("modality", modality);
            builder.AddString("description", description);
            builder.AddFloat("firstCaptureFrame", firstCaptureFrame);
            builder.AddString("captureTriggerMode", captureTriggerMode.ToString());
            builder.AddFloat("simulationDeltaTime", simulationDeltaTime);
            builder.AddInt("framesBetweenCaptures", framesBetweenCaptures);
            builder.AddBool("manualSensorsAffectTiming", manualSensorsAffectTiming);
        }

        /// <inheritdoc />
        public override bool IsValid()
        {
            return base.IsValid() && !string.IsNullOrEmpty(description);
        }
    }
}
