using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth.Consumers
{
    public class NoOutputEndpoint : IConsumerEndpoint
    {
        /// <inheritdoc/>
        public string description => "Quiet passthrough endpoint. It does nothing with the data.";

        /// <inheritdoc/>
        public object Clone()
        {
            return this;
        }

        /// <inheritdoc/>
        public void SimulationStarted(SimulationMetadata metadata)
        {
            Debug.Log("The simulation started without a consumer endpoint, no data is being generated");
        }

        /// <inheritdoc/>
        public void SensorRegistered(SensorDefinition sensor)
        {
        }

        /// <inheritdoc/>
        public void AnnotationRegistered(AnnotationDefinition annotationDefinition)
        {
        }

        /// <inheritdoc/>
        public void MetricRegistered(MetricDefinition metricDefinition)
        {
        }

        /// <inheritdoc/>
        public void FrameGenerated(Frame frame)
        {
            // do nothing :-)
        }

        /// <inheritdoc/>
        public void SimulationCompleted(SimulationMetadata metadata)
        {
            Debug.Log("The simulation completed without a consumer endpoint. No data has been written.");
        }
    }
}
