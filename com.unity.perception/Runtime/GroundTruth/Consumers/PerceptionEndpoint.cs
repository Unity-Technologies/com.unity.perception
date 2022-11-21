using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.GroundTruth.Labelers;
using UnityEngine.Perception.Settings;

namespace UnityEngine.Perception.GroundTruth.Consumers
{
    /// <summary>
    /// Endpoint to write out generated data in the perception format.
    /// </summary>
    [Serializable]
    public class PerceptionEndpoint : IConsumerEndpoint, IFileSystemEndpoint
    {
        /// <summary>
        /// Current frame in the dataset generation
        /// </summary>
        public int currentFrame { get; private set; }

        string m_DatasetPath;
        Dictionary<string, SensorInfo> m_SensorMap = new Dictionary<string, SensorInfo>();
        internal Dictionary<string, AnnotationDefinition> registeredAnnotations = new Dictionary<string, AnnotationDefinition>();
        Dictionary<string, MetricDefinition> m_RegisteredMetrics = new Dictionary<string, MetricDefinition>();
        List<PerceptionCapture> m_CurrentCaptures = new List<PerceptionCapture>();
        internal Dictionary<string, Guid> idToGuidMap = new Dictionary<string, Guid>();
        Guid m_SequenceGuidStart = Guid.NewGuid();

        internal JsonSerializer Serializer { get; } = new JsonSerializer
        {
            ContractResolver = new PerceptionResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };

        /// <summary>
        /// The runtime resolved directory path where the dataset will be written to. This value
        /// should not be accessed directly. Please use <see cref="currentPath"/>
        /// </summary>
        protected string m_CurrentPathDoNotUseDirectly;

        /// <summary>
        /// The number of captures to write to a single captures file.
        /// </summary>
        public int capturesPerFile = 150;

        /// <summary>
        /// The number of metrics to write to a single metrics file.
        /// </summary>
        public int metricsPerFile = 150;

        /// <inheritdoc/>
        public string description => "Produces synthetic data in the perception format.";

        /// <summary>
        /// output version
        /// </summary>
        public static string version => "0.0.2";

        /// <inheritdoc/>
        public string defaultPath => PerceptionSettings.defaultOutputPath;

        /// <inheritdoc/>
        public virtual string basePath
        {
            get => PerceptionSettings.GetOutputBasePath();
            set => PerceptionSettings.SetOutputBasePath(value);
        }

        /// <summary>
        /// The current base path that generated data is being serialized to. This path is the base path plus a GUID. If the
        /// base is set to default or if the simulation is being run in USim, then this path will be the default simulation path.
        /// </summary>
        public virtual string currentPath
        {
            get
            {
                if (string.IsNullOrEmpty(m_CurrentPathDoNotUseDirectly))
                {
#if UNITY_SIMULATION_CORE_PRESENT
                    if (Unity.Simulation.Configuration.Instance.IsSimulationRunningInCloud())
                    {
                        m_CurrentPathDoNotUseDirectly = defaultPath;
                        return defaultPath;
                    }
#endif
                    var p = basePath;
                    if (!Directory.Exists(p))
                    {
                        m_CurrentPathDoNotUseDirectly = PathUtils.CombineUniversal(defaultPath, Guid.NewGuid().ToString());
                        Debug.LogError($"Tried to write perception output to an inaccessible path {p}. Using default path: {m_CurrentPathDoNotUseDirectly}");
                    }
                    else
                    {
                        m_CurrentPathDoNotUseDirectly = PathUtils.CombineUniversal(basePath, Guid.NewGuid().ToString());
                    }
                }
                return m_CurrentPathDoNotUseDirectly;
            }
        }

        /// <summary>
        /// The path that the dataset json files are written to.
        /// </summary>
        public string datasetPath
        {
            get
            {
                return m_DatasetPath ?? (m_DatasetPath = VerifyDirectoryWithGuidExists("Dataset"));
            }
        }

        internal string GetProductPath(RgbSensor sensor)
        {
            return GetRgbProductPath(sensor.id);
        }

        internal string GetRgbProductPath(string id)
        {
            idToGuidMap.TryGetValue(id, out var guid);
            var path = $"RGB{guid}";
            return VerifyDirectoryWithGuidExists(path, false);
        }

