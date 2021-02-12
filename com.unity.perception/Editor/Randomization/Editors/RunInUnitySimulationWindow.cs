using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Simulation.Client;
using UnityEditor.Build.Reporting;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using ZipUtility;

namespace UnityEditor.Perception.Randomization
{
    class RunInUnitySimulationWindow : EditorWindow
    {
        string m_BuildDirectory;
        string m_BuildZipPath;
        SysParamDefinition[] m_SysParamDefinitions;
        IntegerField m_InstanceCountField;
        TextField m_RunNameField;
        IntegerField m_TotalIterationsField;
        int m_SysParamIndex;
        ObjectField m_ScenarioConfig;
        Button m_RunButton;
        Label m_PrevProjectId;
        Label m_PrevExecutionId;

        static string currentOpenScenePath => SceneManager.GetSceneAt(0).path;
        static ScenarioBase currentScenario => FindObjectOfType<ScenarioBase>();
        TextAsset scenarioConfig => (TextAsset)m_ScenarioConfig.value;
        string scenarioConfigAssetPath => AssetDatabase.GetAssetPath(scenarioConfig);

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

        void CreateRunInUnitySimulationUI()
        {
            var root = rootVisualElement;
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{StaticData.uxmlDir}/RunInUnitySimulationWindow.uxml").CloneTree(root);

            m_RunNameField = root.Q<TextField>("run-name");
            m_RunNameField.value = PlayerPrefs.GetString("SimWindow/runName");

            m_TotalIterationsField = root.Q<IntegerField>("total-iterations");
            m_TotalIterationsField.value = PlayerPrefs.GetInt("SimWindow/totalIterations");

            m_InstanceCountField = root.Q<IntegerField>("instance-count");
            m_InstanceCountField.value = PlayerPrefs.GetInt("SimWindow/instanceCount");

            m_SysParamDefinitions = API.GetSysParams();
            var sysParamMenu = root.Q<ToolbarMenu>("sys-param");
            for (var i = 0; i < m_SysParamDefinitions.Length; i++)
            {
                var index = i;
                var param = m_SysParamDefinitions[i];
                sysParamMenu.menu.AppendAction(
                    param.description,
                    action =>
                    {
                        m_SysParamIndex = index;
                        sysParamMenu.text = param.description;
                    });
            }

            m_SysParamIndex = PlayerPrefs.GetInt("SimWindow/sysParamIndex");
            sysParamMenu.text = m_SysParamDefinitions[m_SysParamIndex].description;

            m_ScenarioConfig = root.Q<ObjectField>("scenario-config");
            m_ScenarioConfig.objectType = typeof(TextAsset);
            var configPath = PlayerPrefs.GetString("SimWindow/scenarioConfig");
            if (configPath != string.Empty)
                m_ScenarioConfig.value = AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);

            m_RunButton = root.Q<Button>("run-button");
            m_RunButton.clicked += RunInUnitySimulation;

            m_PrevProjectId = root.Q<Label>("project-id");
            m_PrevProjectId.text = $"Project ID: {CloudProjectSettings.projectId}";

            m_PrevExecutionId = root.Q<Label>("execution-id");
            m_PrevExecutionId.text = $"Execution ID: {PlayerPrefs.GetString("SimWindow/prevExecutionId")}";

            var copyExecutionIdButton = root.Q<Button>("copy-execution-id");
            copyExecutionIdButton.clicked += () =>
                EditorGUIUtility.systemCopyBuffer = PlayerPrefs.GetString("SimWindow/prevExecutionId");

            var copyProjectIdButton = root.Q<Button>("copy-project-id");
            copyProjectIdButton.clicked += () =>
                EditorGUIUtility.systemCopyBuffer = CloudProjectSettings.projectId;
        }

        async void RunInUnitySimulation()
        {
            var runGuid = Guid.NewGuid();
            PerceptionEditorAnalytics.ReportRunInUnitySimulationStarted(
                runGuid,
                m_TotalIterationsField.value,
                m_InstanceCountField.value,
                null);
            try
            {
                ValidateSettings();
                SetNewPlayerPreferences();
                CreateLinuxBuildAndZip();
                await StartUnitySimulationRun(runGuid);
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                PerceptionEditorAnalytics.ReportRunInUnitySimulationFailed(runGuid, e.Message);
                throw;
            }
        }

