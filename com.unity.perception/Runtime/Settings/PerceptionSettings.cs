#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.IO;
using UnityEditor.Perception.GroundTruth;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.Settings
{
    [Serializable]
    public class PerceptionSettings : MonoBehaviour
    {
        static string s_GameObjectName = "_PerceptionSettings";
        static PerceptionSettings s_Instance;
        static bool s_HideInHierarchy = true;
#if !UNITY_EDITOR
        (bool, string) GetPathFromCommandLine()
        {
            (bool, string) errorResult = (false, string.Empty);

            var args = Environment.GetCommandLineArgs();
            var index = Array.FindIndex(args, x => x.Equals("--output-path"));

            if (index == -1)
            {
                Debug.Log($"--output-path was not provided on command line, using default location: {defaultOutputPath}");
                return errorResult;
            }

            if (index == args.Length - 1)
            {
                var msg = $"--output-path was present on command line, but path was not defined, using default location: {defaultOutputPath}";
                Debug.LogError(msg);
                return errorResult;
            }

            var path = args[index + 1];
            if (Directory.Exists(path))
            {
                return (true, path);
            }

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                var msg = $"An invalid path ({path}) was requested with --output-path, using default location: {defaultOutputPath}";
                Debug.LogError(msg + e);
                return errorResult;
            }

            return (true, path);
        }

        public string GetOutputBasePath()
        {
#if UNITY_SIMULATION_CORE_PRESENT
            if (Unity.Simulation.Configuration.Instance.IsSimulationRunningInCloud())
                return defaultOutputPath;
#endif
            var (isOk, path) = GetPathFromCommandLine();

            return isOk ? path : defaultOutputPath;
        }
#else
        public string GetOutputBasePath()
        {
#if UNITY_SIMULATION_CORE_PRESENT
            if (Unity.Simulation.Configuration.Instance.IsSimulationRunningInCloud())
                return defaultOutputPath;
#endif
            return userPreferences.TryGetValue($"{endpoint.GetType().FullName}.output_path", out string path) ? path : defaultOutputPath;
        }
#endif
        /// <summary>
        /// Sets the output path for the active endpoint type. This will set the path for the next simulation, it will
        /// not affect a simulation that is currently executing. In order for this to take effect the caller should call
        /// <see cref="DatasetCapture.ResetSimulation"/>
        /// </summary>
        /// <param name="path">The output path</param>
        public void SetOutputBasePath(string path)
        {
            userPreferences.Add($"{endpoint.GetType().FullName}.output_path", path);
#if UNITY_EDITOR
            Save();
#endif
        }

        public string defaultOutputPath
        {
            get
            {
                var persistentDataPath = Application.persistentDataPath;
#if UNITY_SIMULATION_CORE_PRESENT
                if (Unity.Simulation.Configuration.Instance.IsSimulationRunningInCloud())
                    persistentDataPath = Unity.Simulation.Configuration.Instance.GetStoragePath();
#endif
                return persistentDataPath;
            }
        }

        internal Metadata userPreferences { get; private set; }

        string m_CachedPathValue = string.Empty;

        string filePath
        {
            get
            {
                if (string.IsNullOrEmpty(m_CachedPathValue))
                {
                    m_CachedPathValue = Path.Combine(Application.dataPath, "..", "UserSettings", "PerceptionSettings.json");
                }

                return m_CachedPathValue;

            }
        }

        void Initialize()
        {
            if (File.Exists(filePath))
            {
                try
                {
                    var fileStr = File.ReadAllText(filePath);
                    s_Instance.userPreferences = Metadata.FromJson(fileStr);
                }
                catch (Exception e)
                {
                    Debug.Log($"Failed to load data file {e.Message}");
                    s_Instance.userPreferences = new Metadata();
                }
            }
            else
            {
                s_Instance.userPreferences = new Metadata();
            }
        }

        public static PerceptionSettings instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var obj = GameObject.Find(s_GameObjectName);
                    if (obj == null)
                    {
                        obj = new GameObject(s_GameObjectName);
                    }

                    s_Instance = obj.GetComponent<PerceptionSettings>();
                    if (s_Instance == null)
                    {
                        s_Instance = obj.AddComponent<PerceptionSettings>();
                    }

                    s_Instance.Initialize();
                }

                s_Instance.gameObject.hideFlags = s_HideInHierarchy ? HideFlags.HideInHierarchy | HideFlags.HideInInspector : HideFlags.None;

                return s_Instance;
            }
        }

#if UNITY_EDITOR
        public void Save()
        {
            var json = userPreferences.ToJson();
            File.WriteAllText(filePath, json);
        }
#endif

#if UNITY_EDITOR
        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(instance);
        }
#endif
        public bool useGUIDForFolderNaming = false;
        public ImageEncodingFormat RgbImageEncodingFormat = ImageEncodingFormat.Png;
        public string RgbImageEncodingFormatString
        {
            get
            {
                return Enum.GetName(typeof(ImageEncodingFormat), RgbImageEncodingFormat).ToLower();
            }
        }


        [SerializeReference]
        [ConsumerEndpointDrawer(typeof(IConsumerEndpoint))]
        public IConsumerEndpoint endpoint = new PerceptionEndpoint();
    }
}
