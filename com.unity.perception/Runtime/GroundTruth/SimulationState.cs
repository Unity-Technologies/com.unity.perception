using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Perception.Settings;
#if HDRP_PRESENT
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#endif
namespace UnityEngine.Perception.GroundTruth
{
    class SimulationState
    {
        public const string lastEndpointTypeKey = "LastEndpointTypeKey";
        public const string lastFileSystemPathKey = "LastFileSystemPathKey";

        internal enum ExecutionStateType
        {
            InitializingAccumulation,
            NotStarted,
            Starting,
            Running,
            Complete
        }

        internal static int frameOffset;
        internal static int sequenceId;
        internal static bool dataCaptured;

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

#if HDRP_PRESENT
        HDRenderPipeline m_RenderPipeline = null;
#endif

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

            var metricId = PendingId.CreateMetricId(sequenceId, AcquireStep(), definition.id);

            var frameId = new FrameId(sequenceId, AcquireStep());

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

        public interface IAdditionalSensorData {}
        public struct RgbSensorAdditionalSensorData : IAdditionalSensorData
        {
            public bool useAccumulation;
        }

        public static bool SensorUsesAccumulation(SensorData sd)
        {
            if (sd.additionalSensorData is RgbSensorAdditionalSensorData data)
            {
                return data.useAccumulation;
            }

            return false;
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
            public IAdditionalSensorData additionalSensorData;

            public float sequenceTimeOfNextAccumulation;
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
                if (m_Accumulating)
                {
                    var sensorData = m_Sensors[m_AccumulatingSensorsCapturedOrNot.First().Key];
                    m_UnscaledSequenceTimeDoNotUse += sensorData.renderingDeltaTime;
                }
                else
                {
                    if (Time.timeScale > 0)
                    {
                        m_UnscaledSequenceTimeDoNotUse += Time.deltaTime / Time.timeScale;
                    }
                    CheckTimeScale();
                }
                m_FrameCountLastUpdatedSequenceTime = Time.frameCount;
            }
        }

