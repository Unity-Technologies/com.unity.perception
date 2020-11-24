using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Simulation.Client;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.UIElements;
using UnityEngine.Experimental.Perception.Randomization.Editor;
using UnityEngine.Experimental.Perception.Randomization.Scenarios;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using ZipUtility;

namespace UnityEngine.Perception.Randomization.Editor
{
    class RunInUnitySimulationWindow : EditorWindow
    {
        string m_BuildDirectory;

        string m_BuildZipPath;
        SysParamDefinition m_SysParam;

        TextField m_RunNameField;
        IntegerField m_TotalIterationsField;
        IntegerField m_InstanceCountField;
        ObjectField m_MainSceneField;
        ObjectField m_ScenarioField;
        Button m_RunButton;

        [MenuItem("Window/Run in Unity Simulation")]
        static void ShowWindow()
        {
            var window = GetWindow<RunInUnitySimulationWindow>();
            window.titleContent = new GUIContent("Run In Unity Simulation");
            window.minSize = new Vector2(250, 50);
            window.Show();
        }

        void OnEnable()
        {
            m_BuildDirectory = Application.dataPath + "/../Build";
            Project.Activate();
            Project.clientReadyStateChanged += CreateEstablishingConnectionUI;
            CreateEstablishingConnectionUI(Project.projectIdState);
        }

        void OnFocus()
        {
            Application.runInBackground = true;
        }

        void OnLostFocus()
        {
            Application.runInBackground = false;
        }

        void CreateEstablishingConnectionUI(Project.State state)
        {
            rootVisualElement.Clear();
            if (Project.projectIdState == Project.State.Pending)
            {
                var waitingText = new TextElement();
                waitingText.text = "Waiting for connection to Unity Cloud...";
                rootVisualElement.Add(waitingText);
            }
            else if (Project.projectIdState == Project.State.Invalid)
            {
                var waitingText = new TextElement();
                waitingText.text = "The current project must be associated with a valid Unity Cloud project " +
                    "to run in Unity Simulation";
                rootVisualElement.Add(waitingText);
            }
            else
            {
                CreateRunInUnitySimulationUI();
            }
        }

        /// <summary>
        /// Enables a visual element to remember values between editor sessions
        /// </summary>
        /// <param name="element">The visual element to enable view data for</param>
        static void SetViewDataKey(VisualElement element)
        {
            element.viewDataKey = $"RunInUnitySimulation_{element.name}";
        }

        void CreateRunInUnitySimulationUI()
        {
            var root = rootVisualElement;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/RunInUnitySimulationWindow.uxml").CloneTree(root);

            m_RunNameField = root.Q<TextField>("run-name");
            SetViewDataKey(m_RunNameField);

            m_TotalIterationsField = root.Q<IntegerField>("total-iterations");
            SetViewDataKey(m_TotalIterationsField);

            m_InstanceCountField = root.Q<IntegerField>("instance-count");
            SetViewDataKey(m_InstanceCountField);

            m_MainSceneField = root.Q<ObjectField>("main-scene");
            m_MainSceneField.objectType = typeof(SceneAsset);
            if (SceneManager.sceneCount > 0)
            {
                var path = SceneManager.GetSceneAt(0).path;
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                m_MainSceneField.value = asset;
            }

            m_ScenarioField = root.Q<ObjectField>("scenario");
            m_ScenarioField.objectType = typeof(ScenarioBase);
            m_ScenarioField.value = FindObjectOfType<ScenarioBase>();

            var sysParamDefinitions = API.GetSysParams();
            var sysParamMenu = root.Q<ToolbarMenu>("sys-param");
            foreach (var definition in sysParamDefinitions)
            {
                sysParamMenu.menu.AppendAction(
                    definition.description,
                    action =>
                    {
                        m_SysParam = definition;
                        sysParamMenu.text = definition.description;
                    });
            }
            sysParamMenu.text = sysParamDefinitions[0].description;
            m_SysParam = sysParamDefinitions[0];

            m_RunButton = root.Q<Button>("run-button");
            m_RunButton.clicked += RunInUnitySimulation;
        }