        internal string GetProductPath(SemanticSegmentationAnnotation annotation)
        {
            return GetSemanticSegmentationProductPath(annotation.id);
        }

        internal string GetSemanticSegmentationProductPath(string id)
        {
            idToGuidMap.TryGetValue(id, out var guid);
            var path = $"SemanticSegmentation{guid}";
            return VerifyDirectoryWithGuidExists(path, false);
        }

        internal string GetProductPath(InstanceSegmentationAnnotation annotation)
        {
            return GetInstanceSegmentationProductPath(annotation.id);
        }

        internal string GetInstanceSegmentationProductPath(string id)
        {
            idToGuidMap.TryGetValue(id, out var guid);
            var path = $"InstanceSegmentation{guid}";
            return VerifyDirectoryWithGuidExists(path, false);
        }

        // ReSharper disable NotAccessedField.Local
        [Serializable]
        struct SensorInfo
        {
            public string id;
            public string modality;
            public string description;
        }
        // ReSharper enable NotAccessedField.Local

        /// <summary>
        /// Creates a copy of PerceptionEndpoint.
        /// Copies capturesPerFile and metricsPerFile
        /// </summary>
        /// <returns>New object PerceptionEndpoint</returns>
        public object Clone()
        {
            var cloned = new PerceptionEndpoint
            {
                capturesPerFile = capturesPerFile,
                metricsPerFile = metricsPerFile
            };

            // not copying _CurrentPath on purpose. This needs to be set to null
            // for each cloned version of the endpoint so that a new dataset will
            // be created

            return cloned;
        }

        internal string VerifyDirectoryWithGuidExists(string directoryPrefix, bool appendGuid = true)
        {
            var path = currentPath;
            var dirs = Directory.GetDirectories(path);
            var found = string.Empty;

            foreach (var dir in dirs)
            {
                var dirName = new DirectoryInfo(dir).Name;
                if (dirName.StartsWith(directoryPrefix))
                {
                    found = PathUtils.EnsurePathsAreUniversal(dir);

                    break;
                }
            }

            if (found == string.Empty)
            {
                var dirName = appendGuid ? $"{directoryPrefix}{Guid.NewGuid().ToString()}" : directoryPrefix;
                found = PathUtils.CombineUniversal(path, dirName);
                Directory.CreateDirectory(found);
            }

            return found;
        }

        /// <inheritdoc/>
        public void AnnotationRegistered(AnnotationDefinition annotationDefinition)
        {
            if (registeredAnnotations.ContainsKey(annotationDefinition.id))
            {
                Debug.LogError("Tried to register an annotation twice");
                return;
            }

            registeredAnnotations[annotationDefinition.id] = annotationDefinition;
            idToGuidMap[annotationDefinition.id] = Guid.NewGuid();
        }

        /// <inheritdoc/>
        public void MetricRegistered(MetricDefinition metricDefinition)
        {
            if (m_RegisteredMetrics.ContainsKey(metricDefinition.id))
            {
                Debug.LogError("Tried to register a metric twice");
                return;
            }

            m_RegisteredMetrics[metricDefinition.id] = metricDefinition;
            idToGuidMap[metricDefinition.id] = Guid.NewGuid();
        }

        /// <inheritdoc/>
        public void SensorRegistered(SensorDefinition sensor)
        {
            if (m_SensorMap.ContainsKey(sensor.id))
            {
                Debug.LogError("Tried to register a sensor twice");
                return;
            }

            m_SensorMap[sensor.id] = new SensorInfo
            {
                id = sensor.id,
                modality = sensor.modality,
                description = sensor.description
            };

            idToGuidMap[sensor.id] = Guid.NewGuid();
        }

        /// <summary>
        /// Override this method to register a newly written file after it is written to disk.
        /// </summary>
        /// <param name="path"></param>
        public virtual void RegisterFile(string path)
        {
#if UNITY_SIMULATION_CORE_PRESENT
            Unity.Simulation.Manager.Instance.ConsumerFileProduced(path);
#endif
        }

        internal string RemoveDatasetPathPrefix(string path)
        {
            return path.Replace(currentPath + Path.AltDirectorySeparatorChar, string.Empty);
        }

