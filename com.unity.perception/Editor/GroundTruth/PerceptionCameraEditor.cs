using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.Perception.GroundTruth.Sensors;

using UnityEngine.Perception.Settings;
#if HDRP_PRESENT
using UnityEngine.Rendering.HighDefinition;
#endif

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(PerceptionCamera))]
    sealed class PerceptionCameraEditor : Editor
    {
        Dictionary<SerializedProperty, CameraLabelerDrawer> m_CameraLabelerDrawers = new Dictionary<SerializedProperty, CameraLabelerDrawer>();
        ReorderableList m_LabelersList;
        List<Type> m_SensorTypes = new List<Type>();
        string[] m_SensorTypeOptions;

        const int k_UpdateInterval = 500; //ms
        DateTime m_SceneStatusUpdateTime = DateTime.MinValue;
        PerceptionCamera[] m_OtherPerceptionCameras;
        ScenarioBase m_Scenario;
        bool m_ShouldWarnAboutDifferingDeltaTimes;
        bool m_ShouldWarnAboutDifferingCaptureTriggerModes;

        SerializedProperty labelersProperty => this.serializedObject.FindProperty("m_Labelers");

        PerceptionCamera perceptionCamera => ((PerceptionCamera)this.target);

        public void OnEnable()
        {
            var sensorTypeCache = TypeCache.GetTypesDerivedFrom<CameraSensor>();
            foreach (var type in sensorTypeCache)
                if (!type.IsAbstract)
                    m_SensorTypes.Add(type);

            m_SensorTypeOptions = new string[m_SensorTypes.Count];
            for (var i = 0; i < m_SensorTypes.Count; i++)
                m_SensorTypeOptions[i] = ObjectNames.NicifyVariableName(m_SensorTypes[i].Name);

            m_LabelersList = new ReorderableList(this.serializedObject, labelersProperty, false, true, true, true);
            m_LabelersList.drawHeaderCallback = (rect) =>
            {
                EditorGUI.LabelField(rect, "" +
                    "Camera Labelers", EditorStyles.largeLabel);
            };
            m_LabelersList.elementHeightCallback = GetElementHeight;
            m_LabelersList.drawElementCallback = DrawElement;
            m_LabelersList.onAddCallback += OnAdd;
            m_LabelersList.onRemoveCallback += OnRemove;
        }

        float GetElementHeight(int index)
        {
            if (m_LabelersList.count == 0)
                return 0f;
            var serializedProperty = labelersProperty;
            var element = serializedProperty.GetArrayElementAtIndex(index);
            var editor = GetCameraLabelerDrawer(element, index);
            return editor.GetElementHeight(element);
        }

        void DrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            var element = labelersProperty.GetArrayElementAtIndex(index);
            var editor = GetCameraLabelerDrawer(element, index);
            editor.OnGUI(rect, element);
        }

        void OnRemove(ReorderableList list)
        {
            labelersProperty.DeleteArrayElementAtIndex(list.index);
            serializedObject.ApplyModifiedProperties();
        }

        void OnAdd(ReorderableList list)
        {
            Undo.RegisterCompleteObjectUndo(target, "Remove camera labeler");

            var dropdownOptions = TypeCache.GetTypesDerivedFrom<CameraLabeler>();
            var menu = new GenericMenu();
            foreach (var option in dropdownOptions)
            {
                if (option.Namespace != null && option.Namespace.Contains("Test"))
                {
                    continue;
                }
                var localOption = option;
                var labelerName = ObjectNames.NicifyVariableName(option.Name);
                menu.AddItem(new GUIContent(labelerName), false, () => AddLabeler(localOption));
            }

            menu.ShowAsContext();
        }

        void AddLabeler(Type labelerType)
        {
            var labeler = (CameraLabeler)Activator.CreateInstance(labelerType);
            labeler.enabled = true;
            perceptionCamera.AddLabeler(labeler);
            serializedObject.ApplyModifiedProperties();
        }

        const string k_FrametimeTitle = "Simulation Delta Time";
        const float k_DeltaTimeTooLarge = 200;

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (serializedObject.FindProperty(nameof(perceptionCamera.useAccumulation)).boolValue && (DateTime.Now - m_SceneStatusUpdateTime).TotalMilliseconds > k_UpdateInterval)
                {
                    m_OtherPerceptionCameras = FindObjectsOfType<PerceptionCamera>();
                    m_Scenario = FindObjectOfType<ScenarioBase>();
                    m_SceneStatusUpdateTime = DateTime.Now;
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.id)), new GUIContent("ID", "Provide a unique sensor ID for the camera."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.description)), new GUIContent("Description", "Provide a description for this camera (optional)."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.showVisualizations)), new GUIContent("Show Labeler Visualizations", "Display realtime visualizations for labelers that are currently active on this camera."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.captureRgbImages)), new GUIContent("Save Camera RGB Output to Disk", "For each captured frame, save an RGB image of the camera's output to disk."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.alphaThreshold)));

                var overrideInstanceSegProp = serializedObject.FindProperty(nameof(perceptionCamera.overrideLayerMask));
                EditorGUILayout.PropertyField(overrideInstanceSegProp, new GUIContent("Override Layer Mask", "The layer mask used for filtering objects before capturing labeler data."));
                if (overrideInstanceSegProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("m_LayerMask"), new GUIContent("Layer Mask", "The layer mask to use when rendering instance segmentation images."));
                    EditorGUI.indentLevel--;
                }

                var sensor = perceptionCamera.cameraSensor;
                var currentSensorType = sensor.GetType();
                var sensorIndex = -1;
                for (var i = 0; i < m_SensorTypes.Count; i++)
                {
                    if (m_SensorTypes[i] == currentSensorType)
                    {
                        sensorIndex = i;
                        break;
                    }
                }

                var selectedSensorTypeIndex = EditorGUILayout.Popup("Sensor Type", sensorIndex, m_SensorTypeOptions);
                if (selectedSensorTypeIndex != sensorIndex)
                {
                    var sensorType = m_SensorTypes[selectedSensorTypeIndex];
                    var sensorInstance = (CameraSensor)Activator.CreateInstance(sensorType);
                    Undo.RegisterCompleteObjectUndo(serializedObject.targetObject, "Set CameraSensor");
                    perceptionCamera.cameraSensor = sensorInstance;
                    serializedObject.Update();
                }

                var rgbSensorProperty = serializedObject.FindProperty("m_CameraSensor");
                if (rgbSensorProperty.hasChildren)
                {
                    var nextSiblingProperty = rgbSensorProperty.Copy();
                    nextSiblingProperty.NextVisible(false);
                    rgbSensorProperty.NextVisible(true);
                    GUILayout.BeginVertical("TextArea");
                    do
                    {
                        if (SerializedProperty.EqualContents(rgbSensorProperty, nextSiblingProperty))
                            break;
                        EditorGUILayout.PropertyField(rgbSensorProperty, true);
                    }
                    while (rgbSensorProperty.NextVisible(false));
                    GUILayout.EndVertical();
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.captureTriggerMode)), new GUIContent("Capture Trigger Mode", $"The method of triggering captures for this camera. In {nameof(CaptureTriggerMode.Scheduled)} mode, captures happen automatically based on a start frame and frame delta time. In {nameof(CaptureTriggerMode.Manual)} mode, captures should be triggered manually through calling the {nameof(perceptionCamera.RequestCapture)} method of {nameof(PerceptionCamera)}."));

                if (!serializedObject.FindProperty(nameof(perceptionCamera.useAccumulation)).boolValue)
                {
                    m_ShouldWarnAboutDifferingCaptureTriggerModes = false;
                }
                else if (m_OtherPerceptionCameras != null)
                {
                    m_ShouldWarnAboutDifferingCaptureTriggerModes = false;
                    foreach (var cam in m_OtherPerceptionCameras)
                    {
                        if (cam.useAccumulation && (int)cam.captureTriggerMode != serializedObject.FindProperty(nameof(perceptionCamera.captureTriggerMode)).intValue)
                        {
                            m_ShouldWarnAboutDifferingCaptureTriggerModes = true;
                            break;
                        }
                    }
                }

                if (m_ShouldWarnAboutDifferingCaptureTriggerModes)
                    EditorGUILayout.HelpBox($"This camera is using accumulation and has a different {nameof(perceptionCamera.captureTriggerMode)} from other {nameof(PerceptionCamera)}s active in the Scene. When using accumulation, all cameras should have identical {nameof(perceptionCamera.captureTriggerMode)}s.", MessageType.Warning);


                GUILayout.Space(5);
                if (perceptionCamera.captureTriggerMode.Equals(CaptureTriggerMode.Scheduled))
                {
                    GUILayout.BeginVertical("TextArea");
                    EditorGUILayout.LabelField("Scheduled Capture Properties", EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.simulationDeltaTime)), new GUIContent(k_FrametimeTitle, $"Sets Unity's Time.{nameof(Time.captureDeltaTime)} to the specified number, causing a fixed number of frames to be simulated for each second of elapsed simulation time regardless of the capabilities of the underlying hardware. Thus, simulation time and real time will not be synchronized. Note that large {k_FrametimeTitle} values will lead to lower performance as the engine will need to simulate longer periods of elapsed time for each rendered frame."));

                    if (!serializedObject.FindProperty(nameof(perceptionCamera.useAccumulation)).boolValue)
                    {
                        m_ShouldWarnAboutDifferingDeltaTimes = false;
                    }
                    else if (m_OtherPerceptionCameras != null)
                    {
                        m_ShouldWarnAboutDifferingDeltaTimes = false;
                        foreach (var cam in m_OtherPerceptionCameras)
                        {
                            if (cam.useAccumulation && Math.Abs(cam.simulationDeltaTime - serializedObject.FindProperty(nameof(perceptionCamera.simulationDeltaTime)).floatValue) > 0.0001)
                            {
                                m_ShouldWarnAboutDifferingDeltaTimes = true;
                                break;
                            }
                        }
                    }

                    if (m_ShouldWarnAboutDifferingDeltaTimes)
                        EditorGUILayout.HelpBox($"This camera is using accumulation and has a different {nameof(perceptionCamera.simulationDeltaTime)} from other {nameof(PerceptionCamera)}s active in the Scene. When using accumulation, all cameras should have identical {nameof(perceptionCamera.simulationDeltaTime)}s, otherwise some of the captured images will not be properly accumulated.", MessageType.Warning);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.firstCaptureFrame)), new GUIContent("Start at Frame", $"Frame number at which this camera starts capturing."));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.framesBetweenCaptures)), new GUIContent("Frames Between Captures", "The number of frames to simulate and render between the camera's scheduled captures. Setting this to 0 makes the camera capture every frame."));

                    if (perceptionCamera.simulationDeltaTime > k_DeltaTimeTooLarge)
                    {
                        EditorGUILayout.HelpBox($"Large {k_FrametimeTitle} values can lead to significantly lower simulation performance.", MessageType.Warning);
                    }

                    var interval = (perceptionCamera.framesBetweenCaptures + 1) * perceptionCamera.simulationDeltaTime;
                    var startTime = perceptionCamera.simulationDeltaTime * perceptionCamera.firstCaptureFrame;
                    EditorGUILayout.HelpBox($"First capture at frame {perceptionCamera.firstCaptureFrame} ({startTime} seconds) and consecutive captures every {perceptionCamera.framesBetweenCaptures} frames ({interval} seconds) of simulation.", MessageType.None);
                    if (perceptionCamera.firstCaptureFrame > 0 && m_Scenario != null && m_Scenario is FixedLengthScenario fls && fls.framesPerIteration <= perceptionCamera.firstCaptureFrame)
                        EditorGUILayout.HelpBox($"The Scenario in the Scene runs for only {fls.framesPerIteration} frame(s) each Iteration. You have set this camera's Start at Frame to {perceptionCamera.firstCaptureFrame}, but it should be at least {perceptionCamera.firstCaptureFrame+1} for the camera to capture any frames.", MessageType.Warning);

                    GUILayout.EndVertical();
                }
                else
                {
                    GUILayout.BeginVertical("TextArea");
                    EditorGUILayout.LabelField("Manual Capture Properties", EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.manualSensorAffectSimulationTiming)), new GUIContent("Affect Simulation Timing", $"Have this camera affect simulation timings (similar to a scheduled camera) by requesting a specific frame delta time."));

                    if (perceptionCamera.manualSensorAffectSimulationTiming)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.simulationDeltaTime)), new GUIContent(k_FrametimeTitle, $"Sets Unity's Time.{nameof(Time.captureDeltaTime)} to the specified number, causing a fixed number of frames to be generated for each second of elapsed simulation time regardless of the capabilities of the underlying hardware. Thus, simulation time and real time will not be synchronized."));
                    }

                    EditorGUILayout.HelpBox($"Captures should be triggered manually through calling the {nameof(perceptionCamera.RequestCapture)} method of {nameof(PerceptionCamera)}.", MessageType.None);
                    GUILayout.EndVertical();
                }
                GUILayout.Space(5);

