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
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using ZipUtility;
using Random = System.Random;

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
        UIntField m_RandomSeedField;
        ToolbarMenu m_SysParamMenu;
        int m_SysParamIndex;
        ObjectField m_ScenarioConfigField;
        Button m_RunButton;
        Label m_PrevRunNameLabel;
        Label m_ProjectIdLabel;
        Label m_PrevExecutionIdLabel;
        Label m_PrevRandomSeedLabel;
        RunParameters m_RunParameters;
        const string  k_SupportedGPUString = "NVIDIA";

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
            m_TotalIterationsField = root.Q<IntegerField>("total-iterations");
            m_InstanceCountField = root.Q<IntegerField>("instance-count");
            m_RandomSeedField = root.Q<UIntField>("random-seed");

            var randomizeSeedButton = root.Q<Button>("randomize-seed");
            randomizeSeedButton.clicked += () =>
            {
                var bytes = new byte[4];
                new Random().NextBytes(bytes);
                m_RandomSeedField.value = BitConverter.ToUInt32(bytes, 0);
            };

            m_SysParamDefinitions = API.GetSysParams();
            m_SysParamMenu = root.Q<ToolbarMenu>("sys-param");
            for (var i = 0; i < m_SysParamDefinitions.Length; i++)
            {
                var index = i;
                var param = m_SysParamDefinitions[i];
                m_SysParamMenu.menu.AppendAction(
                    param.description,
                    action =>
                    {
                        m_SysParamIndex = index;
                        m_SysParamMenu.text = param.description;
                    });
            }

            m_ScenarioConfigField = root.Q<ObjectField>("scenario-config");
            m_ScenarioConfigField.objectType = typeof(TextAsset);
            var configPath = PlayerPrefs.GetString("SimWindow/scenarioConfig");
            if (configPath != string.Empty)
                m_ScenarioConfigField.value = AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);

            m_RunButton = root.Q<Button>("run-button");
            m_RunButton.clicked += RunInUnitySimulation;

            m_PrevRunNameLabel = root.Q<Label>("prev-run-name");
            m_ProjectIdLabel = root.Q<Label>("project-id");
            m_PrevExecutionIdLabel = root.Q<Label>("execution-id");
            m_PrevRandomSeedLabel = root.Q<Label>("prev-random-seed");

            var copyExecutionIdButton = root.Q<Button>("copy-execution-id");
            copyExecutionIdButton.clicked += () =>
                EditorGUIUtility.systemCopyBuffer = PlayerPrefs.GetString("SimWindow/prevExecutionId");

            var copyProjectIdButton = root.Q<Button>("copy-project-id");
            copyProjectIdButton.clicked += () =>
                EditorGUIUtility.systemCopyBuffer = CloudProjectSettings.projectId;

            var copyPrevRandomSeedButton = root.Q<Button>("copy-prev-random-seed");
            copyPrevRandomSeedButton.clicked += () =>
                EditorGUIUtility.systemCopyBuffer = PlayerPrefs.GetString("SimWindow/prevRandomSeed");

            SetFieldsFromPlayerPreferences();
        }

        void SetFieldsFromPlayerPreferences()
        {
            m_RunNameField.value = IncrementRunName(PlayerPrefs.GetString("SimWindow/runName"));
            m_TotalIterationsField.value = PlayerPrefs.GetInt("SimWindow/totalIterations");
            m_InstanceCountField.value = PlayerPrefs.GetInt("SimWindow/instanceCount");
            m_SysParamIndex = PlayerPrefs.GetInt("SimWindow/sysParamIndex");

            var prevRandomSeed = PlayerPrefs.GetString("SimWindow/prevRandomSeed");
            m_RandomSeedField.value = string.IsNullOrEmpty(prevRandomSeed)
                ? SamplerUtility.largePrime : uint.Parse(prevRandomSeed);

            m_SysParamMenu.text = m_SysParamDefinitions[m_SysParamIndex].description;
            m_PrevRunNameLabel.text = $"Run Name: {PlayerPrefs.GetString("SimWindow/runName")}";
            m_ProjectIdLabel.text = $"Project ID: {CloudProjectSettings.projectId}";
            m_PrevExecutionIdLabel.text = $"Execution ID: {PlayerPrefs.GetString("SimWindow/prevExecutionId")}";
            m_PrevRandomSeedLabel.text = $"Random Seed: {PlayerPrefs.GetString("SimWindow/prevRandomSeed")}";
        }

        static string IncrementRunName(string runName)
        {
            if (string.IsNullOrEmpty(runName))
                return "Run0";
            var stack = new Stack<char>();
            var i = runName.Length - 1;
            for (; i >= 0; i--)
            {
                if (!char.IsNumber(runName[i]))
                    break;
                stack.Push(runName[i]);
            }
            if (stack.Count == 0)
                return runName + "1";
            var numericString = string.Concat(stack.ToArray());
            var runVersion = int.Parse(numericString) + 1;
            return runName.Substring(0, i + 1) + runVersion;
        }

        async void RunInUnitySimulation()
        {
            #if PLATFORM_CLOUD_RENDERING
            if (!m_SysParamDefinitions[m_SysParamIndex].description.Contains(k_SupportedGPUString))
            {
                EditorUtility.DisplayDialog("Unsupported Sysparam",
                    "The current selection of the Sysparam " + m_SysParamDefinitions[m_SysParamIndex].description +
                    " is not supported by this build target. Please select a sysparam with GPU", "Ok");
                return;
            }
            #endif
            m_RunParameters = new RunParameters
            {
                runName = m_RunNameField.value,
                totalIterations = m_TotalIterationsField.value,
                instanceCount = m_InstanceCountField.value,
                randomSeed = m_RandomSeedField.value,
                sysParamIndex = m_SysParamIndex,
                scenarioConfig = (TextAsset)m_ScenarioConfigField.value,
                currentOpenScenePath = SceneManager.GetSceneAt(0).path,
                currentScenario = FindObjectOfType<ScenarioBase>()
            };
            var runGuid = Guid.NewGuid();
            PerceptionEditorAnalytics.ReportRunInUnitySimulationStarted(
                runGuid,
                m_RunParameters.totalIterations,
                m_RunParameters.instanceCount,
                null);
            try
            {
                ValidateSettings();
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
            if (string.IsNullOrEmpty(m_RunParameters.runName))
                throw new MissingFieldException("Empty run name");
            if (m_RunParameters.instanceCount <= 0)
                throw new NotSupportedException("Invalid instance count specified");
            if (m_RunParameters.totalIterations <= 0)
                throw new NotSupportedException("Invalid total iteration count specified");
            if (string.IsNullOrEmpty(m_RunParameters.currentOpenScenePath))
                throw new MissingFieldException("Invalid scene path");
            if (m_RunParameters.currentScenario == null)
                throw new MissingFieldException(
                    "There is not a Unity Simulation compatible scenario present in the scene");
            if (!StaticData.IsSubclassOfRawGeneric(
                typeof(UnitySimulationScenario<>), m_RunParameters.currentScenario.GetType()))
                throw new NotSupportedException(
                    "Scenario class must be derived from UnitySimulationScenario to run in Unity Simulation");
            if (m_RunParameters.scenarioConfig != null &&
                Path.GetExtension(m_RunParameters.scenarioConfigAssetPath) != ".json")
                throw new NotSupportedException(
                    "Scenario configuration must be a JSON text asset");
        }

        void CreateLinuxBuildAndZip()
        {
            var projectBuildDirectory = $"{m_BuildDirectory}/{m_RunParameters.runName}";
            if (!Directory.Exists(projectBuildDirectory))
                Directory.CreateDirectory(projectBuildDirectory);
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { m_RunParameters.currentOpenScenePath },
                locationPathName = Path.Combine(projectBuildDirectory, $"{m_RunParameters.runName}.x86_64"),
#if PLATFORM_CLOUD_RENDERING
                target = BuildTarget.CloudRendering,
#else
                target = BuildTarget.StandaloneLinux64
#endif
            };
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"The Linux build did not succeed: status = {summary.result}");

            EditorUtility.DisplayProgressBar("Unity Simulation Run", "Zipping Linux build...", 0f);
            Zip.DirectoryContents(projectBuildDirectory, m_RunParameters.runName);
            m_BuildZipPath = projectBuildDirectory + ".zip";
        }

        List<AppParam> UploadAppParam()
        {
            var appParamIds = new List<AppParam>();
            var configuration = JObject.Parse(m_RunParameters.scenarioConfig != null
                ? File.ReadAllText(m_RunParameters.scenarioConfigAssetPath)
                : m_RunParameters.currentScenario.SerializeToJson());

            var constants = configuration["constants"];
            constants["totalIterations"] = m_RunParameters.totalIterations;
            constants["instanceCount"] = m_RunParameters.instanceCount;
            constants["randomSeed"] = m_RunParameters.randomSeed;

            var appParamName = $"{m_RunParameters.runName}";
            var appParamsString = JsonConvert.SerializeObject(configuration, Formatting.Indented);
            var appParamId = API.UploadAppParam(appParamName, appParamsString);
            appParamIds.Add(new AppParam
            {
                id = appParamId,
                name = appParamName,
                num_instances = m_RunParameters.instanceCount
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
                m_RunParameters.runName,
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
                name = m_RunParameters.runName,
                sys_param_id = m_SysParamDefinitions[m_RunParameters.sysParamIndex].id,
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

            // Set new Player Preferences
            PlayerPrefs.SetString("SimWindow/runName", m_RunParameters.runName);
            PlayerPrefs.SetString("SimWindow/prevExecutionId", run.executionId);
            PlayerPrefs.SetInt("SimWindow/totalIterations", m_RunParameters.totalIterations);
            PlayerPrefs.SetInt("SimWindow/instanceCount", m_RunParameters.instanceCount);
            PlayerPrefs.SetString("SimWindow/prevRandomSeed", m_RunParameters.randomSeed.ToString());
            PlayerPrefs.SetInt("SimWindow/sysParamIndex", m_RunParameters.sysParamIndex);
            PlayerPrefs.SetString("SimWindow/scenarioConfig",
                m_RunParameters.scenarioConfig != null ? m_RunParameters.scenarioConfigAssetPath : string.Empty);

            SetFieldsFromPlayerPreferences();
        }

        struct RunParameters
        {
            public string runName;
            public int totalIterations;
            public int instanceCount;
            public uint randomSeed;
            public int sysParamIndex;
            public TextAsset scenarioConfig;
            public string currentOpenScenePath;
            public ScenarioBase currentScenario;

            public string scenarioConfigAssetPath => AssetDatabase.GetAssetPath(scenarioConfig);
        }
    }
}
