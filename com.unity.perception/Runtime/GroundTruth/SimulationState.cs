using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Simulation;
using UnityEngine;
using UnityEngine.Profiling;

// ReSharper disable NotAccessedField.Local
// ReSharper disable NonReadonlyMemberInGetHashCode

namespace UnityEngine.Perception.GroundTruth
{
    partial class SimulationState
    {
        HashSet<SensorHandle> m_ActiveSensors = new HashSet<SensorHandle>();
        Dictionary<SensorHandle, SensorData> m_Sensors = new Dictionary<SensorHandle, SensorData>();
        HashSet<EgoHandle> m_Egos = new HashSet<EgoHandle>();
        HashSet<Guid> m_Ids = new HashSet<Guid>();
        Guid m_SequenceId = Guid.NewGuid();

        //Always use the property SequenceTimeMs instead
        int m_FrameCountLastUpdatedSequenceTime;
        float m_SequenceTimeDoNotUse;
        float m_UnscaledSequenceTimeDoNotUse;

        int m_FrameCountLastStepIncremented = -1;
        int m_Step = -1;

        bool m_HasStarted;
        int m_CaptureFileIndex;
        List<AdditionalInfoTypeData> m_AdditionalInfoTypeData = new List<AdditionalInfoTypeData>();
        List<PendingCapture> m_PendingCaptures = new List<PendingCapture>(k_MinPendingCapturesBeforeWrite + 10);
        List<PendingMetric> m_PendingMetrics = new List<PendingMetric>(k_MinPendingMetricsBeforeWrite + 10);

        int m_MetricsFileIndex;
        int m_NextMetricId = 1;

        CustomSampler m_SerializeCapturesSampler = CustomSampler.Create("SerializeCaptures");
        CustomSampler m_SerializeCapturesAsyncSampler = CustomSampler.Create("SerializeCapturesAsync");
        CustomSampler m_JsonToStringSampler = CustomSampler.Create("JsonToString");
        CustomSampler m_WriteToDiskSampler = CustomSampler.Create("WriteJsonToDisk");
        CustomSampler m_SerializeMetricsSampler = CustomSampler.Create("SerializeMetrics");
        CustomSampler m_SerializeMetricsAsyncSampler = CustomSampler.Create("SerializeMetricsAsync");
        CustomSampler m_GetOrCreatePendingCaptureForThisFrameSampler = CustomSampler.Create("GetOrCreatePendingCaptureForThisFrame");
        float m_LastTimeScale;
        readonly string m_OutputDirectoryName;
        string m_OutputDirectoryPath;

        JsonSerializer m_AnnotationSerializer;
        public bool IsRunning { get; private set; }

        public string OutputDirectory
        {
            get
            {
                if (m_OutputDirectoryPath == null)
                    m_OutputDirectoryPath = Manager.Instance.GetDirectoryFor(m_OutputDirectoryName);

                return m_OutputDirectoryPath;
            }
        }

        //A sensor will be triggered if sequenceTime is within includeThreshold seconds of the next trigger
        const float k_IncludeInFrameThreshold = .01f;
        const int k_MinPendingCapturesBeforeWrite = 150;
        const int k_MinPendingMetricsBeforeWrite = 150;
        const int k_MaxDeltaTime = 10;

        public SimulationState(string outputDirectory)
        {
            m_AnnotationSerializer = JsonSerializer.CreateDefault();
            m_AnnotationSerializer.Converters.Add(new Vector3Converter());
            m_AnnotationSerializer.Converters.Add(new QuaternionConverter());
            m_OutputDirectoryName = outputDirectory;
            IsRunning = true;
        }

