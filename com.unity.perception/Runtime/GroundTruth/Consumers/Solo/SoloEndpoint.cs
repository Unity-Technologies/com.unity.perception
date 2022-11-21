using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.Settings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Consumers
{
    /// <summary>
    /// Endpoint to write out generated data in solo format
    /// </summary>
    [Serializable]
    [MovedFrom("UnityEngine.Perception.GroundTruth.Internal.Consumers")]
    public class SoloEndpoint : IConsumerEndpoint, IFileSystemEndpoint
    {
        const int k_UniqueFileLimit = 10000;

        bool m_DataGenerated;
        JToken m_FrameToken;
        Stack<JToken> m_Tokens = new Stack<JToken>();
        SoloDefinitionHolder m_RegisteredAnnotations = new SoloDefinitionHolder("annotationDefinitions");
        SoloDefinitionHolder m_RegisteredMetrics = new SoloDefinitionHolder("metricDefinitions");
        SoloDefinitionHolder m_RegisteredSensors = new SoloDefinitionHolder("sensorDefinitions");

        /// <summary>
        /// The runtime resolved directory path where the dataset will be written to.
        /// </summary>
        protected string m_CurrentPath;

        /// <summary>
        /// The name of the dataset on disk. If the name is already used, an integer value will
        /// be added to it to make it unique
        /// </summary>
        public string soloDatasetName = "solo";

        /// <summary>
        /// THe description of the endpoint
        /// </summary>
        public string description => "The Synthetic Optimized Labeled Objects (Solo) format used to capture generated data.";

        /// <summary>
        /// The base path the endpoint will write to
        /// </summary>
        public string basePath
        {
            get => PerceptionSettings.GetOutputBasePath();
            set => PerceptionSettings.SetOutputBasePath(value);
        }

        public string defaultPath => PerceptionSettings.defaultOutputPath;

        /// <summary>
        /// The runtime directory that the dataset will be written to.
        /// This directory may be different from the <see cref="basePath"/> in cases where the <see cref="basePath"/>
        /// already existed at the beginning of dataset generation.
        /// </summary>
        public virtual string currentPath
        {
            get
            {
                if (string.IsNullOrEmpty(m_CurrentPath))
                {
#if UNITY_SIMULATION_CORE_PRESENT
                    if (Unity.Simulation.Configuration.Instance.IsSimulationRunningInCloud())
                        return defaultPath;
#endif
                    (_, soloDatasetName) = PathUtils.CheckAndFixFileName(soloDatasetName);

                    var activeBasePath = basePath;

                    if (!Directory.Exists(activeBasePath))
                    {
                        activeBasePath = defaultPath;
                        Debug.LogError($"Tried to write solo output to an inaccessible path {basePath}. Using default path: {defaultPath}");
                    }

                    var(_, index) = GetLastGeneratedFolder(activeBasePath, soloDatasetName, k_UniqueFileLimit);

                    m_CurrentPath = BuildFolderPath(activeBasePath, soloDatasetName, index + 1);

                    if (index > k_UniqueFileLimit)
                    {
                        m_CurrentPath = PathUtils.CombineUniversal(activeBasePath, $"{soloDatasetName}_{Guid.NewGuid().ToString()}");
                    }
                }
                return m_CurrentPath;
            }
        }

        /// <summary>
        /// Provided last generated folder, if exists
        /// </summary>
        /// <param name="baseDirectoryPath">path to the directory where result folder will be stored</param>
        /// <param name="folderName">base name for the output folder</param>
        /// <param name="uniqueFileLimit">max amount of unique folder numbers, after - GUID based names</param>
        /// <param name="folderIndex">start index to search for resuming the folder</param>
        /// <returns> string - path to the last folder, if any. int - folder index</returns>
        (string, int) GetLastGeneratedFolder(string baseDirectoryPath, string folderName, int uniqueFileLimit, int folderIndex = 0)
        {
            var folderPath = BuildFolderPath(baseDirectoryPath, folderName, folderIndex);

            if (!Directory.Exists(folderPath))
            {
                return (string.Empty, folderIndex - 1);
            }

            while (Directory.Exists(BuildFolderPath(baseDirectoryPath, folderName, folderIndex + 1)) && folderIndex < uniqueFileLimit)
            {
                folderIndex++;
            }

            folderPath = BuildFolderPath(baseDirectoryPath, folderName, folderIndex);

            return (folderPath, folderIndex);
        }

        /// <summary>
        /// Generates folder name based on rules
        /// </summary>
        /// <param name="baseDirectoryPath">path to the directory where result folder will be stored</param>
        /// <param name="folderName">base name for the output folder</param>
        /// <param name="index">folder index in the directory</param>
        /// <returns></returns>
        string BuildFolderPath(string baseDirectoryPath, string folderName, int index)
        {
            return PathUtils.CombineUniversal(baseDirectoryPath, index == 0 ? $"{folderName}" : $"{folderName}_{index}");
        }

        /// <summary>
        /// The directory storing the dataset's metadata files.
        /// </summary>
        public virtual string metadataPath
        {
            get
            {
                var path = currentPath;
#if UNITY_SIMULATION_CORE_PRESENT
                // There is an issue with Unity Simulation right now that we can't write
                // files in the base directory, or files more than one directory deep,
                // so right now, the only workaround is to create a metadata directory to
                // store the metadata.
                if (Unity.Simulation.Configuration.Instance.IsSimulationRunningInCloud())
                {
                    path = PathUtils.CombineUniversal(currentPath, "metadata");
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
#endif
                return path;
            }
        }

        /// <inheritdoc/>
        public object Clone()
        {
            var newOne = new SoloEndpoint
            {
                soloDatasetName = soloDatasetName
            };

            // not copying _CurrentPath on purpose. This needs to be set to null
            // for each cloned version of the endpoint so that a new dataset will
            // be created

            return newOne;
        }

        /// <summary>
        /// Checks to see if an endpoint is configured properly. If an endpoint is invalid the endpoint
        /// will not be able to properly produce generated data.
        /// </summary>
        /// <param name="errorMessage">If validation fails, this will be updated with an error message</param>
        /// <returns>True if validation is successful</returns>
        public bool IsValid(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (PathUtils.DoesFilenameIncludeIllegalCharacters(soloDatasetName))
            {
                errorMessage = $"The solo dataset name: {soloDatasetName} is empty or it contains illegal characters.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called when the simulation begins. Provides simulation wide metadata to
        /// the consumer.
        /// </summary>
        /// <param name="metadata">Metadata describing the active simulation</param>
        public void SimulationStarted(SimulationMetadata metadata)
        {
            m_DataGenerated = false;
        }

        internal string GetSequenceDirectoryPath(Frame frame)
        {
            var path = GetSequenceFolderName(frame.sequence);

            // verify that a directory already exists for a sequence,
            // if not, create it.
            path = PathUtils.CombineUniversal(currentPath, path);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        string GetSequenceFolderName(int sequenceIndex) => $"sequence.{sequenceIndex}";

        /// <summary>
        /// Called at the end of each frame. Contains all of the generated data for the
        /// frame. This method is called after the frame has entirely finished processing.
        /// </summary>
        /// <param name="frame">The frame data.</param>
        public void FrameGenerated(Frame frame)
        {
            // lazy initialization to prevent writing an output if no data was generated
            if (!m_DataGenerated)
            {
                var current = currentPath;
                if (!Directory.Exists(current))
                    Directory.CreateDirectory(current);

                m_DataGenerated = true;
            }

            if (m_FrameToken == null)
            {
                m_FrameToken = new JObject();
                m_Tokens.Push(m_FrameToken);
            }

            var msg = new SoloFrameMessageBuilder(this, frame);

            frame.ToMessage(msg);

            // write out current
            var path = GetSequenceDirectoryPath(frame);
            path = PathUtils.CombineUniversal(path, GetFrameStepFilename(frame.step));

            PathUtils.WriteAndReportJsonFile(path, msg.ToJson());
            RegisterFile(path);
        }

        string GetFrameStepFilename(int stepIndex) => $"step{stepIndex}.frame_data.json";

        /// <summary>
        /// Called when an annotation is registered with the perception engine.
        /// </summary>
        /// <param name="annotationDefinition">The registered annotation definition</param>
        public void AnnotationRegistered(AnnotationDefinition annotationDefinition)
        {
            m_RegisteredAnnotations.AddDefinition(annotationDefinition);
        }

        /// <summary>
        /// Called when a metric is registered with the perception engine.
        /// </summary>
        /// <param name="metricDefinition">The registered metric definition</param>
        public void MetricRegistered(MetricDefinition metricDefinition)
        {
            m_RegisteredMetrics.AddDefinition(metricDefinition);
        }

        /// <summary>
        /// Called when a sensor is registered with the perception engine.
        /// </summary>
        /// <param name="sensor">The registered sensor definition</param>
        public void SensorRegistered(SensorDefinition sensor)
        {
            m_RegisteredSensors.AddDefinition(sensor);
        }

        const string k_MetadataFilename = "metadata.json";
        const string k_AnnotationDefinitionsFilename = "annotation_definitions.json";
        const string k_MetricDefinitionsFilename = "metric_definitions.json";
        const string k_SensorDefinitionsFilename = "sensor_definitions.json";

        /// <summary>
        /// Called at the end of the simulation. Contains metadata describing the entire
        /// simulation process.
        /// </summary>
        /// <param name="metadata">Metadata describing the entire simulation process</param>
        public void SimulationCompleted(SimulationMetadata metadata)
        {
            // Only write out metadata if we actually generated any data
            if (m_DataGenerated)
            {
                WriteOutJsonFile(metadataPath, k_MetadataFilename, metadata);
                WriteOutJsonFile(metadataPath, k_AnnotationDefinitionsFilename, m_RegisteredAnnotations);
                WriteOutJsonFile(metadataPath, k_MetricDefinitionsFilename, m_RegisteredMetrics);
                WriteOutJsonFile(metadataPath, k_SensorDefinitionsFilename, m_RegisteredSensors);
            }
        }

        bool CheckFolderForCompletedSimulation()
        {
            var metadataExists = File.Exists(PathUtils.CombineUniversal(metadataPath, k_MetadataFilename));
            var annotationMetadataExists = File.Exists(PathUtils.CombineUniversal(metadataPath, k_AnnotationDefinitionsFilename));
            var metricMetadataExists = File.Exists(PathUtils.CombineUniversal(metadataPath, k_MetricDefinitionsFilename));
            var sensorMetadataExists = File.Exists(PathUtils.CombineUniversal(metadataPath, k_SensorDefinitionsFilename));

            return metadataExists && annotationMetadataExists && metricMetadataExists && sensorMetadataExists;
        }

        /// <summary>
        /// Call this method for endpoint to restore crash point and resume simulation to the same folder
        /// </summary>
        /// <returns>string - path to the folder. int - last generated frame</returns>
        /// <param name="maxFrameCount">maxFrameCount describes required amount of frames to be generated</param>
        public (string, int) ResumeSimulationFromCrash(int maxFrameCount)
        {
            var activeBasePath = basePath;

            // try to get working folder
            var(outputCoreFolderPath, index) = GetLastGeneratedFolder(activeBasePath, soloDatasetName, k_UniqueFileLimit);
            if (string.IsNullOrEmpty(outputCoreFolderPath) || index < 0)
            {
                return ("there is nothing to restore", 0);
            }
            m_CurrentPath = outputCoreFolderPath;

            //previous simulation was successfully completed
            if (CheckFolderForCompletedSimulation())
            {
                // looks like previous simulation is completed, we should start the new one
                outputCoreFolderPath = BuildFolderPath(activeBasePath, soloDatasetName, index + 1);
                m_CurrentPath = outputCoreFolderPath;

                return ("done", 0);
            }

            // try to get last frame index
            for (var i = 0; i < maxFrameCount; i++)
            {
                // check if sequence folder exists
                var sequenceFolder  = PathUtils.CombineUniversal(outputCoreFolderPath, GetSequenceFolderName(i));
                if (!Directory.Exists(sequenceFolder))
                {
                    return ("there is no sequence folder", i - 1);
                }

                // check that there is step data
                var frameConfigFileStep0 = PathUtils.CombineUniversal(sequenceFolder, GetFrameStepFilename(0));
                if (!File.Exists(frameConfigFileStep0))
                {
                    return ("there is no frame step data for step 0", i - 1);
                }

                // try to iterate 10 steps
                for (var frameStep = 0; frameStep < 10; frameStep++)
                {
                    var frameConfigFile = PathUtils.CombineUniversal(sequenceFolder, GetFrameStepFilename(frameStep));
                    if (!File.Exists(frameConfigFile))
                    {
                        break;
                    }

                    string configJson;
                    try
                    {
                        configJson = File.ReadAllText(frameConfigFile);
                    }
                    catch (SystemException e)
                    {
                        Debug.LogError($"Can't read file {frameConfigFile} with exception: {e}");
                        break;
                    }

                    var frameIsValidAndCompleted = SoloFrameMessageBuilder.ValidateAllFilesAreWrittenForFrame(sequenceFolder, configJson);

                    if (!frameIsValidAndCompleted)
                    {
                        return ($"step {frameStep} for {sequenceFolder} was not valid", i - 1);
                    }
                }
            }

            return ("there is nothing to restore", 0);
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

        void WriteOutJsonFile(string path, string filename, IMessageProducer producer)
        {
            var msg = new SoloMessageBuilder(this);
            producer.ToMessage(msg);

            var filePath = PathUtils.CombineUniversal(path, filename);
            PathUtils.WriteAndReportJsonFile(filePath, msg.ToJson());
            RegisterFile(filePath);
        }
    }
}
