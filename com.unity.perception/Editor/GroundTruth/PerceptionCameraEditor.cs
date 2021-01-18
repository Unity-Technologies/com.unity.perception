using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
            m_LabelersList = new ReorderableList(this.serializedObject, labelersProperty, true, false, true, true);
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


        string onlyRenderCaptTitle = "Only Render Captured Frames";
        string periodTilte = "Capture and Render Delta Time";
        string frametimeTitle = "Rendering Delta Time";
        int startFrame;
        public override void OnInspectorGUI()
        {
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.description)), new GUIContent("Description", "Provide a description for this perception camera (optional)."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.showVisualizations)), new GUIContent("Show Labeler Visualizations", "Display realtime visualizations for labelers that are currently active on this perception camera."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.captureRgbImages)),new GUIContent("Save Camera Output to Disk", "For each captured frame, save an RGB image of the perception camera's output to disk."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.captureTriggerMode)),new GUIContent("Capture Trigger Mode", $"The method of triggering captures for this camera. In {nameof(PerceptionCamera.CaptureTriggerMode.Scheduled)} mode, captures happen automatically based on a start time/frame and time/frame interval. In {nameof(PerceptionCamera.CaptureTriggerMode.Manual)} mode, captures should be triggered manually through calling the {nameof(perceptionCamera.CaptureOnNextUpdate)} method of {nameof(PerceptionCamera)}."));

                if (perceptionCamera.captureTriggerMode.Equals(PerceptionCamera.CaptureTriggerMode.Scheduled))
                {
                    GUILayout.BeginVertical("TextArea");
                    EditorGUILayout.LabelField("Scheduled Capture Properties", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.onlyRenderCapturedFrames)),new GUIContent(onlyRenderCaptTitle, $"If this checkbox is enabled, the attached camera will only render those frames that it needs to capture. In addition, the global frame delta time will be altered to match this camera's capture period, thus, the scene will not be visually updated in-between captures (physics simulation is unaffected). Therefore, if you have more than one {nameof(PerceptionCamera)} active, this flag should be either disabled or enabled for all of them, otherwise the cameras will not capture and synchronize properly."));

                    if (perceptionCamera.onlyRenderCapturedFrames)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.startTime)), new GUIContent("Start Time","Time at which this perception camera starts rendering and capturing (seconds)."));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.period)), new GUIContent(periodTilte, "The interval at which the perception camera should render and capture (seconds)."));

                        EditorGUILayout.HelpBox($"First capture at {perceptionCamera.startTime} seconds and consecutive captures every {perceptionCamera.period} seconds of simulation time.", MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.renderingDeltaTime)),new GUIContent(frametimeTitle, "The rendering delta time (seconds of simulation time). E.g. 0.0166 translates to roughly 60 frames per second. Note that if the hardware is not capable of rendering, capturing, and saving the required number of frames per second, the simulation will slow down in real time in order to produce the exact number of required frames per each second of simulation time. Thus, the results will always be correct with regard to simulation time but may look slow in real time."));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.startFrame)), new GUIContent("Start at Frame",$"Frame number at which this camera starts capturing."));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.framesBetweenCaptures)),new GUIContent("Frames Between Captures", "The number of frames to render between the camera's scheduled captures. Setting this to 0 makes the camera capture every rendered frame."));

                        //Because start time only needs to be calculated once, we can do it here. But for scheduling consecutive captures,
                        //we calculate the time of the next capture every time based on the values given for captureEveryXFrames and renderingDeltaTime, in order to preserve accuracy.
                        perceptionCamera.startTime = perceptionCamera.startFrame * perceptionCamera.renderingDeltaTime;

                        var interval = (perceptionCamera.framesBetweenCaptures + 1) * perceptionCamera.renderingDeltaTime;
                        EditorGUILayout.HelpBox($"First capture at {perceptionCamera.startTime} seconds and consecutive captures every {interval} seconds of simulation time.", MessageType.None);
                    }


                    GUILayout.EndVertical();
                }
                else
                {
                    perceptionCamera.onlyRenderCapturedFrames = false;
                    EditorGUILayout.HelpBox($"Captures should be triggered manually through calling the {nameof(perceptionCamera.CaptureOnNextUpdate)} method of {nameof(PerceptionCamera)}. Framerate or simulation timings will not be modified by this camera.", MessageType.None);
                }


                serializedObject.ApplyModifiedProperties();

                m_LabelersList.DoLayoutList();
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
