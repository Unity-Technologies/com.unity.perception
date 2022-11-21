using System.Collections.Generic;

namespace UnityEngine.Perception.GroundTruth.DataModel
{
    /// <summary>
    /// The top level structure that holds all of the artifacts of a simulation
    /// frame. This is only reported after all of the captures, annotations, and
    /// metrics are ready to report for a single frame.
    /// </summary>
    public class Frame : DataModelElement
    {
        /// <summary>
        /// Data model type for Frame
        /// </summary>
        public override string modelType => "type.unity.com/unity.solo.Frame";

        /// <summary>
        /// The perception frame number of this record
        /// </summary>
        public int frame { get; }
        /// <summary>
        /// The sequence that this record is a part of
        /// </summary>
        public int sequence { get; }
        /// <summary>
        /// The step in the sequence that this record is a part of
        /// </summary>
        public int step { get; }
        /// <summary>
        /// The timestamp of the frame
        /// </summary>
        public float timestamp { get; }
        /// <summary>
        /// A list of all of the metrics recorded recorded for the frame.
        /// </summary>
        public List<Metric> metrics { get; }
        /// <summary>
        /// A list of all of the sensor captures recorded for the frame.
        /// </summary>
        public List<Sensor> sensors { get; set; }

        /// <summary>
        /// Creates a new frame.
        /// </summary>
        /// <param name="frame">The simulation frame ID</param>
        /// <param name="sequence">The sequence ID</param>
        /// <param name="step">The step inside the sequence of the frame</param>
        /// <param name="timestamp">The timestamp of the frame</param>
        public Frame(int frame, int sequence, int step, float timestamp) : base($"{sequence}_{step}")
        {
            this.frame = frame;
            this.sequence = sequence;
            this.step = step;
            this.timestamp = timestamp;
            metrics = new List<Metric>();
            sensors = new List<Sensor>();
        }

        /// <inheritdoc />
        public override void ToMessage(IMessageBuilder builder)
        {
            // purposefully not calling base ToMessage, because we do not need to record the string ID to the message

            builder.AddInt("frame", frame);
            builder.AddInt("sequence", sequence);
            builder.AddInt("step", step);
            builder.AddFloat("timestamp", timestamp);

            foreach (var s in sensors)
            {
                var nested = builder.AddNestedMessageToVector("captures");
                s.ToMessage(nested);
            }

            foreach (var m in metrics)
            {
                var nested = builder.AddNestedMessageToVector("metrics");
                m.ToMessage(nested);
            }
        }

        /// <inheritdoc />
        public override bool IsValid()
        {
            return frame > -1 && sequence > -1 && step > -1;
        }
    }
}
