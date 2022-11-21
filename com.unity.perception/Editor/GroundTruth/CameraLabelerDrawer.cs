using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;

namespace UnityEditor.Perception.GroundTruth
{
    /// <summary>
    /// Base class for all inspector drawers for <see cref="CameraLabeler"/> types. To override the inspector for a
    /// CameraLabeler, derive from this class and decorate the class with a <see cref="CameraLabelerDrawerAttribute"/>
    /// </summary>
    [CameraLabelerDrawer(typeof(CameraLabeler))]
    public class CameraLabelerDrawer
    {
        SerializedProperty m_Enabled;

        bool m_IsInitialized;
        List<SerializedProperty> m_LabelerUserProperties = new List<SerializedProperty>();

        /// <summary>
        /// The cameraLabeler insta nce
        /// </summary>
        public CameraLabeler cameraLabeler { get; internal set; }

        /// <summary>
        /// The cameraLabeler instance
        /// </summary>
        public SerializedProperty cameraLabelerProperty { get; private set; }

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
        protected virtual void Initialize() {}

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
                EditorGUI.PropertyField(rect, prop, true);
                var height = EditorGUI.GetPropertyHeight(prop) + EditorGUIUtility.standardVerticalSpacing;
                rect.y += height;
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

            var labelerName = ObjectNames.NicifyVariableName(cameraLabeler.GetType().Name);
            EditorGUI.LabelField(headerRect, new GUIContent($"{labelerName}", $"{cameraLabeler.description}"), EditorStyles.boldLabel);
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

        class Styles
        {
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static float reorderableListHandleIndentWidth = 12;
            public static GUIContent enabled = new GUIContent("Enabled", "Enable or Disable the camera labeler");
        }
    }
}
