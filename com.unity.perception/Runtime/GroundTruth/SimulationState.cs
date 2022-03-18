using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    class SimulationState
    {
        public const string lastEndpointTypeKey = "LastEndpointTypeKey";
        public const string lastFileSystemPathKey = "LastFileSystemPathKey";

        internal enum ExecutionStateType
        {
            NotStarted,
            Starting,
            Running,
            Complete
        }

        internal bool IsRunning()
        {
            return !IsNotRunning();
        }

        internal bool IsNotRunning()
        {
            return ExecutionState == ExecutionStateType.NotStarted || ExecutionState == ExecutionStateType.Complete;
        }

        internal ExecutionStateType ExecutionState { get; private set; }

        HashSet<SensorHandle> m_ActiveSensors = new HashSet<SensorHandle>();
        IDictionary<SensorHandle, SensorData> m_Sensors = new ConcurrentDictionary<SensorHandle, SensorData>();

        internal IConsumerEndpoint consumerEndpoint { get; }

        int m_SequenceId = 0;

        HashSet<string> m_Ids = new HashSet<string>();

        // Always use the property SequenceTimeMs instead
        int m_FrameCountLastUpdatedSequenceTime;
        float m_SequenceTimeDoNotUse;
        float m_UnscaledSequenceTimeDoNotUse;

        int m_FrameCountLastStepIncremented = -1;
        int m_TotalFrames = 0;
        int m_Step = -1;

        Dictionary<int, FrameId> m_FrameToPendingIdMap = new Dictionary<int, FrameId>();
        Dictionary<FrameId, int> m_PendingIdToFrameMap = new Dictionary<FrameId, int>();
        SortedDictionary<FrameId, PendingFrame> m_PendingFrames = new SortedDictionary<FrameId, PendingFrame>();

        float m_LastTimeScale;

        struct FrameId : IComparable<FrameId>
        {
            internal FrameId(int sequence, int step)
            {
                this.sequence = sequence;
                this.step = step;
            }

            internal int sequence { get; private set; }
            internal int step { get; private set; }

            public override string ToString()
            {
                return $"({sequence},{step})";
            }

            internal static FrameId FromPendingId(PendingId id)
            {
                return new FrameId
                {
                    sequence = id.Sequence,
                    step = id.Step
                };
            }

            public int CompareTo(FrameId other)
            {
                var sequenceComparison = sequence.CompareTo(other.sequence);
                return sequenceComparison != 0 ? sequenceComparison : step.CompareTo(other.step);
            }
        }

        //A sensor will be triggered if sequenceTime is within includeThreshold seconds of the next trigger
        const float k_SimulationTimingAccuracy = 0.01f;

        internal SimulationState(IConsumerEndpoint endpoint)
        {
            ExecutionState = ExecutionStateType.NotStarted;

            m_SimulationMetadata = new SimulationMetadata()
            {
                unityVersion = Application.unityVersion,
                perceptionVersion = DatasetCapture.perceptionVersion,
            };

            consumerEndpoint = endpoint;
        }

        bool readyToShutdown => !m_PendingFrames.Any();

        internal (int sequence, int step) GetSequenceAndStepFromFrame(int frame)
        {
            return m_FrameToPendingIdMap.TryGetValue(frame, out var penId) ? (penId.sequence, penId.step) : (-1, -1);
        }

        (PendingFrame, PendingId) GetPendingFrameForMetric(MetricDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var metricId = PendingId.CreateMetricId(m_SequenceId, AcquireStep(), definition.id);

            var frameId = new FrameId(m_SequenceId, AcquireStep());

            var pendingFrame = GetOrCreatePendingFrame(frameId);

            if (pendingFrame == null)
                throw new InvalidOperationException($"Could not get or create a pending frame for {frameId}");

            return (pendingFrame, metricId);
        }

        #region Metadata Reporting

        public void ReportMetadata(string key, int value)
        {
            m_SimulationMetadata.Add(key, value);
        }
        public void ReportMetadata(string key, uint value)
        {
            m_SimulationMetadata.Add(key, value);
        }

        public void ReportMetadata(string key, float value)
        {
            m_SimulationMetadata.Add(key, value);
        }

        public void ReportMetadata(string key, string value)
        {
            m_SimulationMetadata.Add(key, value);
        }

        public void ReportMetadata(string key, bool value)
        {
            m_SimulationMetadata.Add(key, value);
        }

        public void ReportMetadata(string key, int[] value)
        {
            m_SimulationMetadata.Add(key, value);
        }

        public void ReportMetadata(string key, float[] value)
        {
            m_SimulationMetadata.Add(key, value);
        }

        public void ReportMetadata(string key, string[] value)
        {
            m_SimulationMetadata.Add(key, value);
        }

        public void ReportMetadata(string key, bool[] value)
        {
            m_SimulationMetadata.Add(key, value);
        }
        #endregion

        class PendingSensor
        {
            public PendingSensor(PendingId id)
            {
                if (!id.IsValidSensorId) throw new ArgumentException("Passed in wrong ID type");

                m_Id = id;
                m_SensorData = null;
                Annotations = new ConcurrentDictionary<PendingId, Annotation>();
            }

            public PendingSensor(PendingId id, Sensor sensorData) : this(id)
            {
                if (!id.IsValidSensorId) throw new ArgumentException("Passed in wrong ID type");
                m_SensorData = sensorData;
            }

            public Sensor ToSensor()
            {
                if (!IsReadyToReport()) return null;
                m_SensorData.annotations = Annotations.Select(kvp => kvp.Value).ToList();
                return m_SensorData;
            }

            PendingId m_Id;
            Sensor m_SensorData;
            public IDictionary<PendingId, Annotation> Annotations { get; private set; }

            public bool IsPending<T>(AsyncFuture<T> asyncFuture) where T : DataModelElement
            {
                switch (asyncFuture.futureType)
                {
                    case FutureType.Sensor:
                        return m_SensorData == null;
                    case FutureType.Annotation:
                    {
                        var id = asyncFuture.pendingId;

                        if (!id.IsValidAnnotationId)
                            throw new InvalidOperationException("Passed in ID was not correct type for annotation");

                        if (!Annotations.ContainsKey(id))
                            throw new InvalidOperationException("");

                        return
                            Annotations[id] == null;
                    }
                    case FutureType.Metric:
                    {
                        throw new InvalidOperationException("Metrics should not be registered with sensors");
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public bool ReportAsyncResult<T>(AsyncFuture<T> asyncFuture, object result) where T : DataModelElement
            {
                switch (asyncFuture.futureType)
                {
                    case FutureType.Sensor:
                        if (!(result is Sensor sensor))
                        {
                            throw new InvalidOperationException("Tried to report a non-sensor value with an async sensor");
                        }
                        m_SensorData = sensor;
                        return true;

                    case FutureType.Annotation:
                    {
                        var id = asyncFuture.pendingId;

                        if (!id.IsValidAnnotationId)
                            throw new InvalidOperationException("Passed in ID was not correct type for annotation");

                        if (!Annotations.ContainsKey(id))
                            throw new InvalidOperationException("");

                        if (!(result is Annotation annotation))
                        {
                            throw new InvalidOperationException("Tried to report a non-annotation value with an async annotation");
                        }

                        Annotations[id] = annotation;
                        return true;
                    }
                    case FutureType.Metric:
                    {
                        Debug.LogError("Metrics should not be sent to sensors");
                        return false;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public bool IsReadyToReport()
            {
                return
                    m_SensorData != null &&
                    Annotations.All(i => i.Value != null);
            }
        }

        class PendingFrame
        {
            public FrameId pendingId { get; }
            public float timestamp { get; }

            public IDictionary<string, PendingSensor> m_Sensors;
            public IDictionary<string, Metric> m_Metrics;

            public IEnumerable<PendingSensor> sensors => m_Sensors.Values;
            public IEnumerable<Metric> metrics => m_Metrics.Values;

            public bool CaptureReported { get; set; } = false;
            public PendingFrame(FrameId pendingFrameId, float timestamp)
            {
                pendingId = pendingFrameId;
                this.timestamp = timestamp;
                m_Sensors = new ConcurrentDictionary<string, PendingSensor>();
                m_Metrics = new ConcurrentDictionary<string, Metric>();
            }

            public bool IsReadyToReport()
            {
                return
                    m_Metrics.All(i => i.Value != null) &&
                    m_Sensors.All(sensor => sensor.Value.IsReadyToReport());
            }

            public PendingSensor GetOrCreatePendingSensor(PendingId sensorId)
            {
                if (!sensorId.IsValidSensorId)
                    throw new ArgumentException("Passed in a non-sensor ID");
                return GetOrCreatePendingSensor(sensorId, out var _);
            }

            PendingSensor GetOrCreatePendingSensor(PendingId id, out bool created)
            {
                created = false;

                if (!id.IsValidSensorId)
                {
                    throw new ArgumentException("Passed in an invalid sensor ID");
                }

                if (!m_Sensors.TryGetValue(id.SensorId, out var pendingSensor))
                {
                    pendingSensor = new PendingSensor(id);
                    m_Sensors[id.SensorId] = pendingSensor;
                    created = true;
                }

                return pendingSensor;
            }

            public bool IsPending<T>(AsyncFuture<T> asyncFuture) where T : DataModelElement
            {
                var id = asyncFuture.pendingId;

                if (id == null)
                    throw new InvalidOperationException("Async future did not have an ID");

                if (asyncFuture.futureType == FutureType.Metric)
                {
                    if (!id.IsValidMetricId)
                    {
                        throw new InvalidEnumArgumentException("AsyncFuture has the wrong ID type for a metric");
                    }

                    var metricId = id.MetricId;
                    return m_Metrics.ContainsKey(metricId) && m_Metrics[metricId] == null;
                }

                if (!id.IsValidSensorId)
                    throw new InvalidOperationException("Pending ID is not a valid sensor ID");

                return
                    m_Sensors.TryGetValue(id.SensorId, out var pendingSensor) &&
                    pendingSensor.IsPending(asyncFuture);
            }

            public bool ReportAsyncResult<T>(AsyncFuture<T> asyncFuture, T result) where T : DataModelElement
            {
                var id = asyncFuture.pendingId;

                if (id == null)
                    throw new InvalidOperationException("Async future did not have an ID");

                if (asyncFuture.futureType == FutureType.Metric)
                {
                    if (!(result is Metric metric))
                        throw new InvalidOperationException("Future is associated with a non-metric result");

                    if (!id.IsValidMetricId)
                    {
                        throw new InvalidEnumArgumentException("AsyncFuture has the wrong ID type for a metric");
                    }

                    metric.sensorId = id.SensorId;
                    metric.annotationId = id.annotationId;

                    m_Metrics[id.MetricId] = metric;
                    return true;
                }

                if (!id.IsValidSensorId)
                    throw new InvalidOperationException("Pending ID is not a valid sensor ID");

                var sensor = GetOrCreatePendingSensor(id);
                return sensor.ReportAsyncResult(asyncFuture, result);
            }

            public void AddSensor(PendingId id, Sensor sensor)
            {
                if (!id.IsValidSensorId) throw new ArgumentException("Passed in ID is not a valid sensor ID");
                m_Sensors[id.SensorId] = new PendingSensor(id, sensor);
            }

            public void AddMetric(PendingId id, Metric metric)
            {
                if (!id.IsValidMetricId) throw new ArgumentException("Passed in ID is not a valid metric ID");
                m_Metrics[id.MetricId] = metric;
             }
        }

        public struct SensorData
        {
            public string modality;
            public string description;
            public float firstCaptureTime;
            public CaptureTriggerMode captureTriggerMode;
            public float renderingDeltaTime;
            public int framesBetweenCaptures;
            public bool manualSensorAffectSimulationTiming;

            public float sequenceTimeOfNextCapture;
            public float sequenceTimeOfNextRender;
            public int lastCaptureFrameCount;
        }

        /// <summary>
        /// Use this to get the current step when it is desirable to ensure the step has been allocated for this frame. Steps should only be allocated in frames where a capture or metric is reported.
        /// </summary>
        /// <returns>The current step</returns>
        int AcquireStep()
        {
            EnsureStepIncremented();
            EnsureSequenceTimingsUpdated();
            return m_Step;
        }

        /// <summary>
        /// The simulation time that has elapsed since the beginning of the sequence.
        /// </summary>
        public float SequenceTime
        {
            get
            {
                //TODO: Can this be replaced with Time.time - sequenceTimeStart?
                if (ExecutionState != ExecutionStateType.Running)
                    return 0;

                EnsureSequenceTimingsUpdated();

                return m_SequenceTimeDoNotUse;
            }
        }

        /// <summary>
        /// The unscaled simulation time that has elapsed since the beginning of the sequence. This is the time that should be used for scheduling sensors
        /// </summary>
        float UnscaledSequenceTime
        {
            get
            {
                //TODO: Can this be replaced with Time.time - sequenceTimeStart?
                if (ExecutionState != ExecutionStateType.Running)
                    return 0;

                EnsureSequenceTimingsUpdated();
                return m_UnscaledSequenceTimeDoNotUse;
            }
        }

        void EnsureSequenceTimingsUpdated()
        {
            if (ExecutionState != ExecutionStateType.Running)
            {
                ResetTimings();
            }
            else if (m_FrameCountLastUpdatedSequenceTime != Time.frameCount)
            {
                m_SequenceTimeDoNotUse += Time.deltaTime;
                if (Time.timeScale > 0)
                    m_UnscaledSequenceTimeDoNotUse += Time.deltaTime / Time.timeScale;

                CheckTimeScale();

                m_FrameCountLastUpdatedSequenceTime = Time.frameCount;
            }
        }

        void CheckTimeScale()
        {
            if (m_LastTimeScale != Time.timeScale)
                Debug.LogError($"Time.timeScale may not change mid-sequence. This can cause sensors to get out of sync and corrupt the data. Previous: {m_LastTimeScale} Current: {Time.timeScale}");

            m_LastTimeScale = Time.timeScale;
        }

        void EnsureStepIncremented()
        {
            if (m_FrameCountLastStepIncremented != Time.frameCount)
            {
                m_FrameCountLastStepIncremented = Time.frameCount;
                m_Step++;
            }
        }

        bool m_DataCaptured = false;

        public void StartNewSequence()
        {
            ResetTimings();
            m_FrameCountLastStepIncremented = -1;
            if (!m_DataCaptured)
            {
                m_SequenceId = 0;
                m_DataCaptured = true;
            }
            else
            {
                m_SequenceId++;
            }
            m_Step = -1;
            foreach (var kvp in m_Sensors.ToArray())
            {
                var sensorData = kvp.Value;
                sensorData.sequenceTimeOfNextCapture = GetSequenceTimeOfNextCapture(sensorData);
                sensorData.sequenceTimeOfNextRender = 0;
                m_Sensors[kvp.Key] = sensorData;
            }
        }

        void ResetTimings()
        {
            m_FrameCountLastUpdatedSequenceTime = Time.frameCount;
            m_SequenceTimeDoNotUse = 0;
            m_UnscaledSequenceTimeDoNotUse = 0;
            m_LastTimeScale = Time.timeScale;
        }

        string RegisterId(string requestedId)
        {
            var id = requestedId;
            var i = 0;
            while (m_Ids.Contains(id))
            {
                id = $"{requestedId}_{i++}";
            }

            m_Ids.Add(id);
            return id;
        }

        public SensorHandle AddSensor(SensorDefinition sensor, float renderingDeltaTime)
        {
            var sensorData = new SensorData()
            {
                modality = sensor.modality,
                description = sensor.description,
                firstCaptureTime = UnscaledSequenceTime + sensor.firstCaptureFrame * renderingDeltaTime,
                captureTriggerMode = sensor.captureTriggerMode,
                renderingDeltaTime = renderingDeltaTime,
                framesBetweenCaptures = sensor.framesBetweenCaptures,
                manualSensorAffectSimulationTiming = sensor.manualSensorsAffectTiming,
                lastCaptureFrameCount = -1
            };
            sensorData.sequenceTimeOfNextCapture = GetSequenceTimeOfNextCapture(sensorData);
            sensorData.sequenceTimeOfNextRender = UnscaledSequenceTime;

            sensor.id = RegisterId(sensor.id);
            var sensorHandle = new SensorHandle(sensor.id);

            m_ActiveSensors.Add(sensorHandle);
            m_Sensors.Add(sensorHandle, sensorData);

            consumerEndpoint.SensorRegistered(sensor);

            if (ExecutionState == ExecutionStateType.NotStarted)
            {
                ExecutionState = ExecutionStateType.Starting;
            }

            return sensorHandle;
        }

        float GetSequenceTimeOfNextCapture(SensorData sensorData)
        {
            // If the first capture hasn't happened yet, sequenceTimeNextCapture field won't be valid
            if (sensorData.firstCaptureTime >= UnscaledSequenceTime)
            {
                return sensorData.captureTriggerMode == CaptureTriggerMode.Scheduled? sensorData.firstCaptureTime : float.MaxValue;
            }

            return sensorData.sequenceTimeOfNextCapture;
        }

        public bool Contains(string id) => m_Ids.Contains(id);

        public bool IsEnabled(SensorHandle sensorHandle) => m_ActiveSensors.Contains(sensorHandle);

        public void SetEnabled(SensorHandle sensorHandle, bool value)
        {
            if (!value)
                m_ActiveSensors.Remove(sensorHandle);
            else
                m_ActiveSensors.Add(sensorHandle);
        }

        static void CheckDatasetAllowed()
        {
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("Dataset generation is only supported in play mode.");
            }
        }

        SimulationMetadata m_SimulationMetadata;

        internal void Update()
        {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (ExecutionState == ExecutionStateType.NotStarted)
            {
                return;
            }

            if (ExecutionState == ExecutionStateType.Starting)
            {
                UpdateStarting();
            }

            if (ExecutionState == ExecutionStateType.Running)
            {
                UpdateRunning();
            }

            if (ExecutionState == ExecutionStateType.Complete)
            {
                UpdateComplete();
            }
        }

        void UpdateStarting()
        {
            m_SimulationMetadata = new SimulationMetadata()
            {
                unityVersion = Application.unityVersion,
                perceptionVersion = DatasetCapture.perceptionVersion,
            };

            // Add metadata for start time
            m_SimulationMetadata.Add("simulationStartTime", DateTime.Now.ToString());

            consumerEndpoint.SimulationStarted(m_SimulationMetadata);

            //simulation starts now
            m_FrameCountLastUpdatedSequenceTime = Time.frameCount;
            m_LastTimeScale = Time.timeScale;

            ExecutionState = ExecutionStateType.Running;
        }

        void UpdateRunning()
        {
            EnsureSequenceTimingsUpdated();

            //update the active sensors sequenceTimeNextCapture and lastCaptureFrameCount
            foreach (var activeSensor in m_ActiveSensors)
            {
                var sensorData = m_Sensors[activeSensor];

#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPaused)
                {
                    //When the user clicks the 'step' button in the editor, frames will always progress at .02 seconds per step.
                    //In this case, just run all sensors each frame to allow for debugging
                    Debug.Log($"Frame step forced all sensors to synchronize, changing frame timings.");

                    sensorData.sequenceTimeOfNextRender = UnscaledSequenceTime;
                    sensorData.sequenceTimeOfNextCapture = UnscaledSequenceTime;
                    m_Sensors[activeSensor] = sensorData;
                }
#endif

                if (Mathf.Abs(sensorData.sequenceTimeOfNextRender - UnscaledSequenceTime) < k_SimulationTimingAccuracy)
                {
                    //means this frame fulfills this sensor's simulation time requirements, we can move target to next frame.
                    sensorData.sequenceTimeOfNextRender += sensorData.renderingDeltaTime;
                }

                if (activeSensor.ShouldCaptureThisFrame)
                {
                    if (sensorData.captureTriggerMode.Equals(CaptureTriggerMode.Scheduled))
                    {
                        sensorData.sequenceTimeOfNextCapture += sensorData.renderingDeltaTime * (sensorData.framesBetweenCaptures + 1);
                        Debug.Assert(sensorData.sequenceTimeOfNextCapture > UnscaledSequenceTime,
                            $"Next scheduled capture should be after {UnscaledSequenceTime} but is {sensorData.sequenceTimeOfNextCapture}");
                        while (sensorData.sequenceTimeOfNextCapture <= UnscaledSequenceTime)
                            sensorData.sequenceTimeOfNextCapture += sensorData.renderingDeltaTime * (sensorData.framesBetweenCaptures + 1);
                    }
                    else if (sensorData.captureTriggerMode.Equals(CaptureTriggerMode.Manual))
                    {
                        sensorData.sequenceTimeOfNextCapture = float.MaxValue;
                    }

                    sensorData.lastCaptureFrameCount = Time.frameCount;
                }

                m_Sensors[activeSensor] = sensorData;
            }

            //find the deltatime required to land on the next active sensor that needs simulation
            var nextFrameDt = float.PositiveInfinity;
            foreach (var activeSensor in m_ActiveSensors)
            {
                float thisSensorNextFrameDt = -1;

                var sensorData = m_Sensors[activeSensor];
                if (sensorData.captureTriggerMode.Equals(CaptureTriggerMode.Scheduled))
                {
                    thisSensorNextFrameDt = sensorData.sequenceTimeOfNextRender - UnscaledSequenceTime;

                    Debug.Assert(thisSensorNextFrameDt > 0f, "Sensor was scheduled to capture in the past but got skipped over.");
                }
                else if (sensorData.captureTriggerMode.Equals(CaptureTriggerMode.Manual) && sensorData.manualSensorAffectSimulationTiming)
                {
                    thisSensorNextFrameDt = sensorData.sequenceTimeOfNextRender - UnscaledSequenceTime;
                }

                if (thisSensorNextFrameDt > 0f && thisSensorNextFrameDt < nextFrameDt)
                {
                    nextFrameDt = thisSensorNextFrameDt;
                }
            }

            if (float.IsPositiveInfinity(nextFrameDt))
            {
                //means no sensor is controlling simulation timing, so we set Time.captureDeltaTime to 0 (default) which means the setting does not do anything
                nextFrameDt = 0;
            }

            ReportFramesToConsumer();

            Time.captureDeltaTime = nextFrameDt;
        }

        void Shutdown()
        {
            if (m_Ids.Count == 0)
            {
                ExecutionState = ExecutionStateType.NotStarted;
                return;
            }

            ReportFramesToConsumer(true, true);

            Time.captureDeltaTime = 0;

            if (m_Ids.Count == 0)
                return;

            if (readyToShutdown)
            {
                m_SimulationMetadata.Add("totalFrames", m_TotalFrames);
                m_SimulationMetadata.Add("totalSequences", m_SequenceId + 1);

                var i = 0;
                var sensors = new string[m_Sensors.Count];
                foreach (var s in m_Sensors)
                {
                    sensors[i++] = s.Key.Id;
                }
                m_SimulationMetadata.Add("sensors", sensors);

                var annotators = new List<Metadata>();
                foreach (var ann in m_AnnotationIds)
                {
                    var metadata = new Metadata();
                    metadata.Add("name", ann.Item1);
                    metadata.Add("type", ann.Item2);
                    annotators.Add(metadata);
                }

                m_SimulationMetadata.Add("annotators", annotators);
                m_SimulationMetadata.Add("metricCollectors", m_MetricIds);

                // Add metadata for end time
                m_SimulationMetadata.Add("simulationEndTime", DateTime.Now.ToString());

                consumerEndpoint.SimulationCompleted(m_SimulationMetadata);

                PlayerPrefs.SetString(lastEndpointTypeKey, consumerEndpoint.GetType().ToString());

                if (consumerEndpoint is IFileSystemEndpoint fs)
                {
                    PlayerPrefs.SetString(lastFileSystemPathKey, fs.currentPath);
                }

                ExecutionState = ExecutionStateType.NotStarted;

                VerifyNoMorePendingFrames();
            }
        }

        void UpdateComplete()
        {
            VerifyNoMorePendingFrames();
        }

        void VerifyNoMorePendingFrames()
        {
            if (m_PendingFrames.Count > 0)
                Debug.LogError($"Simulation ended with pending {m_PendingFrames.Count} annotations (final id): {m_PendingFrames.Last().Key}");
        }

        public void SetNextCaptureTimeToNowForSensor(SensorHandle sensorHandle)
        {
            if (!m_Sensors.ContainsKey(sensorHandle))
            {
                Debug.LogError($"Tried to set a capture time for an unregistered sensor: {sensorHandle}");
                return;
            }

            var data = m_Sensors[sensorHandle];
            data.sequenceTimeOfNextCapture = UnscaledSequenceTime;
            m_Sensors[sensorHandle] = data;
        }

        public int currentFrame => Time.frameCount;

        public bool ShouldCaptureThisFrame(SensorHandle sensorHandle)
        {
            if (!m_Sensors.ContainsKey(sensorHandle))
                return false;

            var data = m_Sensors[sensorHandle];
            if (data.lastCaptureFrameCount == Time.frameCount)
                return true;

            return data.sequenceTimeOfNextCapture - UnscaledSequenceTime < k_SimulationTimingAccuracy;
        }
        internal bool CapturesLeft()
        {
            return m_PendingFrames.Count > 0;
        }

        public void End()
        {
            Time.captureDeltaTime = 0;
            Shutdown();
            ExecutionState = ExecutionStateType.Complete;
        }

        List<(string, string)> m_AnnotationIds = new List<(string, string)>();

        public void RegisterAnnotationDefinition(AnnotationDefinition definition)
        {
            CheckDatasetAllowed();

            definition.id = RegisterId(definition.id);
            m_AnnotationIds.Add((definition.id, definition.modelType));
            consumerEndpoint.AnnotationRegistered(definition);
        }

        List<string> m_MetricIds = new List<string>();

        public void RegisterMetric(MetricDefinition definition)
        {
            CheckDatasetAllowed();

            definition.id = RegisterId(definition.id);
            m_MetricIds.Add(definition.id);
            consumerEndpoint.MetricRegistered(definition);
        }

        internal PendingId ReportSensor(SensorHandle handle, Sensor sensor)
        {
            var step = AcquireStep();
            var id = PendingId.CreateSensorId(m_SequenceId, step, handle.Id);
            var pendingFrame = GetOrCreatePendingFrame(id);
            pendingFrame.AddSensor(id, sensor);
            return id;
        }

        internal PendingId ReportAnnotation(SensorHandle sensorHandle, AnnotationDefinition definition, Annotation annotation)
        {
            var step = AcquireStep();
            var sensorId = PendingId.CreateSensorId(m_SequenceId, step, sensorHandle.Id);

            var pendingFrame = GetOrCreatePendingFrame(sensorId);
            var sensor = pendingFrame.GetOrCreatePendingSensor(sensorId);

            var annotationId = PendingId.CreateAnnotationId(m_SequenceId, step, sensorHandle.Id, definition.id);

            sensor.Annotations[annotationId] = annotation;
            return annotationId;
        }

        PendingFrame GetOrCreatePendingFrame(PendingId pendingId)
        {
            var frameId = FrameId.FromPendingId(pendingId);
            return GetOrCreatePendingFrame(frameId);
        }

        PendingFrame GetOrCreatePendingFrame(FrameId frameId)
        {
            return GetOrCreatePendingFrame(frameId, out var _);
        }

        PendingFrame GetOrCreatePendingFrame(FrameId frameId, out bool created)
        {
            m_DataCaptured = true;

            created = false;
            EnsureStepIncremented();

            if (!m_PendingFrames.TryGetValue(frameId, out var pendingFrame))
            {
                pendingFrame = new PendingFrame(frameId, SequenceTime);
                m_PendingFrames[frameId] = pendingFrame;

                m_PendingIdToFrameMap[frameId] = Time.frameCount;
                m_FrameToPendingIdMap[Time.frameCount] = frameId;

                created = true;
            }

            return pendingFrame;
        }

        internal AsyncFuture<Annotation> ReportAnnotationAsync(AnnotationDefinition annotationDefinition, SensorHandle sensorHandle)
        {
            return AsyncFuture<Annotation>.CreateAnnotationFuture(ReportAnnotation(sensorHandle, annotationDefinition, null), this);
        }

        internal AsyncFuture<Sensor> ReportSensorAsync(SensorHandle handle)
        {
            return AsyncFuture<Sensor>.CreateSensorFuture(ReportSensor(handle, null), this);
        }

        internal bool IsPending<T>(AsyncFuture<T> asyncFuture) where T : DataModelElement
        {
            var frameId = FrameId.FromPendingId(asyncFuture.pendingId);

            return
                m_PendingFrames.TryGetValue(frameId, out var pendingFrame) &&
                pendingFrame.IsPending(asyncFuture);
        }

        PendingFrame GetPendingFrame<T>(AsyncFuture<T> future) where T : DataModelElement
        {
            return GetPendingFrame(FrameId.FromPendingId(future.pendingId));
        }

        PendingFrame GetPendingFrame(FrameId id)
        {
            return m_PendingFrames[id];
        }

        public bool ReportAsyncResult<T>(AsyncFuture<T> asyncFuture, T result) where T : DataModelElement
        {
            if (!asyncFuture.IsPending()) return false;

            var pendingFrame = GetPendingFrame(asyncFuture);

            if (pendingFrame == null) return false;

            return pendingFrame.ReportAsyncResult<T>(asyncFuture, result);
        }

        public AsyncFuture<Metric> CreateAsyncMetric(MetricDefinition metricDefinition, SensorHandle sensorHandle = default, AnnotationHandle annotationHandle = default)
        {
            EnsureStepIncremented();
            var sensorId = sensorHandle.IsValid ? sensorHandle.Id : default;
            var annotationId = annotationHandle.IsValid() ? annotationHandle.Id : default;
            var pendingId = ReportMetric(metricDefinition, null, sensorId, annotationId);
            return AsyncFuture<Metric>.CreateMetricFuture(pendingId, this);
        }

        internal PendingId ReportMetric(MetricDefinition definition, Metric metric, string sensorId, string annotationId)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var pendingId = PendingId.CreateMetricId(m_SequenceId, AcquireStep(), sensorId, annotationId, definition.id);

            var pendingFrame = GetOrCreatePendingFrame(pendingId);

            if (pendingFrame == null)
                throw new InvalidOperationException($"Could not get or create a pending frame for {pendingId}");

            if (metric != null)
            {
                metric.sensorId = sensorId;
                metric.annotationId = annotationId;
            }

            pendingFrame.AddMetric(pendingId, metric);

            return pendingId;
        }

        PendingId ReportMetric(SensorHandle sensor, MetricDefinition definition, Metric metric, AnnotationHandle annotation)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(metric));

            var pendingId = PendingId.CreateMetricId(m_SequenceId, AcquireStep(), sensor.Id, annotation.Id, definition.id);
            var pendingFrame = GetOrCreatePendingFrame(pendingId);

            if (pendingFrame == null)
                throw new InvalidOperationException($"Could not get or create a pending frame for {pendingId}");

            pendingFrame.AddMetric(pendingId, metric);

            return pendingId;
        }

        Dictionary<int, int> m_SequenceMap = new Dictionary<int, int>();

        Queue<KeyValuePair<FrameId, PendingFrame>> m_PendingFramesToReport = new Queue<KeyValuePair<FrameId, PendingFrame>>();

        static List<Sensor> ConvertToSensors(PendingFrame frame, SimulationState simulationState)
        {
            return frame.sensors.Where(s => s.IsReadyToReport()).Select(s => s.ToSensor()).ToList();
        }

        Frame ConvertToFrameData(PendingFrame pendingFrame, SimulationState simState)
        {
            var frameId = m_PendingIdToFrameMap[pendingFrame.pendingId];

            var frame = new Frame(frameId, pendingFrame.pendingId.sequence, pendingFrame.pendingId.step, pendingFrame.timestamp)
            {
                sensors = ConvertToSensors(pendingFrame, simState)
            };

            foreach (var metric in pendingFrame.metrics)
            {
                frame.metrics.Add(metric);
            }

            return frame;
        }

        void ReportFramesToConsumer(bool flush = false, bool writeCapturesFromThisFrame = false)
        {
            m_PendingFramesToReport.Clear();
            var remainingPendingFrames = new List<KeyValuePair<FrameId, PendingFrame>>();

            // Write out each frame until we reach one that is not ready to write yet, this is in order to
            // assure that all reports happen in sequential order
            foreach (var frame in m_PendingFrames)
            {
                var recordedFrame = m_PendingIdToFrameMap[frame.Key];

                if ((writeCapturesFromThisFrame || recordedFrame < currentFrame) &&
                    frame.Value.IsReadyToReport())
                {
                    m_PendingFramesToReport.Enqueue(frame);
                }
                else if (flush)
                {
                    remainingPendingFrames.Add(frame);
                }
                else
                {
                    break;
                }
            }

            foreach (var pf in m_PendingFramesToReport)
            {
                m_PendingFrames.Remove(pf.Key);
            }

            if (flush)
            {
                foreach (var pf in remainingPendingFrames)
                {
                    var metrics = string.Join(Environment.NewLine,
                        pf.Value.m_Metrics.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key));

                    var annotations = string.Join(Environment.NewLine,
                        pf.Value.m_Sensors.SelectMany(s => s.Value.Annotations).Where(kvp => kvp.Value == null).Select(kvp => kvp.Key));

                    var sensors = string.Join(Environment.NewLine,
                        pf.Value.m_Sensors.Where(kvp => kvp.Value.ToSensor() == null).Select(kvp => kvp.Key));

                    object message = $@"Simulation ended with pending frame {pf.Key}.";
                    if (!string.IsNullOrEmpty(metrics))
                        message += Environment.NewLine + $" Unreported metrics: {metrics}";

                    if (!string.IsNullOrEmpty(annotations))
                        message += Environment.NewLine + $" Unreported annotations: {annotations}";

                    if (!string.IsNullOrEmpty(sensors))
                        message += Environment.NewLine + $" Unreported sensors: {sensors}";

                    Debug.LogError(message);
                    m_PendingFrames.Remove(pf.Key);
                }
            }

            while (m_PendingFramesToReport.Any())
            {
                var converted = ConvertToFrameData(m_PendingFramesToReport.Dequeue().Value, this);
                m_TotalFrames++;

                if (converted == null)
                {
                    Debug.LogError("Could not convert frame data");
                }

                if (consumerEndpoint == null)
                {
                    Debug.LogError("Consumer endpoint is null");
                }

                consumerEndpoint.FrameGenerated(converted);
            }
        }
    }
}