        /// <summary>
        /// A self-sufficient container for all information about a reported capture. Capture writing should not depend on any
        /// state outside of this container, as other state may have changed since the capture was reported.
        /// </summary>
        class PendingCapture
        {
            public Guid Id;
            public SensorHandle SensorHandle;
            public SensorData SensorData;
            public string Path;
            public SensorSpatialData SensorSpatialData;
            public int FrameCount;
            public int Step;
            public float Timestamp;
            public Guid SequenceId;
            public (string, object)[] AdditionalSensorValues;
            public List<(Annotation, AnnotationData)> Annotations = new List<(Annotation, AnnotationData)>();
            public bool CaptureReported;

            public PendingCapture(Guid id, SensorHandle sensorHandle, SensorData sensorData, Guid sequenceId, int frameCount, int step, float timestamp)
            {
                SensorHandle = sensorHandle;
                FrameCount = frameCount;
                Step = step;
                SequenceId = sequenceId;
                Timestamp = timestamp;
                Id = id;
                SensorData = sensorData;
            }
        }

        struct PendingMetric
        {
            public PendingMetric(MetricDefinition metricDefinition, int metricId, SensorHandle sensorHandle, Guid captureId, Annotation annotation, Guid sequenceId, int step, JToken values = null)
            {
                MetricDefinition = metricDefinition;
                MetricId = metricId;
                SensorHandle = sensorHandle;
                Annotation = annotation;
                SequenceId = sequenceId;
                Step = step;
                CaptureId = captureId;
                Values = values;
            }

            public readonly MetricDefinition MetricDefinition;
            public readonly int MetricId;
            public readonly SensorHandle SensorHandle;
            public readonly Guid CaptureId;
            public readonly Annotation Annotation;
            public readonly Guid SequenceId;
            public readonly int Step;
            public JToken Values;

            public bool IsAssigned => Values != null;
        }

        public struct SensorData
        {
            public string modality;
            public string description;
            public float period;
            public float firstCaptureTime;

            public float sequenceTimeNextCapture;
            public int lastCaptureFrameCount;
            public EgoHandle egoHandle;
        }

        struct AnnotationData
        {
            public readonly AnnotationDefinition AnnotationDefinition;
            public string Path;
            public JArray ValuesJson;
            public bool IsAssigned => Path != null || ValuesJson != null;

            public AnnotationData(AnnotationDefinition annotationDefinition, string path, JArray valuesJson)
                : this()
            {
                AnnotationDefinition = annotationDefinition;
                Path = path;
                ValuesJson = valuesJson;
            }
        }

        enum AdditionalInfoKind
        {
            Metric,
            Annotation
        }

        struct AdditionalInfoTypeData : IEquatable<AdditionalInfoTypeData>
        {
            public string name;
            public string description;
            public string format;
            public Guid id;
            public Array specValues;
            public AdditionalInfoKind additionalInfoKind;

            public override string ToString()
            {
                return $"{nameof(name)}: {name}, {nameof(description)}: {description}, {nameof(format)}: {format}, {nameof(id)}: {id}";
            }

            public bool Equals(AdditionalInfoTypeData other)
            {
                var areMembersEqual = additionalInfoKind == other.additionalInfoKind &&
                    string.Equals(name, other.name, StringComparison.InvariantCulture) &&
                    string.Equals(description, other.description, StringComparison.InvariantCulture) &&
                    string.Equals(format, other.format, StringComparison.InvariantCulture) &&
                    id.Equals(other.id);

                if (!areMembersEqual)
                    return false;

                if (specValues == other.specValues)
                    return true;
                if (specValues == null || other.specValues == null)
                    return false;
                if (specValues.Length != other.specValues.Length)
                    return false;

                for (var i = 0; i < specValues.Length; i++)
                {
                    if (!specValues.GetValue(i).Equals(other.specValues.GetValue(i)))
                        return false;
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                return obj is AdditionalInfoTypeData other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (name != null ? StringComparer.InvariantCulture.GetHashCode(name) : 0);
                    hashCode = (hashCode * 397) ^ (description != null ? StringComparer.InvariantCulture.GetHashCode(description) : 0);
                    hashCode = (hashCode * 397) ^ (format != null ? StringComparer.InvariantCulture.GetHashCode(format) : 0);
                    hashCode = (hashCode * 397) ^ id.GetHashCode();
                    return hashCode;
                }
            }
        }