#if HDRP_PRESENT

                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.useAccumulation)), new GUIContent("Use Accumulation", "Whether or not to use accumulation when generating frames, this is useful for rendering techniques like path tracing and effects like motion blur"));
                if (GUILayout.Button("Edit Global Accumulation Settings"))
                {
                    SettingsService.OpenProjectSettings("Project/Perception");
                }
                EditorGUILayout.EndHorizontal();

                var currentVersion = InternalEditorUtility.GetUnityDisplayVersion();
                var versionSplit = currentVersion.Split('.');
                var major = versionSplit[0];
                var minor = versionSplit[1];
                int patch = -1;
                var isValidPatch = Int32.TryParse(versionSplit[2].Split('f')[0], out patch);
                if (isValidPatch && major == "2022" && minor == "1" && patch < 10)
                {
                    EditorGUILayout.HelpBox("For accumulation in Unity 2022.1, Unity 2022.1.10+ is required, your current version is " + currentVersion, MessageType.Warning);
                }
                if (isValidPatch && major == "2021" && minor == "3" && patch < 7)
                {
                    EditorGUILayout.HelpBox("For accumulation in Unity 2021.3, Unity 2021.3.7+ is required, your current version is " + currentVersion, MessageType.Warning);
                }
#endif
                serializedObject.ApplyModifiedProperties();

                GUILayout.Space(15);

                if (PerceptionSettings.endpoint == null)
                {
                    EditorGUILayout.HelpBox("Currently there is not a consumer endpoint setup for this simulation. " +
                        "One can be assigned in Project Settings -> Perception -> Active Endpoint", MessageType.Error);
                    GUILayout.Space(15);
                }

                var nullLabelers = perceptionCamera.labelers.Count(l => l == null);
                if (nullLabelers > 0)
                {
#if UNITY_2021_3_OR_NEWER
                    var missingTypes = SerializationUtility.GetManagedReferencesWithMissingTypes(perceptionCamera);
                    var warning = $"{nullLabelers} Unknown Labeler(s)";
                    if (missingTypes.Length > 0)
                    {
                        warning = missingTypes
                            .Aggregate("", (s, mt) => $"{s}, {mt.className}")
                            .Substring(2);
                    }

                    EditorGUILayout.HelpBox(
                        $"The following labelers were not found: {warning}. You can add the missing types " +
                        $"back in or remove all missing labelers using the option below.",
                        MessageType.Error
                    );
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button($"Remove {nullLabelers} Missing Labeler(s)"))
                    {
                        perceptionCamera.ClearNullLabelers();
                        SerializationUtility.ClearAllManagedReferencesWithMissingTypes(perceptionCamera);
                    }

                    GUILayout.EndHorizontal();
