using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace Tests.Editor
{
    [Serializable]
    [HideFromCreateMenu]
    public class TestCollectorEndpoint : IConsumerEndpoint
    {
        public List<SensorDefinition> sensors = new List<SensorDefinition>();
        public List<AnnotationDefinition> annotationDefinitions = new List<AnnotationDefinition>();
        public List<MetricDefinition> metricDefinitions = new List<MetricDefinition>();

        public string description => "Collector endpoint holds all of the generated data in memory. Used for testing";

        public struct SimulationRun
        {
            public int TotalFrames => frames.Count;
            public List<Frame> frames;
            public SimulationMetadata metadata;
        }

        public List<SimulationRun> collectedRuns = new List<SimulationRun>();
        public SimulationRun currentRun;

        public void SensorRegistered(SensorDefinition sensor)
        {
            sensors.Add(sensor);
        }

        public void AnnotationRegistered(AnnotationDefinition annotationDefinition)
        {
            annotationDefinitions.Add(annotationDefinition);
        }

        public void MetricRegistered(MetricDefinition metricDefinition)
        {
            metricDefinitions.Add(metricDefinition);
        }

        public object Clone()
        {
            return new TestCollectorEndpoint();
        }

        public void SimulationStarted(SimulationMetadata metadata)
        {
            currentRun = new SimulationRun
            {
                frames = new List<Frame>()
            };
        }

        public void FrameGenerated(Frame frame)
        {
            if (currentRun.frames == null)
            {
                Debug.LogError("Current run frames is null, probably means that OnSimulationStarted was never called");
            }

            currentRun.frames?.Add(frame);
        }

        public void SimulationCompleted(SimulationMetadata metadata)
        {
            currentRun.metadata = metadata;
            collectedRuns.Add(currentRun);
        }
    }
}