        void CheckTimeScale()
        {
            if (m_LastTimeScale != Time.timeScale)
            {
                Debug.LogError($"Time.timeScale may not change mid-sequence. This can cause sensors to get out of sync and corrupt the data. Previous: {m_LastTimeScale} Current: {Time.timeScale}");
            }

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

        public void StartNewSequence()
        {
            EndAccumulation();
            ResetTimings();

            m_FrameCountLastStepIncremented = -1;
            if (!dataCaptured)
            {
                sequenceId = 0;
                dataCaptured = true;
            }
            else
            {
                sequenceId++;
            }

            m_Step = -1;
            foreach (var kvp in m_Sensors.ToArray())
            {
                var sensorData = kvp.Value;
                sensorData.sequenceTimeOfNextAccumulation = GetSequenceTimeOfNextAccumulation(sensorData);
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
            IAdditionalSensorData additionalSensorData = null;
            bool useAccumulation = false;
            if (sensor is RgbSensorDefinition rgbSensorDef)
            {
                useAccumulation = rgbSensorDef.useAccumulation;
                additionalSensorData = new RgbSensorAdditionalSensorData { useAccumulation = useAccumulation };
            }

            var sensorData = new SensorData()
            {
                modality = sensor.modality,
                description = sensor.description,
                firstCaptureTime = UnscaledSequenceTime + sensor.firstCaptureFrame * renderingDeltaTime,
                captureTriggerMode = sensor.captureTriggerMode,
                renderingDeltaTime = renderingDeltaTime,
                framesBetweenCaptures = sensor.framesBetweenCaptures,
                manualSensorAffectSimulationTiming = sensor.manualSensorsAffectTiming,
                lastCaptureFrameCount = -1,
                additionalSensorData = additionalSensorData
            };
            sensorData.sequenceTimeOfNextAccumulation = GetSequenceTimeOfNextAccumulation(sensorData);
            sensorData.sequenceTimeOfNextCapture = GetSequenceTimeOfNextCapture(sensorData);
            sensorData.sequenceTimeOfNextRender = UnscaledSequenceTime;

            sensor.id = RegisterId(sensor.id);
            var sensorHandle = new SensorHandle(sensor.id);

            m_ActiveSensors.Add(sensorHandle);
            m_Sensors.Add(sensorHandle, sensorData);

            consumerEndpoint.SensorRegistered(sensor);

            if (ExecutionState == ExecutionStateType.NotStarted || (ExecutionState == ExecutionStateType.Starting && useAccumulation))
            {
                if (useAccumulation)
                    ExecutionState = ExecutionStateType.InitializingAccumulation;
                else
                    ExecutionState = ExecutionStateType.Starting;
            }

            return sensorHandle;
        }

        float GetSequenceTimeOfNextAccumulation(SensorData sensorData)
        {
            if (!SensorUsesAccumulation(sensorData)) return float.MaxValue;

            // If the first capture hasn't happened yet, sequenceTimeNextCapture field won't be valid
            if (sensorData.firstCaptureTime >= UnscaledSequenceTime)
            {
                return sensorData.captureTriggerMode == CaptureTriggerMode.Scheduled ? sensorData.firstCaptureTime : float.MaxValue;
            }

            return sensorData.sequenceTimeOfNextAccumulation;
        }

        bool usingAccumulation
        {
            get
            {
                // We know that we use accumulation if any of the active sensors have accumulation enabled
                foreach (var sensor in m_ActiveSensors)
                {
                    if (SensorUsesAccumulation(m_Sensors[sensor]))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        int ComputeTotalFramesWithAccumulation(int framesPerIteration)
        {
            var realCapturesOccurAt = new HashSet<int>();
            foreach (var cam in m_Sensors)
            {
                if (cam.Value.captureTriggerMode == CaptureTriggerMode.Manual || !SensorUsesAccumulation(cam.Value))
                    continue;
                var frameCounter = 0;
                var firstCaptureFrame = UnityEngine.Mathf.CeilToInt((cam.Value.firstCaptureTime - UnscaledSequenceTime) / cam.Value.renderingDeltaTime);
                if (framesPerIteration < firstCaptureFrame)
                    continue;
                frameCounter += firstCaptureFrame + 1;
                realCapturesOccurAt.Add(frameCounter);
                while (frameCounter <= framesPerIteration)
                {
                    frameCounter += cam.Value.framesBetweenCaptures + 1;
                    if (frameCounter <= framesPerIteration)
                    {
                        realCapturesOccurAt.Add(frameCounter);
                    }
                }
            }
            return framesPerIteration + (realCapturesOccurAt.Count * PerceptionSettings.instance.accumulationSettings.accumulationSamples);
        }

        int m_FindingRenderPipelineAttempts = 0;
        static int s_FindRenderPipelineTimeout = 20;

        void AccumulationSetup()
        {
            // If we have a scenario and the user has set the "adaptFixedLengthScenarioFrames" checkbox to true then:
            if (ScenarioBase.activeScenario != null && PerceptionSettings.instance.accumulationSettings.adaptFixedLengthScenarioFrames && Time.frameCount == 1)
            {
                // Only in the case of it being a FixedLengthScenario
                var scenario = ScenarioBase.activeScenario as FixedLengthScenario;
                if (scenario != null)
                {
                    // Update the total amount of framesPerIteration
                    scenario.framesPerIteration = ComputeTotalFramesWithAccumulation(scenario.framesPerIteration);
                }
            }
            // Try to cache the renderPipeline
            FindRenderPipeline();
            // for each frame that we try to get the renderPipeline we increase the waitFrames value (static) of the PerceptionCamera
            PerceptionCamera.waitFrames++;
            // for each frame of waiting we need to delay each sensor by its renderDeltaTime
            foreach (var sensor in m_ActiveSensors)
            {
                var sensorData = m_Sensors[sensor];
                sensorData.sequenceTimeOfNextAccumulation += m_Sensors[sensor].renderingDeltaTime;
                sensorData.sequenceTimeOfNextCapture += m_Sensors[sensor].renderingDeltaTime;
                // without this line the changes do not get saved
                m_Sensors[sensor] = sensorData;
            }
#if HDRP_PRESENT
            if (m_RenderPipeline != null)
                ExecutionState = ExecutionStateType.Starting;
            else
                m_FindingRenderPipelineAttempts++;

            if (m_FindingRenderPipelineAttempts == s_FindRenderPipelineTimeout)
            {
                Debug.LogError($"Fatal error: perception timed out looking for a HDRenderPipeline object. The simulation will now shutdown.");
                Shutdown();
            }
#else
            Debug.LogError("Accumulation is only supported in HDRP, initalizing accumulation should never have been attempted in this project");
            Shutdown();
#endif
        }

        void FindRenderPipeline()
        {
#if HDRP_PRESENT
            // this only works after a few frames so this method must be called multiple times at the start
            m_RenderPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
#endif
        }

#if HDRP_PRESENT
        void PrepareSubFrameCallBack(ScriptableRenderContext cntx, Camera[] cams)
        {
            // required for accumulation
            m_RenderPipeline?.PrepareNewSubFrame();
        }

#endif

        void StartAccumulation()
        {
#if HDRP_PRESENT
            // Makes sure it is only added once
            RenderPipelineManager.beginFrameRendering -= PrepareSubFrameCallBack;
            RenderPipelineManager.beginFrameRendering += PrepareSubFrameCallBack;
            var acc = PerceptionSettings.instance.accumulationSettings;

            // We use +1 on accumulation samples because the last frame of accumulation can be the denoised frame which doesn't include actual accumulation samples, for this reason it is best to add an extra frame
            m_RenderPipeline?.BeginRecording(acc.accumulationSamples + 1, acc.shutterInterval, Mathf.Min(acc.shutterFullyOpen + (1f / (acc.accumulationSamples + 1)), 1), acc.shutterBeginsClosing);
#endif
        }

        void EndAccumulation()
        {
#if HDRP_PRESENT
            // Needed in order to change back Time variables before continuing with normal simulation
            RenderPipelineManager.beginFrameRendering -= PrepareSubFrameCallBack;
            m_RenderPipeline?.EndRecording();
#endif
            m_AccumulatingSensorsCapturedOrNot = null;
        }

        bool m_Accumulating
        {
            get
            {
#if HDRP_PRESENT
                return m_AccumulatingSensorsCapturedOrNot != null;
#else
                return false;
#endif
            }
        }

        public void CleanUp()
        {
#if HDRP_PRESENT
            // Time variables change within PrepareSubFrameCallBack, without this the time variables keep changing
            // Even when exiting play mode
            RenderPipelineManager.beginFrameRendering -= PrepareSubFrameCallBack;
            m_RenderPipeline?.EndRecording();
#endif
        }

        float GetSequenceTimeOfNextCapture(SensorData sensorData)
        {
            // If the first capture hasn't happened yet, sequenceTimeNextCapture field won't be valid
            if (sensorData.firstCaptureTime >= UnscaledSequenceTime)
            {
                // If we are using accumulation the actual capture time needs to be on the last frame of accumulation
                // which is at sensorData.renderingDeltaTime * PerceptionSettings.instance.accumulationSettings.accumulationSamples after the normal
                // capture time
                if (SensorUsesAccumulation(sensorData))
                    return sensorData.captureTriggerMode == CaptureTriggerMode.Scheduled ? sensorData.firstCaptureTime + sensorData.renderingDeltaTime * PerceptionSettings.instance.accumulationSettings.accumulationSamples : float.MaxValue;

                return sensorData.captureTriggerMode == CaptureTriggerMode.Scheduled ? sensorData.firstCaptureTime : float.MaxValue;
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

            // the renderPipeline will be available after an indeterminate amount of frames (between 3 and 7 typically)
            // Hence, we need to wait for these frames to pass by before we start capturing when using accumulation
            if (ExecutionState == ExecutionStateType.InitializingAccumulation)
            {
                AccumulationSetup();
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

        /// <summary>
        /// returns true if all sensors that are accumulating captured last frame
        /// </summary>
        bool allAccumulatingSensorsCapturedLastFrame
        {
            get
            {
                foreach (var sensor in m_AccumulatingSensorsCapturedOrNot)
                {
                    if (!sensor.Value)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// returns true if at least one sensor will start accumulation this frame
        /// </summary>
        bool oneOrMoreSensorsAccumulateThisFrame
        {
            get
            {
                foreach (var otherSensor in m_ActiveSensors)
                {
                    var otherSensorData = m_Sensors[otherSensor];
                    if (SensorUsesAccumulation(otherSensorData) && m_AccumulatingSensorsCapturedOrNot != null && m_AccumulatingSensorsCapturedOrNot.ContainsKey(otherSensor))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // This helps us keep track of which sensors have been set to accumulate this frame
        // the boolean value keeps track of whether or not the sensor has captured already
        IDictionary<SensorHandle, Boolean> m_AccumulatingSensorsCapturedOrNot;
        // This helps us keep track of which non accumulating sensors have been updated by an accumulation
        List<SensorHandle> m_NonAccumulatingSensorsThatHaveBeenUpdatedByAccumulation;
        // This helps us keep track of which accumulating sensors THAT DO NOT ACCUMULATE IN THE CURRENT FRAME have been updated by an accumulation
        List<SensorHandle> m_OtherAccumulatingSensorsUpdatedByAccumulation;
        void UpdateRunning()
        {
            EnsureSequenceTimingsUpdated();
            m_NonAccumulatingSensorsThatHaveBeenUpdatedByAccumulation = new List<SensorHandle>();
            m_OtherAccumulatingSensorsUpdatedByAccumulation = new List<SensorHandle>();

            if (m_Accumulating && allAccumulatingSensorsCapturedLastFrame)
            {
                EndAccumulation();
            }

            //update the active sensors sequenceTimeNextCapture and lastCaptureFrameCount
            foreach (var activeSensor in m_ActiveSensors)
            {
                var sensorData = m_Sensors[activeSensor];
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPaused && !usingAccumulation)
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
                    // Means this frame fulfills this sensor's simulation time requirements, we can move target to next frame.
                    sensorData.sequenceTimeOfNextRender += sensorData.renderingDeltaTime;
                }

                if (activeSensor.ShouldAccumulateThisFrame)
                {
                    // If this is the first sensor to start accumulation on this frame then create a new Dictionary
                    // and start the accumulation
                    // However if this is not the first one then the accumulation already started and there is no need
                    // to start accumulation again
                    if (!m_Accumulating)
                    {
                        m_AccumulatingSensorsCapturedOrNot = new Dictionary<SensorHandle, Boolean>();
                        StartAccumulation();
                    }

                    // add the current sensor to the dictionary
                    m_AccumulatingSensorsCapturedOrNot[activeSensor] = false;

                    if (sensorData.captureTriggerMode.Equals(CaptureTriggerMode.Scheduled))
                    {
                        var timeToLastFrameOfAccumulation = sensorData.renderingDeltaTime * PerceptionSettings.instance.accumulationSettings.accumulationSamples;
                        var timeToFrameAfterAccumulationEnds = timeToLastFrameOfAccumulation + sensorData.renderingDeltaTime;
                        var timeBeforeNextAccumulation = timeToFrameAfterAccumulationEnds + sensorData.renderingDeltaTime * sensorData.framesBetweenCaptures;

                        // Now that this sensor is accumulating we need to update the timings of all other sensors
                        foreach (var otherSensor in m_ActiveSensors)
                        {
                            // This gives us all sensors that haven't started accumulation this frame (at least not yet)
                            if (!m_AccumulatingSensorsCapturedOrNot.ContainsKey(otherSensor))
                            {
                                var otherSensorData = m_Sensors[otherSensor];
                                var timeDelayToAdd = timeToLastFrameOfAccumulation;
                                // if it does use accumulation but not on this frame then increase its timings by the length of the current accumulation
                                if (SensorUsesAccumulation(otherSensorData) && otherSensorData.sequenceTimeOfNextAccumulation - sensorData.sequenceTimeOfNextAccumulation > k_SimulationTimingAccuracy && !m_OtherAccumulatingSensorsUpdatedByAccumulation.Contains(otherSensor))
                                {
                                    m_OtherAccumulatingSensorsUpdatedByAccumulation.Add(otherSensor);
                                    otherSensorData.sequenceTimeOfNextAccumulation += timeDelayToAdd;
                                    otherSensorData.sequenceTimeOfNextCapture += timeDelayToAdd;
                                    m_Sensors[otherSensor] = otherSensorData;
                                }
                                // if it is not an accumulating sensor and it is not capturing on this frame then increase its timings by the length of the current accumulation
                                // and we have not already updated it's value previously
                                else if (!SensorUsesAccumulation(otherSensorData) && otherSensorData.sequenceTimeOfNextCapture - sensorData.sequenceTimeOfNextAccumulation > k_SimulationTimingAccuracy && !m_NonAccumulatingSensorsThatHaveBeenUpdatedByAccumulation.Contains(otherSensor))
                                {
                                    m_NonAccumulatingSensorsThatHaveBeenUpdatedByAccumulation.Add(otherSensor);
                                    otherSensorData.sequenceTimeOfNextCapture += timeDelayToAdd;
                                    m_Sensors[otherSensor] = otherSensorData;
                                }
                            }
                        }
                        // update the current sensor timings for the next accumulation
                        sensorData.sequenceTimeOfNextAccumulation += timeBeforeNextAccumulation;

                        Debug.Assert(sensorData.sequenceTimeOfNextAccumulation > UnscaledSequenceTime,
                            $"Next scheduled accumulation should be after {UnscaledSequenceTime} but is {sensorData.sequenceTimeOfNextAccumulation}");
                        while (sensorData.sequenceTimeOfNextAccumulation <= UnscaledSequenceTime)
                            sensorData.sequenceTimeOfNextAccumulation += timeBeforeNextAccumulation;
                    }
                    else if (sensorData.captureTriggerMode.Equals(CaptureTriggerMode.Manual))
                    {
                        sensorData.sequenceTimeOfNextAccumulation = float.MaxValue;
                    }
                }
                if (activeSensor.ShouldCaptureThisFrame)
                {
                    if (sensorData.captureTriggerMode.Equals(CaptureTriggerMode.Scheduled))
                    {
                        if (usingAccumulation)
                        {
                            // if the current sensor uses accumulation, or another sensor will start accumulation this frame and the current sensor is trying to capture exactly when another sensor is starting to accumulate (hence it is not part of the UpdatedSensors)
                            if (SensorUsesAccumulation(sensorData) || (oneOrMoreSensorsAccumulateThisFrame && !m_NonAccumulatingSensorsThatHaveBeenUpdatedByAccumulation.Contains(activeSensor)))
                            {
                                // if it doesn't use accumulation add it to the updated sensors
                                if (!SensorUsesAccumulation(sensorData))
                                {
                                    m_NonAccumulatingSensorsThatHaveBeenUpdatedByAccumulation.Add(activeSensor);
                                }
                                // Move the time of next capture past the accumulation that is about to start
                                sensorData.sequenceTimeOfNextCapture += sensorData.renderingDeltaTime * (sensorData.framesBetweenCaptures + PerceptionSettings.instance.accumulationSettings.accumulationSamples + 1);
                            }
                            // if it doesn't use accumulation (and there are no accumulations happening this frame)
                            else if (!SensorUsesAccumulation(sensorData))
                            {
                                m_NonAccumulatingSensorsThatHaveBeenUpdatedByAccumulation.Add(activeSensor);
                                sensorData.sequenceTimeOfNextCapture += sensorData.renderingDeltaTime * (sensorData.framesBetweenCaptures + 1);
                            }
                        }
                        else
                        {
                            sensorData.sequenceTimeOfNextCapture += sensorData.renderingDeltaTime * (sensorData.framesBetweenCaptures + 1);
                        }

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
                    // update sensors just captured and were accumulating
                    if (SensorUsesAccumulation(sensorData))
                    {
                        m_AccumulatingSensorsCapturedOrNot[activeSensor] = true;
                    }
                }

                m_Sensors[activeSensor] = sensorData;
            }

            // only update renderDeltaTime if no sensors will accumulate this frame
            if (!m_Accumulating)
            {
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
                
                Time.captureDeltaTime = nextFrameDt;
            }

            ReportFramesToConsumer();
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
                m_SimulationMetadata.Add("totalFrames", m_TotalFrames + frameOffset);
                m_SimulationMetadata.Add("totalSequences", sequenceId + 1);

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
            if (SensorUsesAccumulation(data))
            {
                data.sequenceTimeOfNextAccumulation = UnscaledSequenceTime;
                data.sequenceTimeOfNextCapture = UnscaledSequenceTime + PerceptionSettings.instance.accumulationSettings.accumulationSamples * data.renderingDeltaTime;
            }
            else
            {
                data.sequenceTimeOfNextCapture = UnscaledSequenceTime;
            }

            m_Sensors[sensorHandle] = data;
        }

        public bool ShouldAccumulateThisFrame(SensorHandle sensorHandle)
        {
            if (!m_Sensors.ContainsKey(sensorHandle))
                return false;

            var data = m_Sensors[sensorHandle];
            if (!SensorUsesAccumulation(data))
                return false;

            return data.sequenceTimeOfNextAccumulation - UnscaledSequenceTime < k_SimulationTimingAccuracy;
        }

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
            var id = PendingId.CreateSensorId(sequenceId, step, handle.Id);
            var pendingFrame = GetOrCreatePendingFrame(id);
            pendingFrame.AddSensor(id, sensor);
            return id;
        }

        internal PendingId ReportAnnotation(SensorHandle sensorHandle, AnnotationDefinition definition, Annotation annotation)
        {
            var step = AcquireStep();
            var sensorId = PendingId.CreateSensorId(sequenceId, step, sensorHandle.Id);

            var pendingFrame = GetOrCreatePendingFrame(sensorId);
            var sensor = pendingFrame.GetOrCreatePendingSensor(sensorId);

            var annotationId = PendingId.CreateAnnotationId(sequenceId, step, sensorHandle.Id, definition.id);

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
            dataCaptured = true;

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

            var pendingId = PendingId.CreateMetricId(sequenceId, AcquireStep(), sensorId, annotationId, definition.id);

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

            var pendingId = PendingId.CreateMetricId(sequenceId, AcquireStep(), sensor.Id, annotation.Id, definition.id);
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

            // in case of simulation restore - include offset to the output frame
            frameId += frameOffset;

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

                if ((writeCapturesFromThisFrame || recordedFrame < Time.frameCount) &&
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
                    continue;
                }

                consumerEndpoint.FrameGenerated(converted);
            }
        }
    }
}