        internal void ReportCapture(SensorHandle sensorHandle, string filename, SensorSpatialData sensorSpatialData, params(string, object)[] additionalSensorValues)
        {
            var sensorData = m_Sensors[sensorHandle];
            var pendingCapture = GetOrCreatePendingCaptureForThisFrame(sensorHandle, out _);

            if (pendingCapture.CaptureReported)
                throw new InvalidOperationException($"Capture for frame {Time.frameCount} already reported for sensor {this}");

            pendingCapture.CaptureReported = true;
            pendingCapture.Path = filename;
            pendingCapture.AdditionalSensorValues = additionalSensorValues;
            pendingCapture.SensorSpatialData = sensorSpatialData;

            sensorData.lastCaptureFrameCount = Time.frameCount;
            m_Sensors[sensorHandle] = sensorData;
        }

        static string GetFormatFromFilename(string filename)
        {
            var ext = Path.GetExtension(filename);
            if (ext == null)
                return null;

            if (ext.StartsWith("."))
                ext = ext.Substring(1);

            return ext.ToUpperInvariant();
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

        // ReSharper restore InconsistentNaming

        /// <summary>
        /// The simulation time that has elapsed since the beginning of the sequence.
        /// </summary>
        public float SequenceTime
        {
            get
            {
                //TODO: Can this be replaced with Time.time - sequenceTimeStart?
                if (!m_HasStarted)
                    return 0;

                EnsureSequenceTimingsUpdated();

                return m_SequenceTimeDoNotUse;
            }
        }

        /// <summary>
        /// The unscaled simulation time that has elapsed since the beginning of the sequence. This is the time that should be used for scheduling sensors
        /// </summary>
        public float UnscaledSequenceTime
        {
            get
            {
                //TODO: Can this be replaced with Time.time - sequenceTimeStart?
                if (!m_HasStarted)
                    return 0;

                EnsureSequenceTimingsUpdated();
                return m_UnscaledSequenceTimeDoNotUse;
            }
        }

        public string GetOutputDirectoryNoCreate() => Path.Combine(Configuration.Instance.GetStoragePath(), m_OutputDirectoryName);

        void EnsureSequenceTimingsUpdated()
        {
            if (!m_HasStarted)
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

        public void StartNewSequence()
        {
            ResetTimings();
            m_FrameCountLastStepIncremented = -1;
            m_Step = -1;
            foreach (var kvp in m_Sensors.ToArray())
            {
                var sensorData = kvp.Value;
                sensorData.sequenceTimeNextCapture = SequenceTimeOfNextCapture(sensorData);
                m_Sensors[kvp.Key] = sensorData;
            }

            m_SequenceId = Guid.NewGuid();
        }

        void ResetTimings()
        {
            m_FrameCountLastUpdatedSequenceTime = Time.frameCount;
            m_SequenceTimeDoNotUse = 0;
            m_UnscaledSequenceTimeDoNotUse = 0;
            m_LastTimeScale = Time.timeScale;
        }

        public void AddSensor(EgoHandle egoHandle, string modality, string description, float period, float firstCaptureTime, SensorHandle sensor)
        {
            var sensorData = new SensorData()
            {
                modality = modality,
                description = description,
                period = period,
                firstCaptureTime = firstCaptureTime,
                egoHandle = egoHandle,
                lastCaptureFrameCount = -1
            };
            sensorData.sequenceTimeNextCapture = SequenceTimeOfNextCapture(sensorData);
            m_ActiveSensors.Add(sensor);
            m_Sensors.Add(sensor, sensorData);
            m_Ids.Add(sensor.Id);
        }

        float SequenceTimeOfNextCapture(SensorData sensorData)
        {
            // If the first capture hasn't happened yet, sequenceTimeNextCapture field won't be valid
            if (sensorData.firstCaptureTime >= UnscaledSequenceTime)
                return sensorData.firstCaptureTime;
            return sensorData.sequenceTimeNextCapture;
        }

        public bool Contains(Guid id) => m_Ids.Contains(id);

        public void AddEgo(EgoHandle egoHandle)
        {
            CheckDatasetAllowed();
            m_Egos.Add(egoHandle);
            m_Ids.Add(egoHandle.Id);
        }


        public bool IsEnabled(SensorHandle sensorHandle) => m_ActiveSensors.Contains(sensorHandle);

        public void SetEnabled(SensorHandle sensorHandle, bool value)
        {
            if (!value)
                m_ActiveSensors.Remove(sensorHandle);
            else
                m_ActiveSensors.Add(sensorHandle);
        }

        void CheckDatasetAllowed()
        {
            if (!Application.isPlaying)
            {
                throw new InvalidOperationException("Dataset generation is only supported in play mode.");
            }
        }

        public void Update()
        {
            if (m_ActiveSensors.Count == 0)
                return;

            if (!m_HasStarted)
            {
                //simulation starts now
                m_FrameCountLastUpdatedSequenceTime = Time.frameCount;
                m_LastTimeScale = Time.timeScale;
                m_HasStarted = true;
            }

            //update the active sensors sequenceTimeNextCapture and lastCaptureFrameCount
            foreach (var activeSensor in m_ActiveSensors)
            {
                var sensorData = m_Sensors[activeSensor];
                if (!activeSensor.ShouldCaptureThisFrame)
                    continue;

                // TODO: AISV-845 This is an errant modification of this record that can lead to undefined behavior
                // Leaving as-is for now because too many components depend on this logic
                sensorData.sequenceTimeNextCapture += sensorData.period;
                Debug.Assert(sensorData.sequenceTimeNextCapture > UnscaledSequenceTime,
                    $"Next scheduled capture should be after {UnscaledSequenceTime} but is {sensorData.sequenceTimeNextCapture}");
                // sensorData.sequenceTimeNextCapture = SequenceTimeOfNextCapture(sensorData);
                sensorData.lastCaptureFrameCount = Time.frameCount;
                m_Sensors[activeSensor] = sensorData;
            }

            //find the deltatime required to land on the next active sensor that needs simulation
            float nextFrameDt = k_MaxDeltaTime;
            foreach (var activeSensor in m_ActiveSensors)
            {
                var sensorData = m_Sensors[activeSensor];
                var thisSensorNextFrameDt = sensorData.sequenceTimeNextCapture - UnscaledSequenceTime;

                Debug.Assert(thisSensorNextFrameDt > 0f, "Sensor was scheduled to run in the past but got skipped over.");
                if (thisSensorNextFrameDt > 0f && thisSensorNextFrameDt < nextFrameDt)
                    nextFrameDt = thisSensorNextFrameDt;
            }

            WritePendingCaptures();
            WritePendingMetrics();

            Time.captureDeltaTime = nextFrameDt;
        }

        public bool ShouldCaptureThisFrame(SensorHandle sensorHandle)
        {
            if (!m_Sensors.ContainsKey(sensorHandle))
                return false;

            var data = m_Sensors[sensorHandle];
            if (data.lastCaptureFrameCount == Time.frameCount)
                return true;

            return (data.sequenceTimeNextCapture - UnscaledSequenceTime) < k_IncludeInFrameThreshold;
        }

        public void End()
        {
            if (m_Ids.Count == 0)
                return;

            WritePendingCaptures(true, writeCapturesFromThisFrame: true);
            if (m_PendingCaptures.Count > 0)
                Debug.LogError($"Simulation ended with pending annotations: {string.Join(", ", m_PendingCaptures.Select(c => $"id:{c.SensorHandle.Id} frame:{c.FrameCount}"))}");

            WritePendingMetrics(true);
            if (m_PendingMetrics.Count > 0)
                Debug.LogError($"Simulation ended with pending metrics: {string.Join(", ", m_PendingMetrics.Select(c => $"id:{c.MetricId} step:{c.Step}"))}");

            WriteReferences();
            Time.captureDeltaTime = 0;
            IsRunning = false;
        }

        public AnnotationDefinition RegisterAnnotationDefinition<TSpec>(string name, TSpec[] specValues, string description, string format, Guid id)
        {
            if (id == Guid.Empty)
                id = Guid.NewGuid();

            RegisterAdditionalInfoType(name, specValues, description, format, id, AdditionalInfoKind.Annotation);

            return new AnnotationDefinition(id);
        }

        public MetricDefinition RegisterMetricDefinition<TSpec>(string name, TSpec[] specValues, string description, Guid id)
        {
            if (id == Guid.Empty)
                id = Guid.NewGuid();

            RegisterAdditionalInfoType(name, specValues, description, null, id, AdditionalInfoKind.Metric);

            return new MetricDefinition(id);
        }

        void RegisterAdditionalInfoType<TSpec>(string name, TSpec[] specValues, string description, string format, Guid id, AdditionalInfoKind additionalInfoKind)
        {
            CheckDatasetAllowed();
            var annotationDefinitionInfo = new AdditionalInfoTypeData()
            {
                additionalInfoKind = additionalInfoKind,
                name = name,
                description = description,
                format = format,
                id = id,
                specValues = specValues
            };

            if (!m_Ids.Add(id))
            {
                foreach (var existingAnnotationDefinition in m_AdditionalInfoTypeData)
                {
                    if (existingAnnotationDefinition.id == id)
                    {
                        if (existingAnnotationDefinition.Equals(annotationDefinitionInfo))
                        {
                            return;
                        }

                        throw new ArgumentException($"{id} has already been registered to an AnnotationDefinition or MetricDefinition with different information.\nExisting: {existingAnnotationDefinition}");
                    }
                }

                throw new ArgumentException($"Id {id} is already in use. Ids must be unique.");
            }

            m_AdditionalInfoTypeData.Add(annotationDefinitionInfo);
        }

        public Annotation ReportAnnotationFile(AnnotationDefinition annotationDefinition, SensorHandle sensorHandle, string filename)
        {
            var annotation = new Annotation(sensorHandle, AcquireStep());
            var pendingCapture = GetOrCreatePendingCaptureForThisFrame(sensorHandle);
            pendingCapture.Annotations.Add((annotation, new AnnotationData(annotationDefinition, filename, null)));
            return annotation;
        }

        public Annotation ReportAnnotationValues<T>(AnnotationDefinition annotationDefinition, SensorHandle sensorHandle, T[] values)
        {
            var annotation = new Annotation(sensorHandle, AcquireStep());
            var pendingCapture = GetOrCreatePendingCaptureForThisFrame(sensorHandle);
            var valuesJson = new JArray();
            foreach (var value in values)
            {
                valuesJson.Add(DatasetJsonUtility.ToJToken(value));
            }
            pendingCapture.Annotations.Add((annotation, new AnnotationData(annotationDefinition, null, valuesJson)));
            return annotation;
        }

        PendingCapture GetOrCreatePendingCaptureForThisFrame(SensorHandle sensorHandle)
        {
            return GetOrCreatePendingCaptureForThisFrame(sensorHandle, out var _);
        }

        PendingCapture GetOrCreatePendingCaptureForThisFrame(SensorHandle sensorHandle, out bool created)
        {
            created = false;
            m_GetOrCreatePendingCaptureForThisFrameSampler.Begin();
            EnsureStepIncremented();

            //Following is this converted to code: m_PendingCaptures.FirstOrDefault(c => c.SensorHandle == sensorHandle && c.FrameCount == Time.frameCount);
            //We also start at the end, since the pending capture list can get long as we await writing to disk
            PendingCapture pendingCapture = null;
            for (var i = m_PendingCaptures.Count - 1; i >= 0; i--)
            {
                var c = m_PendingCaptures[i];
                if (c.SensorHandle == sensorHandle && c.FrameCount == Time.frameCount)
                {
                    pendingCapture = c;
                    break;
                }
            }

            if (pendingCapture == null)
            {
                created = true;
                pendingCapture = new PendingCapture(Guid.NewGuid(), sensorHandle, m_Sensors[sensorHandle], m_SequenceId, Time.frameCount, AcquireStep(), SequenceTime);
                m_PendingCaptures.Add(pendingCapture);
            }

            m_GetOrCreatePendingCaptureForThisFrameSampler.End();
            return pendingCapture;
        }

        public AsyncAnnotation ReportAnnotationAsync(AnnotationDefinition annotationDefinition, SensorHandle sensorHandle)
        {
            return new AsyncAnnotation(ReportAnnotationFile(annotationDefinition, sensorHandle, null), this);
        }

        public void ReportAsyncAnnotationResult<T>(AsyncAnnotation asyncAnnotation, string filename = null, NativeSlice<T> values = default) where T : struct
        {
            var jArray = new JArray();
            foreach (var value in values)
                jArray.Add(new JRaw(DatasetJsonUtility.ToJToken(value)));

            ReportAsyncAnnotationResult<T>(asyncAnnotation, filename, jArray);
        }

        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public class QuaternionConverter : JsonConverter<Quaternion>
        {
            public override void WriteJson(JsonWriter writer, Quaternion quaternion, JsonSerializer serializer)
            {
                writer.WriteStartArray();
                writer.WriteValue(quaternion.x);
                writer.WriteValue(quaternion.y);
                writer.WriteValue(quaternion.z);
                writer.WriteValue(quaternion.w);
                writer.WriteEndArray();
            }

            public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var q = Quaternion.identity;
                reader.Read(); // open [ token
                q.x = (float)reader.ReadAsDecimal();
                q.y = (float)reader.ReadAsDecimal();
                q.z = (float)reader.ReadAsDecimal();
                q.w = (float)reader.ReadAsDecimal();
                reader.Read(); // close ] token
                return q;
            }
        }
        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public class Vector3Converter : JsonConverter<Vector3>
        {
            public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
            {
                writer.WriteStartArray();
                writer.WriteValue(value.x);
                writer.WriteValue(value.y);
                writer.WriteValue(value.z);
                writer.WriteEndArray();
            }

            public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var outVector = new Vector3();
                reader.Read(); // open array token
                outVector.x = (float)reader.ReadAsDecimal();
                outVector.y = (float)reader.ReadAsDecimal();
                outVector.z = (float)reader.ReadAsDecimal();
                reader.Read(); // close array token
                return outVector;
            }
        }
        public void ReportAsyncAnnotationResult<T>(AsyncAnnotation asyncAnnotation, string filename = null, IEnumerable<T> values = null)
        {
            JArray jArray = null;

            if (values != null)
            {
                jArray = new JArray();
                foreach (var value in values)
                {
                    if (value != null)
                        jArray.Add(new JRaw(DatasetJsonUtility.ToJToken(value)));
                }
            }

            ReportAsyncAnnotationResult<T>(asyncAnnotation, filename, jArray);
        }

