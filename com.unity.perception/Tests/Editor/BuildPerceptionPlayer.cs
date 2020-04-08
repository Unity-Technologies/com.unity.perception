using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.TestTools;

namespace EditorTests.BuildTests
{
    public class BuildPerceptionPlayer
    {
        public List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();
        public List<string> testScenesPaths = new List<string>();
        public string testSceneBase = "default base scene";

        private BuildReport report;
        private BuildSummary summary;

        private string buildPath = "Build/PerceptionBuild";

        [SetUp]
        public void SetUp()
        {
            TestsScenesPath();
        }

        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        [RequirePlatformSupport(BuildTarget.StandaloneWindows64)]
        [Test]
        public void BuildPlayerStandaloneWindows64()
        {
            BuildPlayer(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, buildPath, BuildOptions.IncludeTestAssemblies, out report, out summary);
            Assert.AreEqual(BuildResult.Succeeded, summary.result, " BuildTarget.StandaloneWindows64 failed to build");
        }

        [RequirePlatformSupport(BuildTarget.StandaloneLinux64)]
        [Test]
        public void BuildPlayerLinux()
        {
            BuildPlayer(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64, buildPath, BuildOptions.IncludeTestAssemblies, out report, out summary);
            Assert.AreEqual(BuildResult.Succeeded, summary.result, "BuildTarget.StandaloneLinux64 failed to build");
        }

        [UnityPlatform(RuntimePlatform.OSXEditor)]
        [RequirePlatformSupport(BuildTarget.StandaloneOSX)]
        [Test]
        public void BuildPlayerOSX()
        {
            BuildPlayer(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX, buildPath, BuildOptions.IncludeTestAssemblies, out report, out summary);
            Assert.AreEqual(BuildResult.Succeeded, summary.result, "BuildTarget.StandaloneLinux64 failed to build");
        }

        public void TestsScenesPath()
        {
            var allpaths = AssetDatabase.GetAllAssetPaths();
            foreach (var targetPath in allpaths)
            {
                if (targetPath.Contains("com.unity.perception") &&
                    targetPath.Contains("Runtime") &&
                    targetPath.Contains("ScenarioTests") &&
                    targetPath.Contains("Scenes"))
                {
                    if (targetPath.Contains("BaseScene.unity"))
                    {
                        testSceneBase = targetPath;
                        Debug.Log("Scenes Path : " + targetPath);
                        editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(targetPath, true));
                    }
                    else if (targetPath.EndsWith(".unity"))
                        if (targetPath.EndsWith(".unity"))
                    {
                        testScenesPaths.Add(targetPath);
                        Debug.Log("Scenes Path : " + targetPath);

                        editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(targetPath, true));
                    }
                }
                EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
            }
        }

        public void BuildPlayer(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, string buildOutputPath, BuildOptions buildOptions,
            out BuildReport buildReport, out BuildSummary buildSummary)
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.locationPathName = buildOutputPath;
            buildPlayerOptions.target = buildTarget;
            buildPlayerOptions.options = buildOptions;
            buildPlayerOptions.targetGroup = buildTargetGroup;

            if (buildTarget == BuildTarget.StandaloneLinux64)
                TurnOffBurstCompiler();

            buildReport = BuildPipeline.BuildPlayer(buildPlayerOptions);
            buildSummary = buildReport.summary;
        }

        public void TurnOffBurstCompiler()
        {
            var newLine = Environment.NewLine;
            const string BurstAOTSettingsFilePath = "ProjectSettings/BurstAotSettings_StandaloneLinux64.json";
            string[] BurstAOTSettingsText = new[]
            {
                "{" + newLine,
                @"    ""MonoBehaviour"": {" ,
                @"      ""m_EditorHideFlags"": 0,",
                @"      ""m_Name"": """",",
                @"      ""m_EditorClassIdentifier"":""Unity.Burst.Editor:Unity.Burst.Editor:BurstPlatformAotSettings"",",
                @"      ""DisableOptimisations"": false,",
                @"      ""DisableSafetyChecks"": true,",
                @"      ""DisableBurstCompilation"": true",
                "    }",
                "}"
            };

            File.WriteAllLines(BurstAOTSettingsFilePath, BurstAOTSettingsText);
            AssetDatabase.Refresh();
        }
    }
}