        /// <inheritdoc/>
        public bool IsValid(out string errorMessage)
        {
            if (!Directory.Exists(basePath))
            {
                errorMessage = $"The dataset base path: {basePath} is inaccessible, generated data will not be written out properly";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <inheritdoc/>
        public void SimulationStarted(SimulationMetadata metadata)
        {
            Directory.CreateDirectory(currentPath);
        }

        /// <inheritdoc/>
        public void FrameGenerated(Frame frame)
        {
            currentFrame = frame.frame;
            var seqId = GenerateSequenceId(frame);

            var captureIdMap = new Dictionary<(int step, string sensorId), string>();
            foreach (var sensor in frame.sensors)
            {
                if (sensor is RgbSensor rgb)
                {
                    var path = PerceptionJsonFactory.WriteOutCapture(this, frame, rgb);
                    var sensorJToken = PerceptionJsonFactory.Convert(this, frame, rgb);

                    var annotations = new JArray();

                    foreach (var annotation in rgb.annotations)
                    {
                        registeredAnnotations.TryGetValue(annotation.annotationId, out var def);
                        var defId = def?.id ?? string.Empty;
                        var json = PerceptionJsonFactory.Convert(this, frame, annotation.id, defId, annotation);
                        if (json != null)
                            annotations.Add(json);
                    }

                    var id = Guid.NewGuid().ToString();
                    captureIdMap[(frame.step, rgb.id)] = id;
                    var capture = new PerceptionCapture
                    {
                        id = id,
                        sequence_id = seqId,
                        step = frame.step,
                        timestamp = frame.timestamp,
                        sensor = sensorJToken,
                        ego = JToken.FromObject(defaultEgo, Serializer),
                        filename = path,
                        format = "PNG",
                        annotations = annotations
                    };

                    m_CurrentCaptures.Add(capture);
                }
            }

            foreach (var metric in  frame.metrics)
            {
                AddMetricToReport(seqId, frame.step, captureIdMap, metric);
            }

            WriteCaptures();
        }

        string GenerateSequenceId(Frame frame)
        {
            //take the randomly generated sequenceGuidStart and increment by the sequence index to get a new unique id
            var hash = m_SequenceGuidStart.ToByteArray();
            var start = BitConverter.ToUInt32(hash, 0);
            start = start + (uint)frame.sequence;
            var startBytes = BitConverter.GetBytes(start);
            //reverse so that the beginning of the guid always changes
            Array.Reverse(startBytes);
            Array.Copy(startBytes, hash, startBytes.Length);
            var seqId = new Guid(hash).ToString();
            return seqId;
        }

        void WriteMetrics(bool flush = false)
        {
            if (flush || m_MetricsReady.Count > metricsPerFile)
            {
                WriteMetricsFile(m_MetricOutCount++, m_MetricsReady);
                m_MetricsReady.Clear();
            }
        }

        void WriteCaptures(bool flush = false)
        {
            if (flush || m_CurrentCaptures.Count >= capturesPerFile)
            {
                WriteCaptureFile(m_CurrentCaptureIndex++, m_CurrentCaptures);
                m_CurrentCaptures.Clear();
            }
        }

        /// <inheritdoc/>
        public void SimulationCompleted(SimulationMetadata metadata)
        {
            WriteSensorsFile();
            WriteAnnotationsDefinitionsFile();
            WriteMetricsDefinitionsFile();

            WriteCaptures(true);
            WriteMetrics(true);
        }

        /// <summary>
        /// Placeholder for crash resumption logic.
        /// </summary>
        /// <remarks>Not supported for Perception Endpoint</remarks>
        public (string, int) ResumeSimulationFromCrash(int maxFrameCount)
        {
            Debug.LogError("Crash resumption not supported for output from Perception Endpoint.");
            return (string.Empty, 0);
        }

        int m_CurrentCaptureIndex;

        internal string WriteOutImageFile(int frame, RgbSensor rgb)
        {
            var path = PathUtils.CombineUniversal(GetProductPath(rgb), $"rgb_{frame}.png");
            PathUtils.WriteAndReportImageFile(path, rgb.buffer);
            RegisterFile(path);
            return path;
        }

        void WriteJTokenToFile(string filePath, PerceptionJson json)
        {
            PathUtils.WriteAndReportJsonFile(filePath, JToken.FromObject(json, Serializer));
            RegisterFile(filePath);
        }

        void WriteJTokenToFile(string filePath, MetricsJson json)
        {
            PathUtils.WriteAndReportJsonFile(filePath, JToken.FromObject(json, Serializer));
            RegisterFile(filePath);
        }

        void WriteAnnotationsDefinitionsFile()
        {
            var defs = new JArray();

            foreach (var def in registeredAnnotations.Values)
            {
                defs.Add(PerceptionJsonFactory.Convert(this, def.id, def));
            }

            var top = new JObject
            {
                ["version"] = version,
                ["annotation_definitions"] = defs
            };
            var path = PathUtils.CombineUniversal(datasetPath, "annotation_definitions.json");
            PathUtils.WriteAndReportJsonFile(path, top);
            RegisterFile(path);
        }

        void WriteMetricsDefinitionsFile()
        {
            var defs = new JArray();

            foreach (var def in m_RegisteredMetrics.Values)
            {
                defs.Add(PerceptionJsonFactory.Convert(this, def.id, def));
            }

            var top = new JObject
            {
                ["version"] = version,
                ["metric_definitions"] = defs
            };
            var path = PathUtils.CombineUniversal(datasetPath, "metric_definitions.json");
            PathUtils.WriteAndReportJsonFile(path, top);
            RegisterFile(path);
        }

        void WriteSensorsFile()
        {
            var sub = new JArray();
            foreach (var sensor in m_SensorMap)
            {
                sub.Add(JToken.FromObject(sensor.Value, Serializer));
            }
            var top = new JObject
            {
                ["version"] = version,
                ["sensors"] = sub
            };
            var path = PathUtils.CombineUniversal(datasetPath, "sensors.json");
            PathUtils.WriteAndReportJsonFile(path, top);
            RegisterFile(path);
        }

        JToken ToJToken(string sequenceId, int step, Dictionary<(int step, string sensorId), string> captureIdMap,
            Metric metric)
        {
            string captureId = null;
            string annotationId = null;
            string defId = null;

            if (!string.IsNullOrEmpty(metric.sensorId))
            {
                var sensorId = m_SensorMap[metric.sensorId].id;
                captureIdMap.TryGetValue((step, sensorId), out captureId);
            }

            if (!string.IsNullOrEmpty(metric.annotationId))
            {
                annotationId = registeredAnnotations[metric.annotationId].id;
            }

            if (m_RegisteredMetrics.TryGetValue(metric.id, out var def))
            {
                defId = def.id;
            }

            return new JObject
            {
                ["capture_id"] =  captureId,
                ["annotation_id"] = annotationId,
                ["sequence_id"] = sequenceId,
                ["step"] = step,
                ["metric_definition"] = defId,
                ["values"] = JToken.FromObject(metric.GetValues<object>(), Serializer)
            };
        }

        void WriteMetricsFile(int index, IEnumerable<JToken> metrics)
        {
            var top = new MetricsJson
            {
                version = version,
                metrics = metrics
            };

            var path = PathUtils.CombineUniversal(datasetPath, $"metrics_{index:000}.json");
            WriteJTokenToFile(path, top);
        }

        int m_MetricOutCount;
        List<JToken> m_MetricsReady = new List<JToken>();
        void AddMetricToReport(string sequenceId, int step, Dictionary<(int step, string sensorId), string> captureIdMap,
            Metric metric)
        {
            m_MetricsReady.Add(ToJToken(sequenceId, step, captureIdMap, metric));
            WriteMetrics();
        }

        void WriteCaptureFile(int index, IEnumerable<PerceptionCapture> captures)
        {
            var top = new PerceptionJson
            {
                version = version,
                captures = captures
            };

            var path = PathUtils.CombineUniversal(datasetPath, $"captures_{index:000}.json");
            WriteJTokenToFile(path, top);
        }

        // ReSharper disable NotAccessedField.Local
        // ReSharper disable InconsistentNaming
        [Serializable]
        struct PerceptionJson
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public string version;
            public IEnumerable<PerceptionCapture> captures;
        }

        [Serializable]
        struct MetricsJson
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public string version;
            public IEnumerable<JToken> metrics;
        }