        void ReportAsyncAnnotationResult<T>(AsyncAnnotation asyncAnnotation, string filename, JArray jArray)
        {
            if (!asyncAnnotation.IsPending)
                throw new InvalidOperationException("AsyncAnnotation has already been reported and cannot be reported again.");

            PendingCapture pendingCapture = null;
            int annotationIndex = -1;
            foreach (var c in m_PendingCaptures)
            {
                if (c.Step == asyncAnnotation.Annotation.Step && c.SensorHandle == asyncAnnotation.Annotation.SensorHandle)
                {
                    pendingCapture = c;
                    annotationIndex = pendingCapture.Annotations.FindIndex(a => a.Item1.Equals(asyncAnnotation.Annotation));
                    if (annotationIndex != -1)
                        break;
                }
            }

            Debug.Assert(pendingCapture != null && annotationIndex != -1);

            var annotationTuple = pendingCapture.Annotations[annotationIndex];
            var annotationData = annotationTuple.Item2;

            annotationData.Path = filename;
            annotationData.ValuesJson = jArray;

            annotationTuple.Item2 = annotationData;
            pendingCapture.Annotations[annotationIndex] = annotationTuple;
        }

        public bool IsPending(Annotation annotation)
        {
            foreach (var c in m_PendingCaptures)
            {
                foreach (var a in c.Annotations)
                {
                    if (a.Item1.Equals(annotation))
                        return !a.Item2.IsAssigned;
                }
            }

            return false;
        }

