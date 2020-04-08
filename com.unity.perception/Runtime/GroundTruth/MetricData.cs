using System;
using System.Collections.Generic;
using UnityEngine;
using USim = Unity.Simulation;

namespace UnityEngine.Perception.Sensors
{
    class MetricData<T>
    {
        public string Name;
        public SimulationInfo SimulationInfo;
        public CurrentContext CurrentContext;
        public T Metric;

        public MetricData(string name, T metric)
        {
            if(string.IsNullOrEmpty(name)) {
                throw new ArgumentException("Arg name is null or empty");
            }

            this.Name = name;
            this.Metric = metric;
            this.SimulationInfo = SimulationInfo.GetInstance();
            this.CurrentContext = CurrentContext.NewCurrentContext();
        }
    }

    [Serializable]
    class SensorMetric
    {
        public string SensorId;
        public int FrameId;
        public List<ObjectCountEntry> SegmentedHistogram = new List<ObjectCountEntry>();
    }

    [Serializable]
    struct ObjectCountEntry
    {
        public string Label;
        public uint Count;
    }

    [Serializable]
    struct CurrentContext
    {
        static USim.Manager s_Manager;
        public double SimulationElapsedTime;
        public double SimulationElapsedTimeUnscaled;

        public static CurrentContext NewCurrentContext()
        {
            if(s_Manager == null) {
                s_Manager = USim.Manager.Instance;
            }

            return new CurrentContext() {
                SimulationElapsedTime = s_Manager.SimulationElapsedTime,
                SimulationElapsedTimeUnscaled = s_Manager.SimulationElapsedTimeUnscaled,
            };
        }
    }

    /// <summary> SimulationInfo - Context that is unchanged for this entire simulation </summary>
    [Serializable]
    class SimulationInfo
    {
        public string ProjectId;
        public string RunId;
        public string ExecutionId;
        public string AppParamId;
        public string RunInstanceId;
        public string AttemptId;

        public override string ToString()
        {
            return string.Format("{0}/{1}/{2}/{3}/{4}/{5}",
                this.ProjectId,
                this.RunId,
                this.ExecutionId,
                this.AppParamId,
                this.RunInstanceId,
                this.AttemptId);
        }

        static SimulationInfo s_Context;

        public static SimulationInfo GetInstance()
        {
            if(s_Context != null) {
                return s_Context;
            }

            var config = USim.Configuration.Instance;
            if(config.IsSimulationRunningInCloud()) {
                s_Context = GetInstance(config.GetStoragePath());
            } else {
                s_Context = new SimulationInfo() {
                    ProjectId = Guid.NewGuid().ToString(),
                    AppParamId = "urn:app_param_id:app_param_id",
                    ExecutionId = "urn:app_param_id:exn_id",
                    RunId = "urn:app_param_id:run_id",
                    RunInstanceId = "0",
                    AttemptId = "0"
                };
            }

            return s_Context;
        }

        static SimulationInfo GetInstance(string storagePath)
        {
            if(string.IsNullOrEmpty(storagePath)) {
                throw new ArgumentException("Arg storagePath is null or empty");
            }

            const int expectedTokenCount = 8;
            var tokens = storagePath.Split('/');
            if (tokens.Length < expectedTokenCount) {
                var msg = "Storage path not in the expected format";
                Debug.LogError(msg);
                throw new ArgumentException(msg);
            }

            return new SimulationInfo()
            {
                ProjectId = tokens[1],
                RunId = tokens[3],
                ExecutionId = tokens[4],
                AppParamId = tokens[5],
                RunInstanceId = tokens[6],
                AttemptId = tokens[7]
            };
        }
    }
}
