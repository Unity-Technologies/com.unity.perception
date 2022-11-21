using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
#endif
using UnityEngine;

namespace UnityEngine.Perception.Settings
{
    [Serializable]
    class PerceptionPackageVersion
    {
        static string s_FileName = "PerceptionPackageVersion";
        static string s_FolderPath = Path.Combine(Application.dataPath, "Resources");
        static string s_FilePath = Path.Combine(s_FolderPath, $"{s_FileName}.json");

        public static string perceptionVersion => instance.m_PerceptionVersion;

        public string m_PerceptionVersion;

        static PerceptionPackageVersion s_Instance;
        static PerceptionPackageVersion instance
        {
            get
            {
                if (s_Instance == null)
                {
#if UNITY_EDITOR
                    s_Instance = new PerceptionPackageVersion { m_PerceptionVersion = GetPackageVersion()};
                    return s_Instance;
#else
                    try
                    {
                        var fileStr = ReadFile();
                        s_Instance = JsonUtility.FromJson<PerceptionPackageVersion>(fileStr);
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Failed to load data file {e.Message}");
                        s_Instance = new PerceptionPackageVersion();
                    }
#endif
                }
                return s_Instance;
            }
        }

        internal static string ReadFile()
        {
            var result = string.Empty;
#if UNITY_EDITOR
            if (File.Exists(s_FilePath))
            {
                result = File.ReadAllText(s_FilePath);
            }
#else
            var asset = Resources.Load<TextAsset>(s_FileName);
            if (asset != null)
            {
                result = asset.text;
            }
#endif
            return result;
        }

#if UNITY_EDITOR

        static string GetPackageVersion()
        {
            var assembly = typeof(PerceptionPackageVersion).Assembly;
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
            return packageInfo.version;
        }

        internal void Save()
        {
            if (!Directory.Exists(s_FolderPath))
            {
                Directory.CreateDirectory(s_FolderPath);
            }

            File.WriteAllText(s_FilePath, JsonUtility.ToJson(this, false));
        }

        internal void Delete()
        {
            if (File.Exists(s_FilePath))
            {
                File.Delete(s_FilePath);
            }
        }

        class PerceptionPackageVersionSaver : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {
            public int callbackOrder => 0;
            public void OnPostprocessBuild(BuildReport report)
            {
                instance.Delete();
            }

            public void OnPreprocessBuild(BuildReport report)
            {
                instance.Save();
            }
        }
#endif
    }
}