        public bool IsPending(ref AsyncMetric asyncMetric)
        {
            foreach (var m in m_PendingMetrics)
            {
                if (m.MetricId == asyncMetric.Id)
                    return !m.IsAssigned;
            }

            return false;
        }

        public void ReportAsyncMetricResult<T>(AsyncMetric asyncMetric, T[] values)
        {
            var pendingMetricValues = JArrayFromArray(values);
            ReportAsyncMetricResult(asyncMetric, pendingMetricValues);
        }

        public void ReportAsyncMetricResult(AsyncMetric asyncMetric, string valuesJsonArray)
        {
            ReportAsyncMetricResult(asyncMetric, new JRaw(valuesJsonArray));
        }

        void ReportAsyncMetricResult(AsyncMetric asyncMetric, JToken values)
        {
            var metricIndex = -1;
            for (var i = 0; i < m_PendingMetrics.Count; i++)
            {
                if (m_PendingMetrics[i].MetricId == asyncMetric.Id)
                {
                    metricIndex = i;
                    break;
                }
            }

            if (metricIndex == -1)
                throw new InvalidOperationException("asyncMetric is invalid or has already been reported");

            var pendingMetric = m_PendingMetrics[metricIndex];
            if (pendingMetric.IsAssigned)
                throw new InvalidOperationException("asyncMetric already been reported. ReportAsyncMetricResult may only be called once per AsyncMetric");

            pendingMetric.Values = values;
            m_PendingMetrics[metricIndex] = pendingMetric;
        }

