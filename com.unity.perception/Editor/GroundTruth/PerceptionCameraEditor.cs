using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomEditor(typeof(PerceptionCamera))]
    sealed class PerceptionCameraEditor : Editor
    {
        ReorderableList m_LabelersList;

        SerializedProperty labelersProperty => this.serializedObject.FindProperty("m_Labelers");

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

        PerceptionCamera perceptionCamera => ((PerceptionCamera)this.target);

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
                    () => AddLabeler(labelers, localOption));
            }

            menu.ShowAsContext();
        }

        void AddLabeler(SerializedProperty labelers, Type labelerType)
        {
            var insertIndex = labelers.arraySize;
            labelers.InsertArrayElementAtIndex(insertIndex);
            var element = labelers.GetArrayElementAtIndex(insertIndex);
            var labeler = (CameraLabeler)Activator.CreateInstance(labelerType);
            labeler.enabled = true;
            element.managedReferenceValue = labeler;
            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.description)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.period)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.startTime)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(perceptionCamera.captureRgbImages)));

            //EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(PerceptionCamera.labelers)));
            m_LabelersList.DoLayoutList();

            if (EditorSettings.asyncShaderCompilation)
            {
                EditorGUILayout.HelpBox("Asynchronous shader compilation may result in invalid data in beginning frames. This can be disabled in Project Settings -> Edtior -> Asynchronous Shader Compilation", MessageType.Warning);
            }
        }

        Dictionary<SerializedProperty, CameraLabelerDrawer> m_CameraLabelerDrawers = new Dictionary<SerializedProperty, CameraLabelerDrawer>();

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

    [AttributeUsage(AttributeTargets.Class)]
    class CameraLabelerDrawerAttribute : Attribute
    {
        public CameraLabelerDrawerAttribute(Type targetLabelerType)
        {
            this.targetLabelerType = targetLabelerType;
        }

        public Type targetLabelerType;
    }

    /// <summary>
    ///
    /// </summary>
    [CameraLabelerDrawer(typeof(CameraLabeler))]
    public class CameraLabelerDrawer
    {
        /// <summary>
        /// The cameraLabeler instance
        /// </summary>
        public CameraLabeler cameraLabeler { get; internal set; }
        /// <summary>
        /// The cameraLabeler instance
        /// </summary>
        public SerializedProperty cameraLabelerProperty { get; private set; }

        class Styles
        {
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static float reorderableListHandleIndentWidth = 12;
            public static GUIContent enabled = new GUIContent("Enabled", "Enable or Disable the camera labeler");
        }

        bool m_IsInitialized = true;

        // Serialized Properties
        SerializedProperty m_Enabled;
        List<SerializedProperty> m_LabelerUserProperties = new List<SerializedProperty>();

        void FetchProperties()
        {
            m_Enabled = cameraLabelerProperty.FindPropertyRelative(nameof(cameraLabeler.enabled));
        }

        void LoadUserProperties()
        {
            foreach (var field in cameraLabeler.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var serializeField = field.GetCustomAttribute<SerializeField>();
                var hideInInspector = field.GetCustomAttribute<HideInInspector>();
                var nonSerialized = field.GetCustomAttribute<NonSerializedAttribute>();

                if (nonSerialized != null || hideInInspector != null)
                    continue;

                if (!field.IsPublic && serializeField == null)
                    continue;

                if (field.Name == nameof(cameraLabeler.enabled))
                    continue;

                var prop = cameraLabelerProperty.FindPropertyRelative(field.Name);
                if (prop != null)
                    m_LabelerUserProperties.Add(prop);
            }
        }

        void EnsureInitialized(SerializedProperty property)
        {
            if (m_IsInitialized)
                return;

            cameraLabelerProperty = property;
            FetchProperties();
            Initialize();
            LoadUserProperties();
            m_IsInitialized = true;
        }

        /// <summary>
        /// Use this function to initialize the local SerializedProperty you will use in your labeler.
        /// </summary>
        protected virtual void Initialize() { }

        internal void OnGUI(Rect rect, SerializedProperty property)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginChangeCheck();

            EnsureInitialized(property);

            DoHeaderGUI(ref rect);

            EditorGUI.BeginDisabledGroup(!m_Enabled.boolValue);
            {
                DoLabelerGUI(rect);
            }
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Implement this function to draw your custom GUI.
        /// </summary>
        /// <param name="rect">space available for you to draw the UI</param>
        protected virtual void DoLabelerGUI(Rect rect)
        {
            foreach (var prop in m_LabelerUserProperties)
            {
                EditorGUI.PropertyField(rect, prop);
                rect.y += Styles.defaultLineSpace;
            }
        }

        void DoHeaderGUI(ref Rect rect)
        {
            var enabledSize = EditorStyles.boldLabel.CalcSize(Styles.enabled) + new Vector2(Styles.reorderableListHandleIndentWidth, 0);
            var headerRect = new Rect(rect.x,
                rect.y + EditorGUIUtility.standardVerticalSpacing,
                rect.width - enabledSize.x,
                EditorGUIUtility.singleLineHeight);
            rect.y += Styles.defaultLineSpace;
            var enabledRect = headerRect;
            enabledRect.x = rect.xMax - enabledSize.x;
            enabledRect.width = enabledSize.x;

            EditorGUI.LabelField(headerRect, $"{cameraLabeler.GetType().Name}", EditorStyles.boldLabel);
            EditorGUIUtility.labelWidth = enabledRect.width - 14;
            m_Enabled.boolValue = EditorGUI.Toggle(enabledRect, Styles.enabled, m_Enabled.boolValue);
            EditorGUIUtility.labelWidth = 0;
        }

        /// <summary>
        /// Implement this functions if you implement <see cref="DoLabelerGUI"/>. The result of this function must match the number of lines displayed in your custom GUI.
        /// Note that this height can be dynamic.
        /// </summary>
        /// <returns>The height in pixels of tour camera labeler GUI</returns>
        protected virtual float GetHeight()
        {
            var height = 0f;

            foreach (var prop in m_LabelerUserProperties)
            {
                height += EditorGUI.GetPropertyHeight(prop);
                height += EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        internal float GetElementHeight(SerializedProperty property)
        {
            EnsureInitialized(property);
            return Styles.defaultLineSpace + GetHeight();
        }
    }
}