        async void RunInUnitySimulation()
        {
            ValidateSettings();
            CreateLinuxBuildAndZip();
            await StartUnitySimulationRun();
        }

        void ValidateSettings()
        {
            if (string.IsNullOrEmpty(m_RunNameField.value))
                throw new MissingFieldException("Empty run name");
            if (m_MainSceneField.value == null)
                throw new MissingFieldException("Main scene unselected");
            if (m_ScenarioField.value == null)
                throw new MissingFieldException("Scenario unselected");
            var scenario = (ScenarioBase)m_ScenarioField.value;
            if (!StaticData.IsSubclassOfRawGeneric(typeof(UnitySimulationScenario<>), scenario.GetType()))
                throw new NotSupportedException(
                    "Scenario class must be derived from UnitySimulationScenario to run in Unity Simulation");
        }

        void CreateLinuxBuildAndZip()
        {
            // Create build directory
            var projectBuildDirectory = $"{m_BuildDirectory}/{m_RunNameField.value}";
            if (!Directory.Exists(projectBuildDirectory))
                Directory.CreateDirectory(projectBuildDirectory);

            // Create Linux build
            Debug.Log("Creating Linux build...");
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { AssetDatabase.GetAssetPath(m_MainSceneField.value) },
                locationPathName = Path.Combine(projectBuildDirectory, $"{m_RunNameField.value}.x86_64"),
                target = BuildTarget.StandaloneLinux64
            };
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"Build did not succeed: status = {summary.result}");
            Debug.Log("Created Linux build");

            // Zip the build
            Debug.Log("Starting to zip...");
            Zip.DirectoryContents(projectBuildDirectory, m_RunNameField.value);
            m_BuildZipPath = projectBuildDirectory + ".zip";
            Debug.Log("Created build zip");
        }

        List<AppParam> GenerateAppParamIds(CancellationToken token)
        {
            var appParamIds = new List<AppParam>();
            for (var i = 0; i < m_InstanceCountField.value; i++)
            {
                if (token.IsCancellationRequested)
                    return null;
                var appParamName = $"{m_RunNameField.value}_{i}";
                var appParamId = API.UploadAppParam(appParamName, new UnitySimulationConstants
                {
                    totalIterations = m_TotalIterationsField.value,
                    instanceCount = m_InstanceCountField.value,
                    instanceIndex = i
                });
                appParamIds.Add(new AppParam()
                {
                    id = appParamId,
                    name = appParamName,
                    num_instances = 1
                });
            }
            return appParamIds;
        }

        async Task StartUnitySimulationRun()
        {
            m_RunButton.SetEnabled(false);
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            Debug.Log("Uploading build...");
            var buildId = await API.UploadBuildAsync(
                m_RunNameField.value,
                m_BuildZipPath,
                cancellationTokenSource: cancellationTokenSource);
            Debug.Log($"Build upload complete: build id {buildId}");

            var appParams = GenerateAppParamIds(token);
            if (token.IsCancellationRequested)
            {
                Debug.Log("Run cancelled");
                return;
            }
            Debug.Log($"Generated app-param ids: {appParams.Count}");

            var runDefinitionId = API.UploadRunDefinition(new RunDefinition
            {
                app_params = appParams.ToArray(),
                name = m_RunNameField.value,
                sys_param_id = m_SysParam.id,
                build_id = buildId
            });
            Debug.Log($"Run definition upload complete: run definition id {runDefinitionId}");

            var run = Run.CreateFromDefinitionId(runDefinitionId);
            run.Execute();
            cancellationTokenSource.Dispose();
            Debug.Log($"Executing run: {run.executionId}");
            m_RunButton.SetEnabled(true);
        }
    }
}
