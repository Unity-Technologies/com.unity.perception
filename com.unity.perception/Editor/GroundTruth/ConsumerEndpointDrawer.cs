using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.GroundTruth.Consumers;
using UnityEngine.Perception.Settings;

namespace UnityEditor.Perception.GroundTruth
{
    [CustomPropertyDrawer(typeof(ConsumerEndpointDrawerAttribute))]
    public class ConsumerEndpointDrawer : PropertyDrawer
    {
        const int k_PaddingAmount = 5;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginProperty(position, label, property);

            var settings = property.serializedObject.targetObject as PerceptionSettings;
            var endpoint = settings != null ? settings.endpoint : null;
            var serializedObject = property.serializedObject;

            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (endpoint == null)
            {
                var s = new GUIStyle(EditorStyles.boldLabel);
                position.height = height;
                EditorGUI.LabelField(position, "No active endpoint", s);
            }
            else
            {
                var s = new GUIStyle(EditorStyles.boldLabel);
                position.height = height;
                EditorGUI.LabelField(position, ObjectNames.NicifyVariableName(endpoint.GetType().Name), s);
                position.y += height;

                EditorGUI.indentLevel += 1;

                while (property.NextVisible(true))
                {
                    EditorGUI.PropertyField(position, property);
                    position.y += height + k_PaddingAmount;
                }

                if (endpoint is IFileSystemEndpoint fsEndpoint)
                {
                    // Create a field with a name, text box - un-editable, and a choose path button
                    var p = EditorGUI.PrefixLabel(position, new GUIContent("Base Path"));

                    // if the path is set to the default path, then display what the default path will be
                    // on the user's machine
                    var path = fsEndpoint.basePath;

                    // Selectable label boxes are 4 pixels narrower than other boxes, this is just to align it
                    // with other entries
                    p.x -= 15;
                    p.width += 15;
                    EditorGUI.SelectableLabel(p, path, EditorStyles.textField);

                    position.y += height + k_PaddingAmount;

                    var buttonRect = EditorGUI.IndentedRect(position);
                    var width = buttonRect.width / 3f;
                    buttonRect.width = width;

                    if (GUI.Button(buttonRect,"Change Folder"))
                    {
                        path = EditorUtility.OpenFolderPanel("Choose Output Folder", "", "");
                        if (path.Length != 0)
                        {
                            fsEndpoint.basePath = path;
                            serializedObject.Update();
                        }
                    }

                    buttonRect.x += width;

                    if (GUI.Button(buttonRect,"Show Folder"))
                    {
                        EditorUtility.RevealInFinder(path);
                    }

                    buttonRect.x += width;

                    if (GUI.Button(buttonRect, "Reset To Default"))
                    {
                        fsEndpoint.basePath = fsEndpoint.defaultPathToken;
                        serializedObject.Update();
                    }

                    position.y += height + k_PaddingAmount;
                }

                EditorGUI.indentLevel -= 1;

                EditorGUI.EndProperty();

                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var settings = property.serializedObject.targetObject as PerceptionSettings;
            var endpoint = settings != null ? settings.endpoint : null;

            var p = property.Copy();
            var padding = 0;

            var count = 1;
            if (endpoint != null)
            {
                while (p.NextVisible(true)) count++;
            }

            // if this is an IFileSystemEndpoint we need to add in the height for the button row
            if (endpoint is IFileSystemEndpoint)
            {
                count += 2;
                padding += k_PaddingAmount * 4;
            }

            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * count + padding;
        }
    }
}
