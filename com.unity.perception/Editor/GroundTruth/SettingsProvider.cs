using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.Settings;
using UnityEngine.UIElements;

namespace UnityEditor.Perception.GroundTruth
{
    /// <summary>
    /// Settings provider for the perception settings pane
    /// </summary>
    public class PerceptionSettingsProvider : SettingsProvider
    {
        // ReSharper disable once InconsistentNaming
        SerializedObject _customSettings;
        SerializedObject customSettings
        {
            get => _customSettings ?? (_customSettings = PerceptionSettings.GetSerializedSettings());
            set => _customSettings = value;
        }

        const string k_ProjectPath = "Project/Perception";

        /// <summary>
        /// Use this function to implement a handler for when the user clicks on another setting or when the Settings window closes.
        /// </summary>
        public override void OnDeactivate()
        {
            customSettings = null;
        }

        class Styles
        {
            public static GUIContent endpoint = new GUIContent("Active Endpoint");
        }

        PerceptionSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) {}

        /// <summary>
        /// Use this function to implement a handler for when the user clicks on the Settings in the Settings window. You can fetch a settings Asset or set up UIElements UI from this function.
        /// </summary>
        /// <param name="searchContext">Search context in the search box on the Settings window.</param>
        /// <param name="rootElement">Root of the UIElements tree. If you add to this root, the SettingsProvider uses UIElements instead of calling SettingsProvider.OnGUI to build the UI. If you do not add to this VisualElement, then you must use the IMGUI to build the UI.</param>
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            EditorSceneManager.activeSceneChangedInEditMode += (arg0, scene) => { customSettings = null; };
            Undo.undoRedoPerformed += () =>
            {
                var prop = customSettings.FindProperty("endpoint");
                if (prop != null)
                {
                    customSettings.Update();
                }
            };
        }

        void AddConsumer(Type endpointType)
        {
            var endpoint = (IConsumerEndpoint)Activator.CreateInstance(endpointType);

            var prop = customSettings.FindProperty("endpoint");

            var to = (PerceptionSettings)customSettings.targetObject;

            Undo.RecordObject(to, "Set new endpoint");
            to.consumerEndpoint = endpoint;
            var modified = customSettings.hasModifiedProperties;

            customSettings.ApplyModifiedProperties();

            customSettings.Update();
        }

        /// <summary>
        /// Use this function to draw the UI based on IMGUI. This assumes you haven't added any children to the rootElement passed to the OnActivate function.
        /// </summary>
        /// <param name="searchContext">Search context for the Settings window. Used to show or hide relevant properties</param>
        public override void OnGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            var s = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            EditorGUILayout.BeginVertical(s);

            EditorGUILayout.PropertyField(customSettings.FindProperty(nameof(PerceptionSettings.consumerEndpoint)));

            if (GUILayout.Button("Change Endpoint Type"))
            {
                var dropdownOptions = TypeCache.GetTypesDerivedFrom<IConsumerEndpoint>();
                var menu = new GenericMenu();
                foreach (var option in dropdownOptions)
                {
                    // filter out types that have HideFromCreateMenuAttribute
                    if (option.CustomAttributes.Any(att => att.AttributeType == typeof(HideFromCreateMenuAttribute)))
                        continue;

                    var localOption = option;
                    menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(option.Name)),
                        false,
                        () => AddConsumer(localOption));
                }

                menu.ShowAsContext();
            }

            EditorGUILayout.EndVertical();
            var accumulationMenu = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };


            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(accumulationMenu);

            EditorGUILayout.PropertyField(customSettings.FindProperty("accumulationSettings"));

            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
                customSettings.ApplyModifiedProperties();
        }

        /// <summary>
        /// Creates the settings provider
        /// </summary>
        /// <returns>The perception settings</returns>
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new PerceptionSettingsProvider(k_ProjectPath, SettingsScope.Project)
            {
                // Automatically extract all keywords from the Styles.
                keywords = GetSearchKeywordsFromGUIContentProperties<Styles>()
            };

            return provider;
        }
    }
}
