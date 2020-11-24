using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Simulation;
using UnityEngine;

#pragma warning disable 649
namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Global manager for frame scheduling and output capture for simulations.
    /// Data capture follows the schema defined in *TODO: Expose schema publicly*
    /// </summary>
    public static class DatasetCapture
    {
        static readonly Guid k_DatasetGuid = Guid.NewGuid();
        internal static SimulationState SimulationState { get; private set; } = CreateSimulationData();

        internal static string OutputDirectory => SimulationState.GetOutputDirectoryNoCreate();

        /// <summary>
        /// The json metadata schema version the DatasetCapture's output conforms to.
        /// </summary>
        public static string SchemaVersion => "0.0.1";

        /// <summary>
        /// Called when the simulation ends. The simulation ends on playmode exit, application exit, or when <see cref="ResetSimulation"/> is called.
        /// </summary>
        public static event Action SimulationEnding;

        /// <summary>
        /// Register a new ego. Used along with RegisterSensor to organize sensors under a top-level ego container. <seealso cref="RegisterSensor"/>
        /// </summary>
        /// <param name="description">A human-readable description for the ego</param>
        /// <returns>An <see cref="EgoHandle"/>, which can be used to organize sensors under a common ego.</returns>
        public static EgoHandle RegisterEgo(string description)
        {
            var ego = new EgoHandle(Guid.NewGuid(), description);
            SimulationState.AddEgo(ego);
            return ego;
        }

        /// <summary>
        /// Register a new sensor under the given ego.
        /// </summary>
        /// <param name="egoHandle">The ego container for the sensor. Sensor orientation will be reported in the context of the given ego.</param>
        /// <param name="modality">The kind of the sensor (ex. "camera", "lidar")</param>
        /// <param name="description">A human-readable description of the sensor (ex. "front-left rgb camera")</param>
        /// <param name="period">The period, in seconds, on which the sensor should capture. Frames will be scheduled in the simulation such that each sensor is triggered every _period_ seconds.</param>
        /// <param name="firstCaptureTime">The time, in seconds, from the start of the sequence on which this sensor should first be scheduled.</param>
        /// <returns>A <see cref="SensorHandle"/>, which should be used to check <see cref="SensorHandle.ShouldCaptureThisFrame"/> each frame to determine whether to capture (or render) that frame.
        /// It is also used to report captures, annotations, and metrics on the sensor.</returns>
        /// <exception cref="ArgumentException">Thrown if ego is invalid.</exception>
        public static SensorHandle RegisterSensor(EgoHandle egoHandle, string modality, string description, float period, float firstCaptureTime)
        {
            if (!SimulationState.Contains(egoHandle.Id))
                throw new ArgumentException("Supplied ego is not part of the simulation.", nameof(egoHandle));

            var sensor = new SensorHandle(Guid.NewGuid());
            SimulationState.AddSensor(egoHandle, modality, description, period, firstCaptureTime, sensor);
            return sensor;
        }

        /// <summary>
        /// Creates a metric type, which can be used to produce metrics during the simulation.
        /// See <see cref="ReportMetric{T}(MetricDefinition,T[])"/>, <see cref="SensorHandle.ReportMetricAsync(MetricDefinition)"/>, <see cref="SensorHandle.ReportMetric{T}(MetricDefinition,T[])"/>,
        /// <see cref="SensorHandle.ReportMetricAsync(MetricDefinition)"/>, <see cref="Annotation.ReportMetric{T}(MetricDefinition,T[])"/>, <see cref="Annotation.ReportMetricAsync(MetricDefinition)"/>
        /// </summary>
        /// <param name="name">Human readable annotation spec name (e.g. sementic_segmentation, instance_segmentation, etc.)</param>
        /// <param name="description">Description of the annotation.</param>
        /// <param name="id">The ID for this metric. This allows metric types to be shared across simulations and sequences.</param>
        /// <returns>A MetricDefinition, which can be used during this simulation to report metrics.</returns>
        public static MetricDefinition RegisterMetricDefinition(string name, string description = null, Guid id = default)
        {
            return RegisterMetricDefinition<object>(name, null, description, id);
        }

        /// <summary>
        /// Creates a metric type, which can be used to produce metrics during the simulation.
        /// See <see cref="ReportMetric{T}(MetricDefinition,T[])"/>, <see cref="SensorHandle.ReportMetricAsync(MetricDefinition)"/>, <see cref="SensorHandle.ReportMetric{T}(MetricDefinition,T[])"/>,
        /// <see cref="SensorHandle.ReportMetricAsync(MetricDefinition)"/>, <see cref="Annotation.ReportMetric{T}(MetricDefinition,T[])"/>, <see cref="Annotation.ReportMetricAsync(MetricDefinition)"/>
        /// </summary>
        /// <param name="name">Human readable annotation spec name (e.g. sementic_segmentation, instance_segmentation, etc.)</param>
        /// <param name="description">Description of the annotation.</param>
        /// <param name="specValues">Format-specific specification for the metric values. Will be converted to json automatically.</param>
        /// <param name="id">The ID for this metric. This allows metric types to be shared across simulations and sequences.</param>
        /// <typeparam name="TSpec">The type of the <see cref="specValues"/> struct to write.</typeparam>
        /// <returns>A MetricDefinition, which can be used during this simulation to report metrics.</returns>
        public static MetricDefinition RegisterMetricDefinition<TSpec>(string name, TSpec[] specValues, string description = null, Guid id = default)
        {
            return SimulationState.RegisterMetricDefinition(name, specValues, description, id);
        }

        /// <summary>
        /// Creates an annotation type, which can be used to produce annotations during the simulation.
        /// See <see cref="SensorHandle.ReportAnnotationFile"/>, <see cref="SensorHandle.ReportAnnotationValues{T}"/> and <see cref="SensorHandle.ReportAnnotationAsync"/>.
        /// </summary>
        /// <param name="name">Human readable annotation spec name (e.g. sementic_segmentation, instance_segmentation, etc.)</param>
        /// <param name="description">Description of the annotation.</param>
        /// <param name="format">Optional format name.</param>
        /// <param name="id">The ID for this annotation type. This allows annotation types to be shared across simulations and sequences.</param>
        /// <returns>An AnnotationDefinition. If the given <see cref="id"/> has already been defined, its AnnotationDefinition is returned.</returns>
        public static AnnotationDefinition RegisterAnnotationDefinition(string name, string description = null, string format = "json", Guid id = default)
        {
            return RegisterAnnotationDefinition<object>(name, null, description, format, id);
        }

        /// <summary>
        /// Creates an annotation type, which can be used to produce annotations during the simulation.
        /// See <see cref="SensorHandle.ReportAnnotationFile"/>, <see cref="SensorHandle.ReportAnnotationValues{T}"/> and <see cref="SensorHandle.ReportAnnotationAsync"/>.
        /// </summary>
        /// <param name="name">Human readable annotation spec name (e.g. sementic_segmentation, instance_segmentation, etc.)</param>
        /// <param name="description">Description of the annotation.</param>
        /// <param name="format">Optional format name.</param>
        /// <param name="specValues">Format-specific specification for the annotation values (ex. label-value mappings for semantic segmentation images)</param>
        /// <param name="id">The ID for this annotation type. This allows annotation types to be shared across simulations and sequences.</param>
        /// <typeparam name="TSpec">The type of the values for the spec array in the resulting json.</typeparam>
        /// <returns>An AnnotationDefinition. If the given <see cref="id"/> has already been defined, its AnnotationDefinition is returned.</returns>
        public static AnnotationDefinition RegisterAnnotationDefinition<TSpec>(string name, TSpec[] specValues, string description = null, string format = "json", Guid id = default)
        {
            return SimulationState.RegisterAnnotationDefinition(name, specValues, description, format, id);
        }

        /// <summary>
        /// Report a metric not associated with any sensor or annotation.
        /// </summary>
        /// <param name="metricDefinition">The MetricDefinition associated with this metric. <see cref="RegisterMetricDefinition"/></param>
        /// <param name="values">An array to be converted to json and put in the "values" field of the metric</param>
        /// <typeparam name="T">The type of the <see cref="values"/> array</typeparam>
        public static void ReportMetric<T>(MetricDefinition metricDefinition, T[] values)
        {
            SimulationState.ReportMetric(metricDefinition, values, default, default);
        }

        /// <summary>
        /// Report a metric not associated with any sensor or annotation.
        /// </summary>
        /// <param name="metricDefinition">The MetricDefinition associated with this metric. <see cref="RegisterMetricDefinition"/></param>
        /// <param name="valuesJsonArray">A string-based JSON array to be placed in the "values" field of the metric</param>
        public static void ReportMetric(MetricDefinition metricDefinition, string valuesJsonArray)
        {
            SimulationState.ReportMetric(metricDefinition, new JRaw(valuesJsonArray), default, default);
        }

        /// <summary>
        /// Report a metric not associated with any sensor or annotation.
        /// </summary>
        /// <param name="metricDefinition">The metric definition of the metric being reported</param>
        /// <returns>An <see cref="AsyncMetric"/> which should be used to report the metric values, potentially in a later frame</returns>
        public static AsyncMetric ReportMetricAsync(MetricDefinition metricDefinition) => SimulationState.CreateAsyncMetric(metricDefinition);

        /// <summary>
        /// Starts a new sequence in the capture.
        /// </summary>
        public static void StartNewSequence() => SimulationState.StartNewSequence();

        internal static bool IsValid(Guid id) => SimulationState.Contains(id);

        static SimulationState CreateSimulationData()
        {
            //TODO: Remove the Guid path when we have proper dataset merging in Unity Simulation and Thea
            return new SimulationState($"Dataset{k_DatasetGuid}");
        }

        [RuntimeInitializeOnLoadMethod]
        static void OnInitializeOnLoad()
        {
            Manager.Instance.ShutdownNotification += ResetSimulation;
        }

        /// <summary>
        /// Stop the current simulation and start a new one. All pending data is written to disk before returning.
        /// </summary>
        public static void ResetSimulation()
        {
            //this order ensures that exceptions thrown by End() do not prevent the state from being reset
            SimulationEnding?.Invoke();
            var oldSimulationState = SimulationState;
            SimulationState = CreateSimulationData();
            oldSimulationState.End();
        }
    }

    /// <summary>
    /// A handle to a sensor managed by the <see cref="DatasetCapture"/>. It can be used to check whether the sensor
    /// is expected to capture this frame and report captures, annotations, and metrics regarding the sensor.
    /// </summary>
    public struct SensorHandle : IDisposable, IEquatable<SensorHandle>
    {
        /// <summary>
        /// The unique ID of the sensor. This ID is used to refer to this sensor in the json metadata.
        /// </summary>
        public Guid Id { get; }

        internal SensorHandle(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// Whether the sensor is currently enabled. When disabled, the DatasetCapture will no longer schedule frames for running captures on this sensor.
        /// </summary>
        public bool Enabled
        {
            get => DatasetCapture.SimulationState.IsEnabled(this);
            set
            {
                CheckValid();
                DatasetCapture.SimulationState.SetEnabled(this, value);
            }
        }

        /// <summary>
        /// Report a file-based annotation related to this sensor in this frame.
        /// </summary>
        /// <param name="annotationDefinition">The AnnotationDefinition of this annotation.</param>
        /// <param name="filename">The path to the file containing the annotation data.</param>
        /// <returns>A handle to the reported annotation for reporting annotation-based metrics.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this method is called during a frame where <see cref="ShouldCaptureThisFrame"/> is false.</exception>
        /// <exception cref="ArgumentException">Thrown if the given AnnotationDefinition is invalid.</exception>
        public Annotation ReportAnnotationFile(AnnotationDefinition annotationDefinition, string filename)
        {
            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException("Annotation reported on SensorHandle in frame when its ShouldCaptureThisFrame is false.");
            if (!annotationDefinition.IsValid)
                throw new ArgumentException("The given annotationDefinition is invalid", nameof(annotationDefinition));

            return DatasetCapture.SimulationState.ReportAnnotationFile(annotationDefinition, this, filename);
        }

        /// <summary>
        /// Report a value-based annotation related to this sensor in this frame.
        /// </summary>
        /// <param name="annotationDefinition">The AnnotationDefinition of this annotation.</param>
        /// <param name="values">The annotation data, which will be automatically converted to json.</param>
        /// <typeparam name="T">The type of the values array.</typeparam>
        /// <returns>Returns a handle to the reported annotation for reporting annotation-based metrics.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this method is called during a frame where <see cref="ShouldCaptureThisFrame"/> is false.</exception>
        /// <exception cref="ArgumentException">Thrown if the given AnnotationDefinition is invalid.</exception>
        public Annotation ReportAnnotationValues<T>(AnnotationDefinition annotationDefinition, T[] values)
        {
            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException("Annotation reported on SensorHandle in frame when its ShouldCaptureThisFrame is false.");
            if (!annotationDefinition.IsValid)
                throw new ArgumentException("The given annotationDefinition is invalid", nameof(annotationDefinition));

            return DatasetCapture.SimulationState.ReportAnnotationValues(annotationDefinition, this, values);
        }

        /// <summary>
        /// Creates an async annotation for reporting the values for an annotation during a future frame.
        /// </summary>
        /// <param name="annotationDefinition">The AnnotationDefinition of this annotation.</param>
        /// <returns>Returns a handle to the <see cref="AsyncAnnotation"/>, which can be used to report annotation data during a subsequent frame.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this method is called during a frame where <see cref="ShouldCaptureThisFrame"/> is false.</exception>
        /// <exception cref="ArgumentException">Thrown if the given AnnotationDefinition is invalid.</exception>
        public AsyncAnnotation ReportAnnotationAsync(AnnotationDefinition annotationDefinition)
        {
            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException("Annotation reported on SensorHandle in frame when its ShouldCaptureThisFrame is false.");
            if (!annotationDefinition.IsValid)
                throw new ArgumentException("The given annotationDefinition is invalid", nameof(annotationDefinition));

            return DatasetCapture.SimulationState.ReportAnnotationAsync(annotationDefinition, this);
        }

        /// <summary>
        /// Report a sensor capture recorded to disk. This should be called on the same frame as the capture is taken, and may be called before the file is written to disk.
        /// </summary>
        /// <param name="filename">The path to the capture data.</param>
        /// <param name="sensorSpatialData">Spatial data describing the sensor and the ego containing it.</param>
        /// <param name="additionalSensorValues">Additional values to be emitted as json name/value pairs on the sensor object under the capture.</param>
        /// <exception cref="InvalidOperationException">Thrown if ReportCapture is being called when ShouldCaptureThisFrame is false or it has already been called this frame.</exception>
        public void ReportCapture(string filename, SensorSpatialData sensorSpatialData, params(string, object)[] additionalSensorValues)
        {
            if (!ShouldCaptureThisFrame)
            {
                throw new InvalidOperationException("Capture reported in frame when ShouldCaptureThisFrame is false.");
            }

            DatasetCapture.SimulationState.ReportCapture(this, filename, sensorSpatialData, additionalSensorValues);
        }

        /// <summary>
        /// Whether the sensor should capture this frame. Sensors are expected to call this method each frame to determine whether
        /// they should capture during the frame. Captures should only be reported when this is true.
        /// </summary>
        public bool ShouldCaptureThisFrame => DatasetCapture.SimulationState.ShouldCaptureThisFrame(this);

        /// <summary>
        /// Report a metric regarding this sensor in the current frame.
        /// </summary>
        /// <param name="metricDefinition">The <see cref="MetricDefinition"/> of the metric.</param>
        /// <param name="values">An array to be converted to json and put in the "values" field of the metric</param>
        /// <typeparam name="T">The value type</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if values is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ShouldCaptureThisFrame"/> is false.</exception>
        public void ReportMetric<T>(MetricDefinition metricDefinition, [NotNull] T[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException($"Sensor-based metrics may only be reported when SensorHandle.ShouldCaptureThisFrame is true");

            DatasetCapture.SimulationState.ReportMetric(metricDefinition, values, this, default);
        }

        /// <summary>
        /// Report a metric regarding this sensor in the current frame.
        /// </summary>
        /// <param name="metricDefinition">The <see cref="MetricDefinition"/> of the metric.</param>
        /// <param name="valuesJsonArray">A string-based JSON array to be placed in the "values" field of the metric</param>
        /// <exception cref="ArgumentNullException">Thrown if values is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ShouldCaptureThisFrame"/> is false.</exception>
        public void ReportMetric(MetricDefinition metricDefinition, [NotNull] string valuesJsonArray)
        {
            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException($"Sensor-based metrics may only be reported when SensorHandle.ShouldCaptureThisFrame is true");

            DatasetCapture.SimulationState.ReportMetric(metricDefinition, new JRaw(valuesJsonArray), this, default);
        }

        /// <summary>
        /// Start an async metric for reporting metric values for this frame in a subsequent frame.
        /// </summary>
        /// <param name="metricDefinition">The <see cref="MetricDefinition"/> of the metric</param>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="ShouldCaptureThisFrame"/> is false</exception>
        /// <returns>An <see cref="AsyncMetric"/> which should be used to report the metric values, potentially in a later frame</returns>
        public AsyncMetric ReportMetricAsync(MetricDefinition metricDefinition)
        {
            if (!ShouldCaptureThisFrame)
                throw new InvalidOperationException($"Sensor-based metrics may only be reported when SensorHandle.ShouldCaptureThisFrame is true");

            return DatasetCapture.SimulationState.CreateAsyncMetric(metricDefinition, this);
        }

        /// <summary>
        /// Dispose this SensorHandle.
        /// </summary>
        public void Dispose()
        {
            this.Enabled = false;
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
            return Id.Equals(other.Id);
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
    /// Handle to a metric whose values may be reported in a subsequent frame.
    /// </summary>
    public struct AsyncMetric
    {
        internal readonly int Id;
        readonly SimulationState m_SimulationState;

        internal AsyncMetric(MetricDefinition metricDefinition, int id, SimulationState simulationState)
        {
            this.Id = id;
            MetricDefinition = metricDefinition;
            m_SimulationState = simulationState;
        }

        /// <summary>
        /// The MetricDefinition associated with this AsyncMetric.
        /// </summary>
        public readonly MetricDefinition MetricDefinition;

        /// <summary>
        /// True if the simulation is still running.
        /// </summary>
        public bool IsValid => !IsNil && m_SimulationState.IsRunning;

        /// <summary>
        /// True if ReportValues has not been called yet.
        /// </summary>
        public bool IsPending => !IsNil && m_SimulationState.IsPending(ref this);

        /// <summary>
        /// Returns true if the AsyncMetric is its default value.
        /// </summary>
        public bool IsNil => m_SimulationState == null && Id == default;

        /// <summary>
        /// Report the values for this AsyncMetric. Calling this method will transition <see cref="IsPending"/> to false.
        /// ReportValues may only be called once per AsyncMetric.
        /// </summary>
        /// <param name="values">The values to report for the metric. These values will be converted to json.</param>
        /// <typeparam name="T">The type of the values</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if values is null</exception>
        public void ReportValues<T>(T[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            m_SimulationState.ReportAsyncMetricResult(this, values: values);
        }

        /// <summary>
        /// Report the values for this AsyncMetric. Calling this method will transition <see cref="IsPending"/> to false.
        /// ReportValues may only be called once per AsyncMetric.
        /// </summary>
        /// <param name="valuesJsonArray">A JSON array in string form.</param>
        /// <exception cref="ArgumentNullException">Thrown if values is null</exception>
        public void ReportValues(string valuesJsonArray)
        {
            if (valuesJsonArray == null)
                throw new ArgumentNullException(nameof(valuesJsonArray));

            m_SimulationState.ReportAsyncMetricResult(this, valuesJsonArray);
        }
    }

    /// <summary>
    /// A handle to an async annotation, used to report values for an annotation after the frame for the annotation has past.
    /// See <see cref="SensorHandle.ReportAnnotationAsync"/>
    /// </summary>
    public struct AsyncAnnotation
    {
        internal AsyncAnnotation(Annotation annotation, SimulationState simulationState)
        {
            Annotation = annotation;
            m_SimulationState = simulationState;
        }

        /// <summary>
        /// The annotation associated with this AsyncAnnotation. Can be used to report metrics on the annotation.
        /// </summary>
        public readonly Annotation Annotation;
        readonly SimulationState m_SimulationState;
        /// <summary>
        /// True if the annotation is nil (was created using default instantiation)
        /// </summary>
        internal bool IsNil => m_SimulationState == null && Annotation.IsNil;
        /// <summary>
        /// True if the annotation is generated by the currently running simulation.
        /// </summary>
        public bool IsValid => !IsNil && m_SimulationState.IsRunning;
        /// <summary>
        /// True if neither <see cref="ReportValues{T}"/> nor <see cref="ReportFile"/> have been called.
        /// </summary>
        public bool IsPending => !IsNil && m_SimulationState.IsPending(Annotation);

        /// <summary>
        /// Report a file-based data for this annotation.
        /// </summary>
        /// <param name="path">The path to the file containing the annotation data.</param>
        /// <exception cref="ArgumentNullException">Thrown if path is null</exception>
        public void ReportFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            m_SimulationState.ReportAsyncAnnotationResult<object>(this, path);
        }

        /// <summary>
        /// Report file-based and value-based data for this annotation.
        /// </summary>
        /// <param name="path">The path to the file containing the annotation data.</param>
        /// <param name="values">The annotation data.</param>
        /// <typeparam name="T">The type of the data.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if path or values is null</exception>
        public void ReportFileAndValues<T>(string path, IEnumerable<T> values)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (values == null)
                throw new ArgumentNullException(nameof(values));

            m_SimulationState.ReportAsyncAnnotationResult(this, path, values);
        }

        /// <summary>
        /// Report a value-based data for this annotation.
        /// </summary>
        /// <param name="values">The annotation data.</param>
        /// <typeparam name="T">The type of the data.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if values is null</exception>
        public void ReportValues<T>(IEnumerable<T> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            m_SimulationState.ReportAsyncAnnotationResult(this, values: values);
        }

        /// <summary>
        /// Report a value-based data for this annotation.
        /// </summary>
        /// <param name="values">The annotation data.</param>
        /// <typeparam name="T">The type of the data.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if values is null</exception>
        public void ReportValues<T>(NativeSlice<T> values) where T : struct
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            m_SimulationState.ReportAsyncAnnotationResult(this, values: values);
        }
    }

    /// <summary>
    /// A handle to an annotation. Can be used to report metrics on the annotation.
    /// </summary>
    public struct Annotation : IEquatable<Annotation>
    {
        /// <summary>
        /// The ID of the annotation which will be used in the json metadata.
        /// </summary>
        public readonly Guid Id;
        /// <summary>
        /// The step on which the annotation was reported.
        /// </summary>
        public readonly int Step;
        /// <summary>
        /// The SensorHandle on which the annotation was reported
        /// </summary>
        public readonly SensorHandle SensorHandle;

        internal Annotation(SensorHandle sensorHandle, int step)
        {
            Id = Guid.NewGuid();
            SensorHandle = sensorHandle;
            Step = step;
        }

        /// <summary>
        /// Returns true if the annotation is nil (created using default instantiation).
        /// </summary>
        public bool IsNil => Id == Guid.Empty;

        /// <summary>
        /// Reports a metric on this annotation. May only be called in the same frame as the annotation was reported.
        /// </summary>
        /// <param name="metricDefinition"></param>
        /// <param name="values"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentNullException">Thrown if values is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Annotation.SensorHandle"/> reports false for <see cref="UnityEngine.Perception.GroundTruth.SensorHandle.ShouldCaptureThisFrame"/>.</exception>
        public void ReportMetric<T>(MetricDefinition metricDefinition, [NotNull] T[] values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (!SensorHandle.ShouldCaptureThisFrame)
                throw new InvalidOperationException($"Sensor-based metrics may only be reported when SensorHandle.ShouldCaptureThisFrame is true");

            DatasetCapture.SimulationState.ReportMetric(metricDefinition, values, SensorHandle, this);
        }

        /// <summary>
        /// Reports a metric on this annotation. May only be called in the same frame as the annotation was reported.
        /// </summary>
        /// <param name="metricDefinition"></param>
        /// <param name="valuesJsonArray">A string-based JSON array to be placed in the "values" field of the metric</param>
        /// <exception cref="ArgumentNullException">Thrown if values is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Annotation.SensorHandle"/> reports false for
        /// <see cref="UnityEngine.Perception.GroundTruth.SensorHandle.ShouldCaptureThisFrame"/>.</exception>
        public void ReportMetric(MetricDefinition metricDefinition, [NotNull] string valuesJsonArray)
        {
            if (valuesJsonArray == null)
                throw new ArgumentNullException(nameof(valuesJsonArray));

            if (!SensorHandle.ShouldCaptureThisFrame)
                throw new InvalidOperationException($"Sensor-based metrics may only be reported when SensorHandle.ShouldCaptureThisFrame is true");

            DatasetCapture.SimulationState.ReportMetric(metricDefinition, new JRaw(valuesJsonArray), SensorHandle, this);
        }

        /// <summary>
        /// Report a metric whose values will be supplied in a later frame.
        /// </summary>
        /// <param name="metricDefinition">The type of the metric.</param>
        /// <returns>A handle to an AsyncMetric, which can be used to report values for this metric in future frames.</returns>
        public AsyncMetric ReportMetricAsync(MetricDefinition metricDefinition) => DatasetCapture.SimulationState.CreateAsyncMetric(metricDefinition, SensorHandle, this);

        /// <inheritdoc/>
        public bool Equals(Annotation other)
        {
            return Id.Equals(other.Id);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is Annotation other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    /// <summary>
    /// An ego, which is used to group multiple sensors under a single frame of reference.
    /// </summary>
    public struct EgoHandle : IEquatable<EgoHandle>
    {
        /// <summary>
        /// The ID for this ego. This ID will be used to refer to this ego in the json metadata.
        /// </summary>
        public readonly Guid Id;

        /// <summary>
        /// A human-readable description of this ego.
        /// </summary>
        public readonly string Description;

        internal EgoHandle(Guid id, string description)
        {
            this.Id = id;
            this.Description = description;
        }

        /// <inheritdoc/>
        public bool Equals(EgoHandle other)
        {
            return Id.Equals(other.Id);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is EgoHandle other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Compares two <see cref="EgoHandle"/> instances for equality.
        /// </summary>
        /// <param name="left">The first EgoHandle.</param>
        /// <param name="right">The second EgoHandle.</param>
        /// <returns>Returns true if the two EgoHandles refer to the same ego.</returns>
        public static bool operator==(EgoHandle left, EgoHandle right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="EgoHandle"/> instances for inequality.
        /// </summary>
        /// <param name="left">The first EgoHandle.</param>
        /// <param name="right">The second EgoHandle.</param>
        /// <returns>Returns true if the two EgoHandles refer to the same ego.</returns>
        public static bool operator!=(EgoHandle left, EgoHandle right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// A metric type, used to define a kind of metric. <see cref="DatasetCapture.RegisterMetricDefinition"/>.
    /// </summary>
    public struct MetricDefinition : IEquatable<MetricDefinition>
    {
        /// <summary>
        /// The ID of the metric
        /// </summary>
        public readonly Guid Id;

        internal MetricDefinition(Guid id)
        {
            Id = id;
        }

        /// <inheritdoc />
        public bool Equals(MetricDefinition other)
        {
            return Id.Equals(other.Id);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is MetricDefinition other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    /// <summary>
    /// A metric type, used to define a kind of annotation. <see cref="DatasetCapture.RegisterAnnotationDefinition"/>.
    /// </summary>
    public struct AnnotationDefinition : IEquatable<AnnotationDefinition>
    {
        /// <inheritdoc/>
        public bool Equals(AnnotationDefinition other)
        {
            return Id.Equals(other.Id);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is AnnotationDefinition other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// The ID of the annotation type. Used in the json metadata to associate anntations with the type.
        /// </summary>
        public readonly Guid Id;
        internal bool IsValid => DatasetCapture.IsValid(Id);

        internal AnnotationDefinition(Guid id)
        {
            Id = id;
        }
    }

    /// <summary>
    /// Container holding the poses of the ego and sensor. Also optionally contains the ego velocity and acceleration.
    /// </summary>
    public struct SensorSpatialData
    {
        /// <summary>
        /// The pose of the ego.
        /// </summary>
        public Pose EgoPose;
        /// <summary>
        /// The pose of the sensor relative to the ego.
        /// </summary>
        public Pose SensorPose;
        /// <summary>
        /// The velocity of the ego (optional).
        /// </summary>
        public Vector3? EgoVelocity;
        /// <summary>
        /// The acceleration of the ego (optional).
        /// </summary>
        public Vector3? EgoAcceleration;

        /// <summary>
        /// Create a new SensorSpatialData with the given values.
        /// </summary>
        /// <param name="egoPose">The pose of the ego.</param>
        /// <param name="sensorPose">The pose of the sensor relative to the ego.</param>
        /// <param name="egoVelocity">The velocity of the ego.</param>
        /// <param name="egoAcceleration">The acceleration of the ego.</param>
        public SensorSpatialData(Pose egoPose, Pose sensorPose, Vector3? egoVelocity, Vector3? egoAcceleration)
        {
            EgoPose = egoPose;
            SensorPose = sensorPose;
            EgoVelocity = egoVelocity;
            EgoAcceleration = egoAcceleration;
        }

        /// <summary>
        /// Create a SensorSpatialData from two <see cref="UnityEngine.GameObject"/>s, one representing the ego and the other representing the sensor.
        /// </summary>
        /// <param name="ego">The ego GameObject.</param>
        /// <param name="sensor">The sensor GameObject.</param>
        /// <returns>Returns a SensorSpatialData filled out with EgoPose and SensorPose based on the given objects.</returns>
        public static SensorSpatialData FromGameObjects(GameObject ego, GameObject sensor)
        {
            ego = ego == null ? sensor : ego;
            var egoRotation = ego.transform.rotation;
            var egoPosition = ego.transform.position;
            var sensorSpatialData = new SensorSpatialData()
            {
                EgoPose = new Pose(egoPosition, egoRotation),
                SensorPose = new Pose(sensor.transform.position - egoPosition, sensor.transform.rotation * Quaternion.Inverse(egoRotation))
            };
            return sensorSpatialData;
        }
    }
}
