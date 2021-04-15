using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(PerceptionCamera))]
    sealed class PerceptionCameraEditor : Editor
    {
        Dictionary<SerializedProperty, CameraLabelerDrawer> m_CameraLabelerDrawers = new Dictionary<SerializedProperty, CameraLabelerDrawer>();
        ReorderableList m_LabelersList;

        SerializedProperty labelersProperty => this.serializedObject.FindProperty("m_Labelers");

        PerceptionCamera perceptionCamera => ((PerceptionCamera)this.target);

        public void OnEnable()
        {
            m_LabelersList = new ReorderableList(this.serializedObject, labelersProperty, true, true, true, true);
            m_LabelersList.drawHeaderCallback = (rect) =>
            {
                EditorGUI.LabelField(rect, "Camera Labelers", EditorStyles.largeLabel);
            };
            m_LabelersList.elementHeightCallback = GetElementHeight;
            m_LabelersList.drawElementCallback = DrawElement;
            m_LabelersList.onAddCallback += OnAdd;
            m_LabelersList.onRemoveCallback += OnRemove;
        }

        float GetElementHeight(int index)
        {
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
            var labelers = labelersProperty;

            var dropdownOptions = TypeCache.GetTypesDerivedFrom<CameraLabeler>();
            var menu = new GenericMenu();
            foreach (var option in dropdownOptions)
            {
                var localOption = option;
                menu.AddItem(new GUIContent(option.Name),
                    false,
                    () => AddLabeler(localOption));
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
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.description)), new GUIContent("Description", "Provide a description for this camera (optional)."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.showVisualizations)), new GUIContent("Show Labeler Visualizations", "Display realtime visualizations for labelers that are currently active on this camera."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.captureRgbImages)),new GUIContent("Save Camera RGB Output to Disk", "For each captured frame, save an RGB image of the camera's output to disk."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.captureTriggerMode)),new GUIContent("Capture Trigger Mode", $"The method of triggering captures for this camera. In {nameof(CaptureTriggerMode.Scheduled)} mode, captures happen automatically based on a start frame and frame delta time. In {nameof(CaptureTriggerMode.Manual)} mode, captures should be triggered manually through calling the {nameof(perceptionCamera.RequestCapture)} method of {nameof(PerceptionCamera)}."));

                GUILayout.Space(5);
                if (perceptionCamera.captureTriggerMode.Equals(CaptureTriggerMode.Scheduled))
                {
                    GUILayout.BeginVertical("TextArea");
                    EditorGUILayout.LabelField("Scheduled Capture Properties", EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.simulationDeltaTime)),new GUIContent(k_FrametimeTitle, $"Sets Unity's Time.{nameof(Time.captureDeltaTime)} to the specified number, causing a fixed number of frames to be simulated for each second of elapsed simulation time regardless of the capabilities of the underlying hardware. Thus, simulation time and real time will not be synchronized. Note that large {k_FrametimeTitle} values will lead to lower performance as the engine will need to simulate longer periods of elapsed time for each rendered frame."));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.firstCaptureFrame)), new GUIContent("Start at Frame",$"Frame number at which this camera starts capturing."));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.framesBetweenCaptures)),new GUIContent("Frames Between Captures", "The number of frames to simulate and render between the camera's scheduled captures. Setting this to 0 makes the camera capture every frame."));

                    if (perceptionCamera.simulationDeltaTime > k_DeltaTimeTooLarge)
                    {
                        EditorGUILayout.HelpBox($"Large {k_FrametimeTitle} values can lead to significantly lower simulation performance.", MessageType.Warning);
                    }

                    var interval = (perceptionCamera.framesBetweenCaptures + 1) * perceptionCamera.simulationDeltaTime;
                    var startTime = perceptionCamera.simulationDeltaTime * perceptionCamera.firstCaptureFrame;
                    EditorGUILayout.HelpBox($"First capture at {startTime} seconds and consecutive captures every {interval} seconds of simulation time.", MessageType.None);

                    GUILayout.EndVertical();
                }
                else
                {
                    GUILayout.BeginVertical("TextArea");
                    EditorGUILayout.LabelField("Manual Capture Properties", EditorStyles.boldLabel);

                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.manualSensorAffectSimulationTiming)),new GUIContent("Affect Simulation Timing", $"Have this camera affect simulation timings (similar to a scheduled camera) by requesting a specific frame delta time."));

                    if (perceptionCamera.manualSensorAffectSimulationTiming)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.simulationDeltaTime)),new GUIContent(k_FrametimeTitle, $"Sets Unity's Time.{nameof(Time.captureDeltaTime)} to the specified number, causing a fixed number of frames to be generated for each second of elapsed simulation time regardless of the capabilities of the underlying hardware. Thus, simulation time and real time will not be synchronized."));
                    }

                    EditorGUILayout.HelpBox($"Captures should be triggered manually through calling the {nameof(perceptionCamera.RequestCapture)} method of {nameof(PerceptionCamera)}.", MessageType.None);
                    GUILayout.EndVertical();
                }


                serializedObject.ApplyModifiedProperties();

                GUILayout.Space(15);

                m_LabelersList.DoLayoutList();
            }

            var dir = PlayerPrefs.GetString(SimulationState.latestOutputDirectoryKey, string.Empty);
            if (dir != string.Empty)
            {
                EditorGUILayout.LabelField("Latest Output Folder");
                GUILayout.BeginVertical("TextArea");
                EditorGUILayout.HelpBox(dir, MessageType.None);
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
                EditorGUILayout.HelpBox("Asynchronous shader compilation may result in invalid data in beginning frames. " +
                    "This can be disabled in Project Settings -> Editor -> Asynchronous Shader Compilation", MessageType.Warning);
            }
#if HDRP_PRESENT
            var hdRenderPipelineAsset = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset as UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset;
            if (hdRenderPipelineAsset != null &&
                hdRenderPipelineAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode ==
                UnityEngine.Rendering.HighDefinition.RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly)
            {
                EditorGUILayout.HelpBox("Deferred Only shader mode is not supported by rendering-based labelers. " +
                    "For correct labeler output, switch Lit Shader Mode to Both or Forward Only in your HD Render Pipeline Asset", MessageType.Error);
            }
#endif
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
