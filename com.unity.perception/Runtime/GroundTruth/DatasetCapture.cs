using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.Settings;

#pragma warning disable 649
namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Global manager for frame scheduling and output capture for simulations.
    /// Data capture follows the schema defined in *TODO: Expose schema publicly*
    /// </summary>
    public static class DatasetCapture
    {
        static SimulationState m_ActiveSimulation;

        static DatasetCapture()
        {
            Application.wantsToQuit += () =>
            {
                ResetSimulation();
                return true;
            };
        }

        internal static SimulationState currentSimulation => m_ActiveSimulation ?? (m_ActiveSimulation = CreateSimulationData());

        /// <summary>
        /// The json metadata schema version the DatasetCapture's output conforms to.
        /// </summary>
        public static string schemaVersion => "0.0.1";

        /// <summary>
        /// The current perception version
        /// </summary>
        public static string perceptionVersion => "0.10.0-preview.1"; // TODO: Can we automate this?

        internal static int currentFrame => currentSimulation.currentFrame;

        /// <summary>
        /// Called when the simulation ends. The simulation ends on playmode exit, application exit, or when <see cref="ResetSimulation"/> is called.
        /// </summary>
        public static event Action SimulationEnding;

        /// <summary>
        /// Registers a sensor with the current simulation.
        /// </summary>
        /// <param name="sensor">The sensor to register</param>
        /// <returns>A handle to the registered sensor</returns>
        public static SensorHandle RegisterSensor(SensorDefinition sensor)
        {
            return currentSimulation.AddSensor(sensor, sensor.simulationDeltaTime);
        }

        /// <summary>
        /// Registers a metric with the running simulation.
        /// </summary>
        /// <param name="metricDefinition">The metric to register</param>
        public static void RegisterMetric(MetricDefinition metricDefinition)
        {
            currentSimulation.RegisterMetric(metricDefinition);
        }

        /// <summary>
        /// Registers an annotation with the running simulation.
        /// </summary>
        /// <param name="definition">The annotation to register</param>
        public static void RegisterAnnotationDefinition(AnnotationDefinition definition)
        {
            currentSimulation.RegisterAnnotationDefinition(definition);
        }

        /// <summary>
        /// Retrieves the sequence and step numbers from a simulation frame.
        /// </summary>
        /// <param name="frame">The simulation frame</param>
        /// <returns>The sequence and step numbers</returns>
        public static (int sequence, int step) GetSequenceAndStepFromFrame(int frame)
        {
            return currentSimulation.GetSequenceAndStepFromFrame(frame);
        }

        /// <summary>
        /// Reports a metric for the activate frame that is not associated with a sensor or an annotation. To
        /// report a metric associated with an annotation use <see cref="AnnotationHandle.ReportMetric"/>, to
        /// report a metric associated with a sensor user <see cref="SensorHandle.ReportMetric"/>.
        /// </summary>
        /// <param name="definition">The metric definition to use</param>
        /// <param name="metric">The metric to report</param>
        public static void ReportMetric(MetricDefinition definition, Metric metric)
        {
            currentSimulation.ReportMetric(definition, metric, null, null);
        }

        public static AsyncFuture<Metric> ReportMetric(MetricDefinition definition)
        {
            return currentSimulation.CreateAsyncMetric(definition);
        }

        /// <summary>
        ///  Report simulation metadata
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public static void ReportMetadata(string key, int value)
        {
            currentSimulation.ReportMetadata(key, value);
        }

        /// <summary>
        ///  Report simulation metadata
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public static void ReportMetadata(string key, float value)
        {
            currentSimulation.ReportMetadata(key, value);
        }

        /// <summary>
        ///  Report simulation metadata
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public static void ReportMetadata(string key, uint value)
        {
            currentSimulation.ReportMetadata(key, value);
        }

        /// <summary>
        ///  Report simulation metadata
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public static void ReportMetadata(string key, string value)
        {
            currentSimulation.ReportMetadata(key, value);
        }

        /// <summary>
        ///  Report simulation metadata
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public static void ReportMetadata(string key, bool value)
        {
            currentSimulation.ReportMetadata(key, value);
        }

        /// <summary>
        ///  Report simulation metadata
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public static void ReportMetadata(string key, int[] value)
        {
            currentSimulation.ReportMetadata(key, value);
        }

        /// <summary>
        ///  Report simulation metadata
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public static void ReportMetadata(string key, float[] value)
        {
            currentSimulation.ReportMetadata(key, value);
        }

        /// <summary>
        ///  Report simulation metadata
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public static void ReportMetadata(string key, string[] value)
        {
            currentSimulation.ReportMetadata(key, value);
        }

        /// <summary>
        ///  Report simulation metadata
        /// </summary>
        /// <param name="key">The key of the metadata</param>
        /// <param name="value">The value of the metadata</param>
        public static void ReportMetadata(string key, bool[] value)
        {
            currentSimulation.ReportMetadata(key, value);
        }

        /// <summary>
        /// Starts a new sequence in the capture.
        /// </summary>
        public static void StartNewSequence() => currentSimulation.StartNewSequence();

        internal static bool IsValid(string id) => id != null && currentSimulation.Contains(id);

        static IConsumerEndpoint m_OverrideEndpoint;

        /// <summary>
        /// Retrieve a handle to the active endpoint.
        /// </summary>
        public static IConsumerEndpoint activateEndpoint => m_ActiveSimulation.consumerEndpoint;

        /// <summary>
        /// Sets the current output path for <see cref="IFileSystemEndpoint"/> endpoints. This will set the path for the next simulation, it will
        /// not affect a simulation that is currently executing. In order for this to take effect the caller should call
        /// <see cref="ResetSimulation()"/>
        /// </summary>
        /// <remarks>If the current endpoint is not an <see cref="IFileSystemEndpoint"/> the value will be unused</remarks>
        /// <param name="outputPath">The path to set</param>
        public static void SetOutputPath(string outputPath)
        {
            PerceptionSettings.instance.SetOutputBasePath(outputPath);
        }

        internal static void OverrideEndpoint(IConsumerEndpoint endpoint)
        {
            m_OverrideEndpoint = endpoint;
        }

        static SimulationState CreateSimulationData()
        {
            if (m_OverrideEndpoint == null && PerceptionSettings.instance.endpoint == null)
            {
                throw new InvalidOperationException("An endpoint has not been set for dataset capture");
            }

            var endpoint = m_OverrideEndpoint ?? PerceptionSettings.instance.endpoint.Clone();
            return new SimulationState(endpoint as IConsumerEndpoint);
        }

        internal static void Update()
        {
            currentSimulation.Update();
        }

        /// <summary>
        /// Shuts down the active simulation and starts a new simulation.
        /// </summary>
        public static void ResetSimulation()
        {
            SimulationEnding?.Invoke();

            if (m_ActiveSimulation != null &&  m_ActiveSimulation.IsRunning())
            {
                var old = m_ActiveSimulation;
                m_ActiveSimulation = null;
                old.End();
            }

            m_ActiveSimulation = CreateSimulationData();
        }
    }

    class PendingId
    {
        internal static PendingId CreateSensorId(int sequence, int step, string sensorId)
        {
            return new PendingId(FutureType.Sensor, sequence, step, sensorId, string.Empty,string.Empty);
        }

        internal static PendingId CreateMetricId(int sequence, int step, string metricId)
        {
            return new PendingId(FutureType.Metric, sequence, step, string.Empty, string.Empty, metricId);
        }

        internal static PendingId CreateMetricId(int sequence, int step, string sensorId, string metricId)
        {
            return new PendingId(FutureType.Metric, sequence, step, sensorId, string.Empty, metricId);
        }

        internal static PendingId CreateMetricId(int sequence, int step, string sensorId, string annotationId, string metricId)
        {
            return new PendingId(FutureType.Metric, sequence, step, sensorId, annotationId, metricId);
        }

        internal static PendingId CreateAnnotationId(int sequence, int step, string sensorId, string annotationId)
        {
            return new PendingId(FutureType.Annotation, sequence, step, sensorId, annotationId, string.Empty);
        }

        PendingId(FutureType futureType, int sequence, int step, string sensorId, string annotationId, string metricId)
        {
            FutureType = futureType;
            Sequence = sequence;
            Step = step;
            SensorId = sensorId;
            this.annotationId = annotationId;
            MetricId = metricId;
        }

        internal FutureType FutureType { get; }

        internal int Sequence { get; }

        internal int Step { get; }

        internal string SensorId { get; }
        internal string annotationId { get; }

        internal string MetricId { get; }

        bool isBaseValid => Sequence > -1 && Step > -1;

        public override string ToString()
        {
            return $"{FutureType} {SensorId} {annotationId ?? MetricId}[{Sequence}, {Step}]";
        }

        internal bool IsValidSensorId =>
            // Do not check if it's a sensor ID because both annotation and (some) metric IDs can be used to
            // load sensors
            isBaseValid && !string.IsNullOrEmpty(SensorId);

        internal bool IsValidMetricId =>
            isBaseValid &&
            FutureType == FutureType.Metric &&
            !string.IsNullOrEmpty(MetricId);

        internal bool IsValidAnnotationId =>
            isBaseValid &&
            FutureType == FutureType.Annotation &&
            !string.IsNullOrEmpty(SensorId) &&
            !string.IsNullOrEmpty(annotationId);

        public override bool Equals(object obj)
        {
            if (obj is PendingId other)
            {
                if (other.FutureType != FutureType) return false;
                if (other.Sequence != Sequence) return false;
                if (other.Step != Step) return false;

                switch (FutureType)
                {
                   case FutureType.Metric:
                        if (other.MetricId != MetricId) return false;
                        if (other.annotationId != annotationId) return false;
                        return other.SensorId == SensorId;
                   case FutureType.Annotation:
                        if (other.annotationId != annotationId) return false;
                        return other.SensorId == SensorId;
                   case FutureType.Sensor:
                        return other.SensorId == SensorId;
                    default:
                        return true;
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            var hc = (Sequence * 397) ^ Step;
            hc = (SensorId != null ? SensorId.GetHashCode() : 0 * 397) ^ hc;
            hc = (annotationId != null ? annotationId.GetHashCode() : 0 * 397) ^ hc;
            return (MetricId != null ? MetricId.GetHashCode() : 0 * 397) ^ hc;

        }
    }

    enum FutureType
    {
        Sensor,
        Metric,
        Annotation
    }

    /// <summary>
    /// A handle back to a result that will be calculated in the future. This class is used to report an asynchronous
    /// solution.
    /// </summary>
    /// <typeparam name="T">The data type of the future (either a sensor, annotation, or metric)</typeparam>
    public readonly struct AsyncFuture<T> where T : DataModelElement
    {
        internal static AsyncFuture<Sensor> CreateSensorFuture(PendingId id, SimulationState simState)
        {
            return new AsyncFuture<Sensor>(id, simState);
        }

        internal static AsyncFuture<Metric> CreateMetricFuture(PendingId id, SimulationState simState)
        {
            return new AsyncFuture<Metric>(id, simState);
        }

        internal static AsyncFuture<Annotation> CreateAnnotationFuture(PendingId id, SimulationState simState)
        {
            return new AsyncFuture<Annotation>(id, simState);
        }

        AsyncFuture(PendingId id, SimulationState simulationState)
        {
            pendingId = id;
            this.simulationState = simulationState;
        }

        SimulationState simulationState { get; }
        internal PendingId pendingId { get; }

        internal FutureType futureType => pendingId.FutureType;

        /// <summary>
        /// Is the future valid?
        /// </summary>
        /// <returns>Is the future valid?</returns>
        public bool IsValid()
        {
            return simulationState != null && simulationState.IsRunning();
        }

        /// <summary>
        /// Is this future still pending?
        /// </summary>
        /// <returns>Is this future still pending</returns>
        public bool IsPending()
        {
            return simulationState.IsPending(this);
        }

        /// <summary>
        /// Report the result that this future has been waiting for.
        /// </summary>
        /// <param name="toReport">The value to report</param>
        public void Report(T toReport)
        {
            simulationState.ReportAsyncResult(this, toReport);
        }
    }

    public interface IDatasetHandle
    {
        string Id { get; }
        bool IsValid();
    }

    /// <summary>
    /// A handle to a sensor managed by the <see cref="DatasetCapture"/>. It can be used to check whether the sensor
    /// is expected to capture this frame and report captures, annotations, and metrics regarding the sensor.
    /// </summary>
    public struct SensorHandle : IDisposable, IEquatable<SensorHandle>
    {
        /// <summary>
        /// The Id of the sensor
        /// </summary>
        public string Id { get; }

        public bool valid => DatasetCapture.IsValid(Id);

        internal SensorHandle(string id)
        {
            Id = id ?? string.Empty;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Id;
        }

        /// <summary>
        /// Whether the sensor is currently enabled. When disabled, the DatasetCapture will no longer schedule frames for running captures on this sensor.
        /// </summary>
        bool enabled
        {
            get => DatasetCapture.currentSimulation.IsEnabled(this);
            set
            {
                CheckValid();
                DatasetCapture.currentSimulation.SetEnabled(this, value);
            }
        }

        /// <summary>
        /// Reports an annotation for the sensor.
        /// </summary>
        /// <param name="definition">The annotation definition</param>
        /// <param name="annotation">The annotation value</param>
        /// <exception cref="InvalidOperationException">Thrown when an annotation is reported for a frame that should not be captured</exception>
        /// <exception cref="ArgumentException">Thrown when an annotation is reported with an invalid annotation definition</exception>
        public AnnotationHandle ReportAnnotation(AnnotationDefinition definition, Annotation annotation)
        {
            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException("Annotation reported on SensorHandle in frame when its ShouldCaptureThisFrame is false.");
            if (!definition.IsValid())
                throw new ArgumentException("The given annotationDefinition is invalid", nameof(definition));

            DatasetCapture.currentSimulation.ReportAnnotation(this, definition, annotation);

            return new AnnotationHandle(this, definition);
        }

        /// <summary>
        /// Creates an async annotation for reporting the values for an annotation during a future frame.
        /// </summary>
        /// <param name="annotationDefinition">The AnnotationDefinition of this annotation.</param>
        /// <returns>Returns a handle to the <see cref="AsyncFuture{T}"/>, which can be used to report annotation data during a subsequent frame.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this method is called during a frame where <see cref="ShouldCaptureThisFrame"/> is false.</exception>
        /// <exception cref="ArgumentException">Thrown if the given AnnotationDefinition is invalid.</exception>
        public AsyncFuture<Annotation> ReportAnnotationAsync(AnnotationDefinition annotationDefinition)
        {
            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException("Annotation reported on SensorHandle in frame when its ShouldCaptureThisFrame is false.");
            if (!annotationDefinition.IsValid())
                throw new ArgumentException("The given annotationDefinition is invalid", nameof(annotationDefinition));

            return DatasetCapture.currentSimulation.ReportAnnotationAsync(annotationDefinition, this);
        }

        /// <summary>
        /// Creates an async sensor for reporting the values for a sensor during a future frame.
        /// </summary>
        /// <returns>Returns a handle to the <see cref="AsyncFuture{T}"/>, which can be used to report annotation data during a subsequent frame.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this method is called during a frame where <see cref="ShouldCaptureThisFrame"/> is false.</exception>
        /// <exception cref="ArgumentException">Thrown if the given AnnotationDefinition is invalid.</exception>
        public AsyncFuture<Sensor> ReportSensorAsync()
        {
            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException("Sensor capture reported on SensorHandle in frame when its ShouldCaptureThisFrame is false.");
            if (!IsValid)
                throw new ArgumentException($"The sensor is invalid {Id}");

            return DatasetCapture.currentSimulation.ReportSensorAsync(this);
        }

        /// <summary>
        /// Reports a sensor capture immediately for the current frame
        /// </summary>
        /// <param name="sensor">The capture to report</param>
        /// <exception cref="InvalidOperationException">Thrown if this method is called during a frame where <see cref="ShouldCaptureThisFrame"/> is false.</exception>
        /// <exception cref="ArgumentException">Thrown if the given AnnotationDefinition is invalid.</exception>
        public void ReportSensor(Sensor sensor)
        {
            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException("Annotation reported a sensor in frame when its ShouldCaptureThisFrame is false.");
            if (!IsValid)
                throw new ArgumentException("The sensor is invalid", Id);

            DatasetCapture.currentSimulation.ReportSensor(this, sensor);
        }

        /// <summary>
        /// Whether the sensor should capture this frame. Sensors are expected to call this method each frame to determine whether
        /// they should capture during the frame. Captures should only be reported when this is true.
        /// </summary>
        public bool ShouldCaptureThisFrame => DatasetCapture.currentSimulation.ShouldCaptureThisFrame(this);

        /// <summary>
        /// Requests a capture from this sensor on the next rendered frame. Can only be used with manual capture mode (<see cref="CaptureTriggerMode.Manual"/>).
        /// </summary>
        public void RequestCapture()
        {
            DatasetCapture.currentSimulation.SetNextCaptureTimeToNowForSensor(this);
        }

        /// <summary>
        /// Reports a metric on the current frame.
        /// </summary>
        /// <param name="definition">The metric definition</param>
        /// <param name="metric">The metric value</param>
        /// <exception cref="InvalidOperationException">Thrown when a metric is reported on a frame that should not capture</exception>
        /// <exception cref="ArgumentException">Thrown if the passed in metric is invalid</exception>
        public void ReportMetric(MetricDefinition definition, Metric metric)
        {
            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException("Metric reported on SensorHandle in frame when its ShouldCaptureThisFrame is false.");
            if (!IsValid)
                throw new ArgumentException("The given metric is invalid", Id);

            DatasetCapture.currentSimulation.ReportMetric(definition, metric, this.Id, null);
        }

        /// <summary>
        /// Start an async metric for reporting metric values for this frame in a subsequent frame.
        /// </summary>
        /// <param name="metricDefinition">The <see cref="MetricDefinition"/> of the metric</param>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ShouldCaptureThisFrame"/> is false</exception>
        /// <returns>An <see cref="AsyncFuture{T}"/> which should be used to report the metric values, potentially in a later frame</returns>
        public AsyncFuture<Metric> ReportMetricAsync(MetricDefinition metricDefinition)
        {
            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException($"Sensor-based metrics may only be reported when SensorHandle.ShouldCaptureThisFrame is true");
            if (!metricDefinition.IsValid())
                throw new ArgumentException("The passed in metric definition is invalid", nameof(metricDefinition));

            return DatasetCapture.currentSimulation.CreateAsyncMetric(metricDefinition, this);
        }

        /// <summary>
        /// Dispose this SensorHandle.
        /// </summary>
        public void Dispose()
        {
            this.enabled = false;
        }

        /// <summary>
        /// Returns whether this SensorHandle is valid in the current simulation. Nil SensorHandles are never valid.
        /// </summary>
        public bool IsValid => DatasetCapture.IsValid(this.Id);

        /// <summary>
        /// Returns true if this SensorHandle was default-instantiated.
        /// </summary>
        public bool IsNil => this == default;

        void CheckValid()
        {
            if (!DatasetCapture.IsValid(this.Id))
                throw new InvalidOperationException("SensorHandle has been disposed or its simulation has ended");
        }

        /// <inheritdoc/>
        public bool Equals(SensorHandle other)
        {
            switch (Id)
            {
                case null when other.Id == null:
                    return true;
                case null:
                    return false;
                default:
                    return Id.Equals(other.Id);
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is SensorHandle other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Compares two <see cref="SensorHandle"/> instances for equality.
        /// </summary>
        /// <param name="left">The first SensorHandle.</param>
        /// <param name="right">The second SensorHandle.</param>
        /// <returns>Returns true if the two SensorHandles refer to the same sensor.</returns>
        public static bool operator==(SensorHandle left, SensorHandle right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="SensorHandle"/> instances for inequality.
        /// </summary>
        /// <param name="left">The first SensorHandle.</param>
        /// <param name="right">The second SensorHandle.</param>
        /// <returns>Returns false if the two SensorHandles refer to the same sensor.</returns>
        public static bool operator!=(SensorHandle left, SensorHandle right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// A handle to an annotation. Can be used to report metrics on the annotation.
    /// </summary>
    public readonly struct AnnotationHandle : IDatasetHandle, IEquatable<AnnotationHandle>
    {
        internal AnnotationHandle(SensorHandle sensorHandle, AnnotationDefinition definition)
        {
            m_SensorHandle = sensorHandle;
            m_Definition = definition;
        }

        readonly AnnotationDefinition m_Definition;

        public bool IsValid() => m_Definition != null;


        /// <summary>
        /// The ID of the annotation which will be used in the json metadata.
        /// </summary>
        public string Id => m_Definition != null ? m_Definition.id : string.Empty;

        /// <summary>
        /// The SensorHandle on which the annotation was reported
        /// </summary>
        readonly SensorHandle m_SensorHandle;

        /// <summary>
        /// Returns true if the annotation is nil (created using default instantiation).
        /// </summary>
        public bool IsNil => Id == string.Empty;

        /// <inheritdoc/>
        public bool Equals(AnnotationHandle other)
        {
            return m_SensorHandle.Equals(other.m_SensorHandle) && m_Definition.Equals(other.m_Definition);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is AnnotationHandle other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = (Id != null ? StringComparer.InvariantCulture.GetHashCode(Id) : 0);
            return hash;
        }

        /// <summary>
        /// Synchronously report a metric for the current simulation frame.
        /// </summary>
        /// <param name="definition">The definition of the metric to report</param>
        /// <param name="metric">The metric value</param>
        public void ReportMetric(MetricDefinition definition, Metric metric)
        {
            if (!m_SensorHandle.ShouldCaptureThisFrame)
                throw new InvalidOperationException("Metric reported on SensorHandle in frame when its ShouldCaptureThisFrame is false.");
            if (!metric.IsValid())
                throw new ArgumentException("The given metric is invalid", Id);
            DatasetCapture.currentSimulation.ReportMetric(definition, metric, m_SensorHandle.Id, this.Id);
        }

        /// <summary>
        /// Report a metric whose values will be supplied in a later frame.
        /// </summary>
        /// <param name="metricDefinition">The type of the metric.</param>
        /// <returns>A handle to an AsyncMetric, which can be used to report values for this metric in future frames.</returns>
        public AsyncFuture<Metric> ReportMetricAsync(MetricDefinition metricDefinition) => DatasetCapture.currentSimulation.CreateAsyncMetric(metricDefinition, m_SensorHandle, this);
    }
}