#else
                    EditorGUILayout.HelpBox(
                        "One or more labelers have missing scripts. See the console for more information.",
                        MessageType.Error
                    );
#endif
                    GUILayout.Space(10);
                }
                else
                    m_LabelersList.DoLayoutList();
            }

            var lastEndpointType = PlayerPrefs.GetString(SimulationState.lastEndpointTypeKey, string.Empty);
            var dir = string.Empty;

            if (lastEndpointType != string.Empty)
            {
                var t = GetEndpointFromName(lastEndpointType);
                if (t != null && typeof(IFileSystemEndpoint).IsAssignableFrom(t))
                {
                    dir = PlayerPrefs.GetString(SimulationState.lastFileSystemPathKey, string.Empty);
                }
            }

            if (dir != string.Empty)
            {
                EditorGUILayout.LabelField("Latest Generated Dataset");
                GUILayout.BeginVertical("TextArea");

                var generatedDatasetLabelFieldStyle = new GUIStyle(EditorStyles.textField) { wordWrap = true };
                EditorGUILayout.LabelField(dir, generatedDatasetLabelFieldStyle);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Show Folder"))
                {
                    EditorUtility.RevealInFinder(dir);
                }
                if (GUILayout.Button("Copy Path"))
                {
                    GUIUtility.systemCopyBuffer = dir;
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            if (EditorSettings.asyncShaderCompilation)
            {
                GUILayout.Space(15);
                EditorGUILayout.HelpBox("Asynchronous shader compilation may result in invalid data in beginning frames. " +
                    "This can be disabled in Project Settings -> Editor -> Asynchronous Shader Compilation", MessageType.Warning);
            }
#if !HDRP_PRESENT
            EditorGUILayout.HelpBox("Perception requires HDRP and will not work in this project.", MessageType.Error);
#else
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                var renderPipelineAsset = QualitySettings.GetRenderPipelineAssetAt(i) as HDRenderPipelineAsset;
                CheckRenderPipelineAsset(renderPipelineAsset, $"Issue with HD Render Pipeline for quality level \"{QualitySettings.names[i]}\":\n", "\nConsider removing unnecessary quality levels");
            }
            var hdRenderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset as UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset;
            CheckRenderPipelineAsset(hdRenderPipelineAsset);

            var camera = perceptionCamera.gameObject.GetComponent<HDAdditionalCameraData>();
            if (camera != null && (camera.volumeLayerMask & (1 << perceptionCamera.gameObject.layer)) == 0)
            {
                GUILayout.Space(15);
                EditorGUILayout.HelpBox("Labelers require the parent GameObject must be in a layer included in the camera's Volume Layer Mask.", MessageType.Error);
            }
#endif
        }

