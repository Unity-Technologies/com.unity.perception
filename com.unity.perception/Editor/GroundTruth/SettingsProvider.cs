using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.Settings;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

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

        /// <inheritdoc/>
        public override void OnDeactivate()
        {
            customSettings = null;
        }

        class Styles
        {
            public static GUIContent endpoint = new GUIContent("Active Endpoint");
        }

        PerceptionSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        /// <inheritdoc/>
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
            to.endpoint = endpoint;
            var modified = customSettings.hasModifiedProperties;

            customSettings.ApplyModifiedProperties();

            customSettings.Update();
        }

        /// <inheritdoc/>
        public override void OnGUI(string searchContext)
        {
            EditorGUI.BeginChangeCheck();

            var s = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            EditorGUILayout.BeginVertical(s);

            var itr = customSettings.GetIterator();
            var firstTime = true;
            while (itr.NextVisible(firstTime))
            {
                // Do not display the box that shows the script name
                if (itr.name.Contains("cript")) continue;

                firstTime = false;
                EditorGUILayout.PropertyField(itr);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Change Endpoint Type"))
            {
                var dropdownOptions = TypeCache.GetTypesDerivedFrom<IConsumerEndpoint>();
                var menu = new GenericMenu();
                foreach (var option in dropdownOptions)
                {
                    // filter out types that have HideFromCreateMenuAttribute
                    if (option.CustomAttributes.Any(att => att.AttributeType == typeof(HideFromCreateMenu)))
                        continue;

                    var localOption = option;
                    menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(option.Name)),
                        false,
                        () => AddConsumer(localOption));
                }

                menu.ShowAsContext();
            }

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

