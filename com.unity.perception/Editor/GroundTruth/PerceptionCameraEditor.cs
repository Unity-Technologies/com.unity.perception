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

        public override void OnInspectorGUI()
        {
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.description)), new GUIContent("Description", "Provide a description for this perception camera (optional)."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.period)), new GUIContent("Capture Interval", "The interval at which the perception camera should render and capture (seconds)."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.startTime)), new GUIContent("Start Time","Time at which this perception camera starts rendering and capturing (seconds)."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.showVisualizations)), new GUIContent("Show Labeler Visualizations", "Display realtime visualizations for labelers that are currently active on this perception camera."));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.captureRgbImages)),new GUIContent("Save Camera Output to Disk", "For each captured frame, save an RGB image of the perception camera's output to disk."));
                serializedObject.ApplyModifiedProperties();

                //EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(PerceptionCamera.labelers)));
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