        static JArray JArrayFromArray<T>(T[] values)
        {
            var jArray = new JArray();

            foreach (var value in values)
                jArray.Add(DatasetJsonUtility.ToJToken(value));

            return jArray;
        }

        public AsyncMetric CreateAsyncMetric(MetricDefinition metricDefinition, SensorHandle sensorHandle = default, Annotation annotation = default)
        {
            EnsureStepIncremented();
            var id = m_NextMetricId++;
            var captureId = Guid.Empty;
            if (sensorHandle != default)
            {
                var capture = GetOrCreatePendingCaptureForThisFrame(sensorHandle);
                captureId = capture.Id;
            }

            m_PendingMetrics.Add(new PendingMetric(metricDefinition, id, sensorHandle, captureId, annotation, m_SequenceId, AcquireStep()));
            return new AsyncMetric(metricDefinition, id, this);
        }

        public void ReportMetric<T>(MetricDefinition metricDefinition, T[] values, SensorHandle sensorHandle, Annotation annotation)
        {
            var jArray = JArrayFromArray(values);
            ReportMetric(metricDefinition, jArray, sensorHandle, annotation);
        }

        public void ReportMetric(MetricDefinition metricDefinition, JToken values, SensorHandle sensorHandle, Annotation annotation)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            EnsureStepIncremented();
            var captureId = sensorHandle.IsNil ? Guid.Empty : GetOrCreatePendingCaptureForThisFrame(sensorHandle).Id;
            m_PendingMetrics.Add(new PendingMetric(metricDefinition, m_NextMetricId++, sensorHandle, captureId, annotation, m_SequenceId, AcquireStep(), values));
        }
    }
}