        [Serializable]
        struct PerceptionCapture
        {
            public string id;
            public string sequence_id;
            public int step;
            public float timestamp;
            public JToken sensor;
            public JToken ego;
            public string filename;
            public string format;
            public JArray annotations;
        }

        [Serializable]
        struct Ego
        {
            public string ego_id;
            public Vector3 translation;
            public Quaternion rotation;
            public Vector3? velocity;
            public Vector3? acceleration;
        }

        Ego defaultEgo => new Ego
        {
            ego_id = "ego",
            translation = Vector3.zero,
            rotation = Quaternion.identity,
            velocity = null,
            acceleration = null
        };

        static float[][] ToFloatArray(float3x3 inF3)
        {
            return new[]
            {
                new[] { inF3[0][0], inF3[0][1], inF3[0][2] },
                new[] { inF3[1][0], inF3[1][1], inF3[1][2] },
                new[] { inF3[2][0], inF3[2][1], inF3[2][2] }
            };
        }

        // ReSharper enable NotAccessedField.Local
        // ReSharper enable InconsistentNaming
    }

    class PerceptionResolver : DefaultContractResolver
    {
        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);
            if (objectType == typeof(Vector3) ||
                objectType == typeof(Vector2) ||
                objectType == typeof(Color) ||
                objectType == typeof(Quaternion))
            {
                contract.Converter = PerceptionConverter.Instance;
            }