        void ValidateSettings()
        {
            if (string.IsNullOrEmpty(m_RunNameField.value))
                throw new MissingFieldException("Empty run name");
            if (string.IsNullOrEmpty(currentOpenScenePath))
                throw new MissingFieldException("Invalid scene path");
            if (currentScenario == null)
                throw new MissingFieldException(
                    "There is not a Unity Simulation compatible scenario present in the scene");
            if (!StaticData.IsSubclassOfRawGeneric(typeof(UnitySimulationScenario<>), currentScenario.GetType()))
                throw new NotSupportedException(
                    "Scenario class must be derived from UnitySimulationScenario to run in Unity Simulation");
            if (scenarioConfig != null && Path.GetExtension(scenarioConfigAssetPath) != ".json")
                throw new NotSupportedException(
                    "Scenario configuration must be a JSON text asset");
        }

        void SetNewPlayerPreferences()
        {
            PlayerPrefs.SetString("SimWindow/runName", m_RunNameField.value);
            PlayerPrefs.SetInt("SimWindow/totalIterations", m_TotalIterationsField.value);
            PlayerPrefs.SetInt("SimWindow/instanceCount", m_InstanceCountField.value);
            PlayerPrefs.SetInt("SimWindow/sysParamIndex", m_SysParamIndex);
            PlayerPrefs.SetString("SimWindow/scenarioConfig",
                scenarioConfig != null ? scenarioConfigAssetPath : string.Empty);
        }

        void CreateLinuxBuildAndZip()
        {
            var projectBuildDirectory = $"{m_BuildDirectory}/{m_RunNameField.value}";
            if (!Directory.Exists(projectBuildDirectory))
                Directory.CreateDirectory(projectBuildDirectory);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { currentOpenScenePath },
                locationPathName = Path.Combine(projectBuildDirectory, $"{m_RunNameField.value}.x86_64"),
                target = BuildTarget.StandaloneLinux64
            };
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"The Linux build did not succeed: status = {summary.result}");

            EditorUtility.DisplayProgressBar("Unity Simulation Run", "Zipping Linux build...", 0f);
            Zip.DirectoryContents(projectBuildDirectory, m_RunNameField.value);
            m_BuildZipPath = projectBuildDirectory + ".zip";
        }

        List<AppParam> UploadAppParam()
        {
            var appParamIds = new List<AppParam>();
            var configuration = JObject.Parse(scenarioConfig != null
                ? File.ReadAllText(scenarioConfigAssetPath)
                : currentScenario.SerializeToJson());

            var constants = configuration["constants"];
            constants["totalIterations"] = m_TotalIterationsField.value;
            constants["instanceCount"] = m_InstanceCountField.value;

            var appParamName = $"{m_RunNameField.value}";
            var appParamsString = JsonConvert.SerializeObject(configuration, Formatting.Indented);
            var appParamId = API.UploadAppParam(appParamName, appParamsString);
            appParamIds.Add(new AppParam
            {
                id = appParamId,
                name = appParamName,
                num_instances = m_InstanceCountField.value
            });
            return appParamIds;
        }

        async Task StartUnitySimulationRun(Guid runGuid)
        {
            m_RunButton.SetEnabled(false);

            // Upload build
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            var buildId = await API.UploadBuildAsync(
                m_RunNameField.value,
                m_BuildZipPath,
                null, null,
                cancellationTokenSource,
                progress =>
                {
                    EditorUtility.DisplayProgressBar(
                        "Unity Simulation Run", "Uploading build...", progress * 0.90f);
                });
            if (token.IsCancellationRequested)
            {
                Debug.Log("The build upload process has been cancelled. Aborting Unity Simulation launch.");
                EditorUtility.ClearProgressBar();
                return;
            }

            // Generate and upload app-params
            EditorUtility.DisplayProgressBar("Unity Simulation Run", "Uploading app-params...", 0.90f);
            var appParams = UploadAppParam();

            // Upload run definition
            EditorUtility.DisplayProgressBar("Unity Simulation Run", "Uploading run definition...", 0.95f);
            var runDefinitionId = API.UploadRunDefinition(new RunDefinition
            {
                app_params = appParams.ToArray(),
                name = m_RunNameField.value,
                sys_param_id = m_SysParamDefinitions[m_SysParamIndex].id,
                build_id = buildId
            });

            // Execute run
            EditorUtility.DisplayProgressBar("Unity Simulation Run", "Executing run...", 1f);
            var run = Run.CreateFromDefinitionId(runDefinitionId);
            run.Execute();

            // Cleanup
            m_RunButton.SetEnabled(true);
            EditorUtility.ClearProgressBar();
            PerceptionEditorAnalytics.ReportRunInUnitySimulationSucceeded(runGuid, run.executionId);
            PlayerPrefs.SetString("SimWindow/prevExecutionId", run.executionId);
            m_PrevExecutionId.text = $"Execution ID: {run.executionId}";
        }
    }
}
