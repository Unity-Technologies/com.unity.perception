using System;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Base class for a consumer endpoint. A consumer endpoint acts on the data produced by the perception simulation.
    /// </summary>
    public interface IConsumerEndpoint : ICloneable
    {
        /// <summary>
        /// The human readable description of the endpoint
        /// </summary>
        public string description { get; }

        /// <summary>
        /// Checks to see if an endpoint is configured properly. If an endpoint is invalid the endpoint
        /// will not be able to properly produce generated data.
        /// </summary>
        /// <param name="errorMessage">If validation fails, this will be updated with an error message</param>
        /// <returns>True if validation is successful</returns>
        public bool IsValid(out string errorMessage);

        /// <summary>
        /// Called when the simulation begins. Provides simulation wide metadata to
        /// the consumer.
        /// </summary>
        /// <param name="metadata">Metadata describing the active simulation</param>
        public void SimulationStarted(SimulationMetadata metadata);

        /// <summary>
        /// Called when a sensor is registered with the perception engine.
        /// </summary>
        /// <param name="sensor">The registered sensor definition</param>
        public void SensorRegistered(SensorDefinition sensor);

        /// <summary>
        /// Called when an annotation is registered with the perception engine.
        /// </summary>
        /// <param name="annotationDefinition">The registered annotation definition</param>
        public void AnnotationRegistered(AnnotationDefinition annotationDefinition);

        /// <summary>
        /// Called when a metric is registered with the perception engine.
        /// </summary>
        /// <param name="metricDefinition">The registered metric definition</param>
        public void MetricRegistered(MetricDefinition metricDefinition);

        /// <summary>
        /// Called at the end of each frame. Contains all of the generated data for the
        /// frame. This method is called after the frame has entirely finished processing.
        /// </summary>
        /// <param name="frame">The frame data.</param>
        public void FrameGenerated(Frame frame);

        /// <summary>
        /// Called at the end of the simulation. Contains metadata describing the entire
        /// simulation process.
        /// </summary>
        /// <param name="metadata">Metadata describing the entire simulation process</param>
        public void SimulationCompleted(SimulationMetadata metadata);

        /// <summary>
        /// Call this method for endpoint to restore crash point and resume simulation to the same folder
        /// </summary>
        /// <returns>string - path to the folder. int - last generated frame</returns>
        /// <param name="maxFrameCount">maxFrameCount describes required amount of frames to be generated</param>
        public (string, int) ResumeSimulationFromCrash(int maxFrameCount);
    }
}