            return contract;
        }
    }

    class PerceptionConverter : JsonConverter
    {
        public static PerceptionConverter Instance = new PerceptionConverter();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (value)
            {
                case int _:
                case uint _:
                case float _:
                case double _:
                case string _:
                    writer.WriteValue(value);
                    break;
                case Vector3 v3:
                {
                    writer.WriteStartArray();
                    writer.WriteValue(v3.x);
                    writer.WriteValue(v3.y);
                    writer.WriteValue(v3.z);
                    writer.WriteEndArray();
                    break;
                }
                case Vector2 v2:
                {
                    writer.WriteStartArray();
                    writer.WriteValue(v2.x);
                    writer.WriteValue(v2.y);
                    writer.WriteEndArray();
                    break;
                }
                case Color32 rgba:
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("r");
                    writer.WriteValue(rgba.r);
                    writer.WritePropertyName("g");
                    writer.WriteValue(rgba.g);
                    writer.WritePropertyName("b");
                    writer.WriteValue(rgba.b);
                    writer.WritePropertyName("a");
                    writer.WriteValue(rgba.a);
                    writer.WriteEndObject();
                    break;
                }
                case Color rgba:
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("r");
                    writer.WriteValue(rgba.r);
                    writer.WritePropertyName("g");
                    writer.WriteValue(rgba.g);
                    writer.WritePropertyName("b");
                    writer.WriteValue(rgba.b);
                    writer.WritePropertyName("a");
                    writer.WriteValue(rgba.a);
                    writer.WriteEndObject();
                    break;
                }
                case Quaternion quaternion:
                {
                    writer.WriteStartArray();
                    writer.WriteValue(quaternion.x);
                    writer.WriteValue(quaternion.y);
                    writer.WriteValue(quaternion.z);
                    writer.WriteValue(quaternion.w);
                    writer.WriteEndArray();
                    break;
                }
                case float3x3 f3x3:
                    writer.WriteStartArray();
                    writer.WriteStartArray();
                    writer.WriteValue(f3x3.c0[0]);
                    writer.WriteValue(f3x3.c0[1]);
                    writer.WriteValue(f3x3.c0[2]);
                    writer.WriteEndArray();
                    writer.WriteStartArray();
                    writer.WriteValue(f3x3.c1[0]);
                    writer.WriteValue(f3x3.c1[1]);
                    writer.WriteValue(f3x3.c1[2]);
                    writer.WriteEndArray();
                    writer.WriteStartArray();
                    writer.WriteValue(f3x3.c2[0]);
                    writer.WriteValue(f3x3.c2[1]);
                    writer.WriteValue(f3x3.c2[2]);
                    writer.WriteEndArray();
                    writer.WriteEndArray();
                    break;
            }
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return null;
        }

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(int)) return true;
            if (objectType == typeof(uint)) return true;
            if (objectType == typeof(double)) return true;
            if (objectType == typeof(float)) return true;
            if (objectType == typeof(string)) return true;
            if (objectType == typeof(Vector3)) return true;
            if (objectType == typeof(Vector2)) return true;
            if (objectType == typeof(Quaternion)) return true;
            if (objectType == typeof(float3x3)) return true;
            if (objectType == typeof(Color)) return true;
            return objectType == typeof(Color32);
        }
    }
}