#if HDRP_PRESENT
        void CheckRenderPipelineAsset(HDRenderPipelineAsset hdRenderPipelineAsset, string prefix = "", string suffix = "")
        {
            if (hdRenderPipelineAsset != null &&
                hdRenderPipelineAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode ==
                UnityEngine.Rendering.HighDefinition.RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly)
            {
                GUILayout.Space(15);
                EditorGUILayout.HelpBox(prefix + "Deferred Only shader mode is not supported by rendering-based labelers. " +
                    "For correct labeler output, switch Lit Shader Mode to Both or Forward Only in your HD Render Pipeline Asset" + suffix, MessageType.Error);
            }

            if (hdRenderPipelineAsset != null &&
                !hdRenderPipelineAsset.currentPlatformRenderPipelineSettings.supportCustomPass)
            {
                GUILayout.Space(15);
                EditorGUILayout.HelpBox(prefix + "Labelers require Custom Passes to be enabled in your HD Render Pipeline Asset" + suffix, MessageType.Error);
            }
        }

#endif

        Type GetEndpointFromName(string typeName)
        {
            return (from assembly in AppDomain.CurrentDomain.GetAssemblies()  select assembly.GetType(typeName)).FirstOrDefault(t => t != null);
        }

        CameraLabelerDrawer GetCameraLabelerDrawer(SerializedProperty element, int listIndex)
        {
            CameraLabelerDrawer drawer;

            if (m_CameraLabelerDrawers.TryGetValue(element, out drawer))
                return drawer;

            var labeler = perceptionCamera.labelers[listIndex];

            foreach (var drawerType in TypeCache.GetTypesWithAttribute(typeof(CameraLabelerDrawerAttribute)))
            {
                var attr = (CameraLabelerDrawerAttribute)drawerType.GetCustomAttributes(typeof(CameraLabelerDrawerAttribute), true)[0];
                if (attr.targetLabelerType == labeler.GetType())
                {
                    drawer = (CameraLabelerDrawer)Activator.CreateInstance(drawerType);
                    drawer.cameraLabeler = labeler;
                    break;
                }

                if (attr.targetLabelerType.IsAssignableFrom(labeler.GetType()))
                {
                    drawer = (CameraLabelerDrawer)Activator.CreateInstance(drawerType);
                    drawer.cameraLabeler = labeler;
                }
            }

            m_CameraLabelerDrawers[element] = drawer;

            return drawer;
        }
    }
}
